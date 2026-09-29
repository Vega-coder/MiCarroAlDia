using MiCarroAlDia.Application.Abstractions;
using MiCarroAlDia.Application.Common;
using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Domain.Exceptions;

namespace MiCarroAlDia.Application.CustomerTracking;

public class GetCustomerTrackingUseCase
{
    private readonly ICustomerAccessLinkRepository _linkRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IAdditionalQuoteRepository _quoteRepository;
    private readonly TimeProvider _timeProvider;

    public GetCustomerTrackingUseCase(
        ICustomerAccessLinkRepository linkRepository,
        IWorkOrderRepository workOrderRepository,
        IWorkshopRepository workshopRepository,
        IAdditionalQuoteRepository quoteRepository,
        TimeProvider timeProvider)
    {
        _linkRepository = linkRepository;
        _workOrderRepository = workOrderRepository;
        _workshopRepository = workshopRepository;
        _quoteRepository = quoteRepository;
        _timeProvider = timeProvider;
    }

    public async Task<CustomerTrackingDto?> ExecuteAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var link = await _linkRepository.GetByTokenAsync(token.Trim(), ct);
        if (link == null || !link.IsActive)
        {
            return null;
        }

        var workOrder = await _workOrderRepository.GetByIdAsync(link.WorkOrderId, ct);
        if (workOrder == null || workOrder.WorkshopId != link.WorkshopId)
        {
            // Aislamiento: si la orden no pertenece al taller del enlace, no se revela información
            return null;
        }

        var workshop = await _workshopRepository.GetByIdAsync(link.WorkshopId, ct);
        if (workshop == null)
        {
            return null;
        }

        var nowUtc = _timeProvider.GetUtcNow();

        // Construir la secuencia de los 6 estados del vehículo
        var steps = new List<VehicleProgressStepDto>();
        var allStates = Enum.GetValues<VehicleProgressState>().OrderBy(s => (int)s);

        foreach (var state in allStates)
        {
            var historyRecord = workOrder.ProgressHistory.LastOrDefault(h => h.State == state);
            var isCurrent = workOrder.CurrentProgressState == state;
            var isCompleted = (int)state <= (int)workOrder.CurrentProgressState;

            steps.Add(new VehicleProgressStepDto
            {
                State = state,
                Name = state.ToFriendlyName(),
                IsCompleted = isCompleted,
                IsCurrent = isCurrent,
                ReachedAtFormatted = historyRecord?.TimestampUtc.ToFriendlyColombiaDate(),
                Note = historyRecord?.Note
            });
        }

        // Consultar adicionales si existen
        var quote = await _quoteRepository.GetByWorkOrderIdAsync(workOrder.Id, ct);
        AdditionalQuoteDetailsDto? quoteDto = null;

