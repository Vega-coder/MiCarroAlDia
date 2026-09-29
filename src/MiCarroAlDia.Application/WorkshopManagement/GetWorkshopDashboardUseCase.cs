using MiCarroAlDia.Application.Abstractions;
using MiCarroAlDia.Application.Common;
using MiCarroAlDia.Domain.Enums;

namespace MiCarroAlDia.Application.WorkshopManagement;

public class GetWorkshopDashboardUseCase
{
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IAdditionalQuoteRepository _quoteRepository;
    private readonly ICustomerAccessLinkRepository _accessLinkRepository;
    private readonly TimeProvider _timeProvider;

    public GetWorkshopDashboardUseCase(
        IWorkshopRepository workshopRepository,
        IWorkOrderRepository workOrderRepository,
        IAdditionalQuoteRepository quoteRepository,
        ICustomerAccessLinkRepository accessLinkRepository,
        TimeProvider timeProvider)
    {
        _workshopRepository = workshopRepository;
        _workOrderRepository = workOrderRepository;
        _quoteRepository = quoteRepository;
        _accessLinkRepository = accessLinkRepository;
        _timeProvider = timeProvider;
    }

    public async Task<WorkshopDashboardDto> ExecuteAsync(string workshopId = "taller-autofrenos", CancellationToken ct = default)
    {
        var workshop = await _workshopRepository.GetByIdAsync(workshopId, ct);
        var nowUtc = _timeProvider.GetUtcNow();

        var dto = new WorkshopDashboardDto
        {
            WorkshopId = workshopId,
            WorkshopName = workshop?.Name ?? "Autofrenos del Norte",
            WorkshopPhone = workshop?.Phone ?? "(604) 444-1234",
            WorkshopAddress = workshop != null ? $"{workshop.Address}, {workshop.City}" : "Sede Principal"
        };

        var allOrders = await _workOrderRepository.GetAllAsync(ct);
        var workshopOrders = allOrders.Where(o => o.WorkshopId == workshopId).ToList();

        foreach (var order in workshopOrders)
        {
            var quote = await _quoteRepository.GetByWorkOrderIdAsync(order.Id, ct);
            var link = await _accessLinkRepository.GetByWorkOrderIdAsync(order.Id, ct);

            var nextState = (int)order.CurrentProgressState < 6 
                ? (VehicleProgressState?)((int)order.CurrentProgressState + 1) 
                : null;

            var card = new WorkshopOrderCardDto
            {
                WorkOrderId = order.Id,
                VehiclePlate = order.VehiclePlate,
                VehicleModel = order.VehicleModel,
                CustomerName = order.CustomerName,
                CurrentState = order.CurrentProgressState,
                CurrentStepNumber = (int)order.CurrentProgressState,
                CurrentStateName = order.CurrentProgressState.ToFriendlyName(),
                CanAdvanceProgress = nextState.HasValue,
                NextState = nextState,
                NextStateName = nextState?.ToFriendlyName(),
                CustomerToken = link?.Token,
                CustomerAccessUrl = link != null ? $"/t/{link.Token}" : null,
                CanAddQuote = quote == null,
                History = order.ProgressHistory
                    .OrderByDescending(h => h.TimestampUtc)
                    .Select(h => new ProgressHistoryItemDto
                    {
                        StateName = h.State.ToFriendlyName(),
                        TimestampFormatted = h.TimestampUtc.ToShortColombiaDate(),
                        Note = h.Note
                    }).ToList()
            };

            if (quote != null)
            {
                card.QuoteId = quote.Id;
                var status = quote.GetStatus(nowUtc);
                card.QuoteStatus = status;

                var (baseProp, ivaProp, totalProp) = quote.CalculateProposedTotals();
                card.TotalProposedCop = totalProp.ToColombianCurrency();

                if (status == Domain.Enums.QuoteStatus.Respondida && quote.Response != null)
                {
                    card.QuoteStatusBadge = "Respondida por cliente";
                    card.TotalAuthorizedCop = quote.Response.TotalAuthorizedAmount.ToColombianCurrency();
                    card.ItemsApprovedCount = quote.Response.Items.Count(i => i.Decision == CustomerDecision.Aprobar);
                    card.ItemsRejectedCount = quote.Response.Items.Count(i => i.Decision == CustomerDecision.Rechazar);
                    card.RespondedAtFormatted = quote.Response.SubmittedAtUtc.ToShortColombiaDate();
                }
                else if (status == Domain.Enums.QuoteStatus.Vencida)
                {
                    card.QuoteStatusBadge = "Vencida (>48h)";
                    card.QuoteHoursRemainingText = "Plazo de 48h expirado";
                }
                else
                {
                    card.QuoteStatusBadge = "Pendiente cliente";
                    var remaining = quote.ExpiresAtUtc - nowUtc;
                    var hours = Math.Max(0, (int)remaining.TotalHours);
                    card.QuoteHoursRemainingText = $"{hours}h restantes";
                }
            }

            dto.Orders.Add(card);
        }

        dto.TotalActiveOrders = dto.Orders.Count;
        dto.InProgressCount = dto.Orders.Count(o => o.CurrentState == VehicleProgressState.Diagnostico || o.CurrentState == VehicleProgressState.Reparacion);
        dto.PendingQuotesCount = dto.Orders.Count(o => o.QuoteStatus == Domain.Enums.QuoteStatus.Pendiente);
        dto.CompletedCount = dto.Orders.Count(o => o.CurrentState >= VehicleProgressState.ListoParaEntregar);

        return dto;
    }
}
