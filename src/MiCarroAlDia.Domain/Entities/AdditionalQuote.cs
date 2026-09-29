using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Domain.Exceptions;

namespace MiCarroAlDia.Domain.Entities;

public class AdditionalQuote
{
    private readonly List<QuoteItem> _items = new();

    public string Id { get; private set; }
    public string WorkOrderId { get; private set; }
    public DateTimeOffset PublishedAtUtc { get; private set; }
    public IReadOnlyList<QuoteItem> Items => _items.AsReadOnly();
    public QuoteResponse? Response { get; private set; }

    public DateTimeOffset ExpiresAtUtc => PublishedAtUtc.AddHours(48);

    private AdditionalQuote() { Id = null!; WorkOrderId = null!; }

    public AdditionalQuote(
        string id,
        string workOrderId,
        DateTimeOffset publishedAtUtc,
        IEnumerable<QuoteItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(workOrderId);
        ArgumentNullException.ThrowIfNull(items);

        Id = id;
        WorkOrderId = workOrderId;
        PublishedAtUtc = publishedAtUtc;
        _items.AddRange(items);

        if (_items.Count == 0)
        {
            throw new ArgumentException("Una cotización de adicionales debe tener al menos un ítem.", nameof(items));
        }
    }

    public QuoteStatus GetStatus(DateTimeOffset nowUtc)
    {
        if (Response != null)
        {
            return QuoteStatus.Respondida;
        }

        if (nowUtc >= ExpiresAtUtc)
        {
            return QuoteStatus.Vencida;
        }

        return QuoteStatus.Pendiente;
    }

    public bool IsExpired(DateTimeOffset nowUtc) => nowUtc >= ExpiresAtUtc;

    public (decimal BaseAmount, decimal IvaAmount, decimal TotalAmount) CalculateProposedTotals()
    {
        var baseTotal = _items.Sum(i => i.BaseAmount);
        var ivaTotal = _items.Sum(i => i.IvaAmount);
        var grandTotal = _items.Sum(i => i.TotalAmount);
        return (baseTotal, ivaTotal, grandTotal);
    }

    private readonly object _syncRoot = new();

    public void SubmitCustomerResponse(
        IEnumerable<(string ItemId, CustomerDecision Decision, bool SecurityConfirmed)> itemDecisions,
        DateTimeOffset nowUtc)
    {
        lock (_syncRoot)
        {
            if (Response != null)
            {
                throw new DomainConflictException("Esta cotización de adicionales ya fue respondida previamente y sus decisiones no pueden ser modificadas desde el celular.");
            }

            if (IsExpired(nowUtc))
            {
                throw new DomainValidationException("El plazo de 48 horas para autorizar los adicionales ha vencido. Comunícate directamente con el taller para coordinar.");
            }

            var decisionsList = itemDecisions.ToList();

            // 1. Validar que no existan decisiones duplicadas para un mismo ítem
            if (decisionsList.GroupBy(d => d.ItemId).Any(g => g.Count() > 1))
            {
                throw new DomainValidationException("No se permiten decisiones duplicadas para un mismo ítem.");
            }

            // 2. Validar que no se envíen identificadores ajenos que no pertenezcan a esta cotización
            var foreignItems = decisionsList.Where(d => !_items.Any(i => i.Id == d.ItemId)).ToList();
            if (foreignItems.Count > 0)
            {
                throw new DomainValidationException("La solicitud contiene identificadores de ítems que no corresponden a esta cotización.");
            }

            // 3. Validar que se hayan respondido todos los ítems de la cotización (sin omisiones)
            var missingItems = _items.Where(i => !decisionsList.Any(d => d.ItemId == i.Id)).ToList();
            if (missingItems.Count > 0)
            {
                throw new DomainValidationException("Debes responder todas y cada una de las solicitudes adicionales (aprobar o rechazar cada una).");
            }

            var itemResponses = new List<ItemResponse>();

            foreach (var item in _items)
            {
                var match = decisionsList.First(d => d.ItemId == item.Id);
                if (match.Decision == CustomerDecision.SinSeleccionar)
                {
                    throw new DomainValidationException($"Debes indicar una decisión para el ítem '{item.Description}'.");
                }

                if (match.Decision == CustomerDecision.Rechazar && item.Category.IsSafetyCritical() && !match.SecurityConfirmed)
                {
                    throw new DomainValidationException($"El ítem '{item.Description}' afecta la seguridad del vehículo. Debes confirmar expresamente que entiendes el riesgo para poder rechazarlo.");
                }

                var itemResponse = new ItemResponse(
                    quoteItemId: item.Id,
                    description: item.Description,
                    type: item.Type,
                    category: item.Category,
                    decision: match.Decision,
                    securityRejectionConfirmed: match.SecurityConfirmed,
                    quantity: item.Quantity,
                    unitPrice: item.UnitPrice,
                    baseAmount: item.BaseAmount,
                    ivaAmount: item.IvaAmount,
                    totalAmount: item.TotalAmount
                );

                itemResponses.Add(itemResponse);
            }

            Response = new QuoteResponse(
                id: Guid.NewGuid().ToString("N"),
                submittedAtUtc: nowUtc,
                items: itemResponses
            );
        }
    }

    /// <summary>
    /// Usado para inicializar escenarios semilla ya respondidos.
    /// </summary>
    public void SetSeededResponse(QuoteResponse response)
    {
        Response = response;
    }
}