        if (quote != null)
        {
            var status = quote.GetStatus(nowUtc);
            var isExpired = quote.IsExpired(nowUtc);
            var remainingHours = (quote.ExpiresAtUtc - nowUtc).TotalHours;

            string remainingText = status switch
            {
                QuoteStatus.Respondida => "Respuesta enviada",
                QuoteStatus.Vencida => "Plazo de 48 horas vencido",
                _ => remainingHours switch
                {
                    > 1 => $"Tienes hasta el {quote.ExpiresAtUtc.ToFriendlyColombiaDate()} para responder (aprox. {Math.Ceiling(remainingHours)} horas restantes).",
                    > 0 => $"¡Atención! Queda menos de una hora para responder (vence a las {quote.ExpiresAtUtc.ToShortColombiaDate()}).",
                    _ => "Plazo de 48 horas vencido"
                }
            };

            var (proposedBase, proposedIva, proposedTotal) = quote.CalculateProposedTotals();

            var itemsDto = quote.Items.Select(item => new QuoteItemDisplayDto
            {
                Id = item.Id,
                Description = item.Description,
                Type = item.Type,
                TypeName = item.Type.ToFriendlyName(),
                Category = item.Category,
                IsSafetyCritical = item.Category.IsSafetyCritical(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                UnitPriceFormatted = item.UnitPrice.ToColombianCurrency(),
                BaseAmount = item.BaseAmount,
                BaseAmountFormatted = item.BaseAmount.ToColombianCurrency(),
                IvaAmount = item.IvaAmount,
                IvaAmountFormatted = item.IvaAmount.ToColombianCurrency(),
                TotalAmount = item.TotalAmount,
                TotalAmountFormatted = item.TotalAmount.ToColombianCurrency()
            }).ToList();

            QuoteResponseDisplayDto? responseDto = null;
            if (quote.Response != null)
            {
                responseDto = new QuoteResponseDisplayDto
                {
                    SubmittedAtFormatted = quote.Response.SubmittedAtUtc.ToFriendlyColombiaDate(),
                    TotalAuthorizedBase = quote.Response.TotalAuthorizedBase,
                    TotalAuthorizedBaseFormatted = quote.Response.TotalAuthorizedBase.ToColombianCurrency(),
                    TotalAuthorizedIva = quote.Response.TotalAuthorizedIva,
                    TotalAuthorizedIvaFormatted = quote.Response.TotalAuthorizedIva.ToColombianCurrency(),
                    TotalAuthorizedAmount = quote.Response.TotalAuthorizedAmount,
                    TotalAuthorizedAmountFormatted = quote.Response.TotalAuthorizedAmount.ToColombianCurrency(),
                    Items = quote.Response.Items.Select(r => new ItemResponseDisplayDto
                    {
                        QuoteItemId = r.QuoteItemId,
                        Description = r.Description,
                        TypeName = r.Type.ToFriendlyName(),
                        IsSafetyCritical = r.Category.IsSafetyCritical(),
                        Decision = r.Decision,
                        DecisionFriendlyName = r.Decision.ToFriendlyName(),
                        SecurityRejectionConfirmed = r.SecurityRejectionConfirmed,
                        Quantity = r.Quantity,
                        UnitPriceFormatted = r.UnitPrice.ToColombianCurrency(),
                        TotalAmountFormatted = r.TotalAmount.ToColombianCurrency()
                    }).ToList()
                };
            }

            quoteDto = new AdditionalQuoteDetailsDto
            {
                QuoteId = quote.Id,
                Status = status,
                StatusFriendlyName = status.ToFriendlyName(),
                PublishedAtFormatted = quote.PublishedAtUtc.ToFriendlyColombiaDate(),
                ExpiresAtFormatted = quote.ExpiresAtUtc.ToFriendlyColombiaDate(),
                IsExpired = isExpired,
                RemainingTimeText = remainingText,
                TotalProposedBase = proposedBase,
                TotalProposedBaseFormatted = proposedBase.ToColombianCurrency(),
                TotalProposedIva = proposedIva,
                TotalProposedIvaFormatted = proposedIva.ToColombianCurrency(),
                TotalProposedAmount = proposedTotal,
                TotalProposedAmountFormatted = proposedTotal.ToColombianCurrency(),
                Items = itemsDto,
                Response = responseDto
            };
        }

        return new CustomerTrackingDto
        {
            WorkshopName = workshop.Name,
            WorkshopPhone = workshop.Phone,
            WorkshopAddress = workshop.Address,
            WorkshopCity = workshop.City,
            VehiclePlate = workOrder.VehiclePlate,
            VehicleModel = workOrder.VehicleModel,
            CustomerName = workOrder.CustomerName,
            CurrentProgressState = workOrder.CurrentProgressState,
            CurrentProgressStateName = workOrder.CurrentProgressState.ToFriendlyName(),
            CurrentProgressStateDescription = workOrder.CurrentProgressState.ToFriendlyDescription(),
            ProgressSteps = steps,
            HasAdditionalQuote = quote != null,
            Quote = quoteDto
        };
    }
}
