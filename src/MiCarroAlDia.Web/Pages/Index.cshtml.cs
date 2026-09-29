using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiCarroAlDia.Application.Abstractions;
using MiCarroAlDia.Application.Common;
using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Infrastructure.Persistence.InMemory;
using MiCarroAlDia.Infrastructure.Persistence.Postgres;

namespace MiCarroAlDia.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ICustomerAccessLinkRepository _linkRepository;
    private readonly IWorkOrderRepository _orderRepository;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IAdditionalQuoteRepository _quoteRepository;
    private readonly TimeProvider _timeProvider;

    public IndexModel(
        ICustomerAccessLinkRepository linkRepository,
        IWorkOrderRepository orderRepository,
        IWorkshopRepository workshopRepository,
        IAdditionalQuoteRepository quoteRepository,
        TimeProvider timeProvider)
    {
        _linkRepository = linkRepository;
        _orderRepository = orderRepository;
        _workshopRepository = workshopRepository;
        _quoteRepository = quoteRepository;
        _timeProvider = timeProvider;
    }

    public List<ActiveOrderCardDto> ActiveCards { get; private set; } = new();

    [BindProperty]
    public string? CustomToken { get; set; }

    public async Task OnGetAsync()
    {
        var links = await _linkRepository.GetAllActiveAsync();
        var nowUtc = _timeProvider.GetUtcNow();

        var cards = new List<ActiveOrderCardDto>();

        foreach (var link in links)
        {
            var order = await _orderRepository.GetByIdAsync(link.WorkOrderId);
            if (order == null) continue;

            var workshop = await _workshopRepository.GetByIdAsync(link.WorkshopId);
            var quote = await _quoteRepository.GetByWorkOrderIdAsync(order.Id);

            var status = quote?.GetStatus(nowUtc);

            string statusName;
            string badgeClass;
            string borderClass;
            string headerClass;
            string btnClass;
            string btnText;

            if (quote == null)
            {
                statusName = "Sin adicionales";
                badgeClass = "bg-info text-dark";
                borderClass = "border-info";
                headerClass = "bg-info text-dark";
                btnClass = "btn-outline-info";
                btnText = "🚗 Consultar avance listo →";
            }
            else if (status == QuoteStatus.Respondida)
            {
                statusName = "Respondida (Bloqueada)";
                badgeClass = "bg-success text-white";
                borderClass = "border-success";
                headerClass = "bg-success text-white";
                btnClass = "btn-outline-success";
                btnText = "📋 Ver comprobante definitivo →";
            }
            else if (status == QuoteStatus.Vencida)
            {
                statusName = "Vencida (>48 horas)";
                badgeClass = "bg-secondary text-white";
                borderClass = "border-secondary";
                headerClass = "bg-secondary text-white";
                btnClass = "btn-outline-secondary";
                btnText = "⏰ Ver vista vencida →";
            }
            else // Pendiente
            {
                var remaining = quote.ExpiresAtUtc - nowUtc;
                var hoursLeft = Math.Max(0, (int)remaining.TotalHours);
                statusName = $"Pendiente ({hoursLeft}h restantes)";
                badgeClass = "bg-warning text-dark";
                borderClass = "border-primary";
                headerClass = "bg-primary text-white";
                btnClass = "btn-primary";
                btnText = "📲 Abrir vista del cliente →";
            }

            string itemsSummary;
            if (quote == null)
            {
                itemsSummary = "Mantenimiento rutinario sin daños imprevistos. Línea de tiempo completa disponible.";
            }
            else
            {
                var safetyCount = quote.Items.Count(i => i.Category == ItemCategory.Seguridad);
                var generalCount = quote.Items.Count - safetyCount;
                var parts = new List<string>();
                if (safetyCount > 0) parts.Add($"{safetyCount} de seguridad");
                if (generalCount > 0) parts.Add($"{generalCount} general");
                var catSummary = string.Join(" y ", parts);

                itemsSummary = $"Contiene {quote.Items.Count} adicional(es) ({catSummary}). Total cotizado: {quote.CalculateProposedTotals().TotalAmount.ToColombianCurrency()}";
            }

            cards.Add(new ActiveOrderCardDto
            {
                Token = link.Token,
                WorkshopName = workshop?.Name ?? "Taller",
                VehiclePlate = order.VehiclePlate,
                VehicleModel = order.VehicleModel,
                CustomerName = order.CustomerName,
                ProgressStateName = order.CurrentProgressState.ToFriendlyName(),
                QuoteStatusName = statusName,
                QuoteBadgeClass = badgeClass,
                CardBorderClass = borderClass,
                CardHeaderClass = headerClass,
                ActionBtnClass = btnClass,
                ActionBtnText = btnText,
                ItemsSummary = itemsSummary,
                AdditionalItemsCount = quote?.Items.Count ?? 0,
                ProposedTotalFormatted = quote != null ? quote.CalculateProposedTotals().TotalAmount.ToColombianCurrency() : "$0",
                HasQuote = quote != null,
                IsAnswered = status == QuoteStatus.Respondida,
                IsExpired = status == QuoteStatus.Vencida
            });
        }

        ActiveCards = cards.OrderBy(c => c.VehiclePlate).ToList();
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(CustomToken))
        {
            return Page();
        }

        return RedirectToPage("/Tracking/Index", new { token = CustomToken.Trim() });
    }

    public async Task<IActionResult> OnPostResetAsync([FromServices] IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetService<MiCarroAlDiaDbContext>();
        if (dbContext != null)
        {
            await PostgresSeeder.ResetAsync(dbContext, _timeProvider);
            TempData["GlobalMessage"] = "Datos de prueba reiniciados con éxito en la base de datos Supabase.";
        }
        else
        {
            var inMemoryDb = serviceProvider.GetService<InMemoryDatabase>();
            inMemoryDb?.Reset(_timeProvider);
            TempData["GlobalMessage"] = "Datos de prueba en memoria reiniciados con éxito.";
        }

        return RedirectToPage("/Index");
    }
}

public class ActiveOrderCardDto
{
    public required string Token { get; init; }
    public required string WorkshopName { get; init; }
    public required string VehiclePlate { get; init; }
    public required string VehicleModel { get; init; }
    public required string CustomerName { get; init; }
    public required string ProgressStateName { get; init; }
    public required string QuoteStatusName { get; init; }
    public required string QuoteBadgeClass { get; init; }
    public required string CardBorderClass { get; init; }
    public required string CardHeaderClass { get; init; }
    public required string ActionBtnClass { get; init; }
    public required string ActionBtnText { get; init; }
    public required string ItemsSummary { get; init; }
    public int AdditionalItemsCount { get; init; }
    public required string ProposedTotalFormatted { get; init; }
    public bool HasQuote { get; init; }
    public bool IsAnswered { get; init; }
    public bool IsExpired { get; init; }
}
