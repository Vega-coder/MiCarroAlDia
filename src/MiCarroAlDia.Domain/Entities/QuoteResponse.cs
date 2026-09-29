using MiCarroAlDia.Domain.Enums;

namespace MiCarroAlDia.Domain.Entities;

public class QuoteResponse
{
    private readonly List<ItemResponse> _items;

    public string Id { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }
    public IReadOnlyList<ItemResponse> Items => _items.AsReadOnly();

    public decimal TotalProposedBase { get; private set; }
    public decimal TotalProposedIva { get; private set; }
    public decimal TotalProposedAmount { get; private set; }

    public decimal TotalAuthorizedBase { get; private set; }
    public decimal TotalAuthorizedIva { get; private set; }
    public decimal TotalAuthorizedAmount { get; private set; }

    private QuoteResponse() { Id = null!; _items = new(); }

    public QuoteResponse(
        string id,
        DateTimeOffset submittedAtUtc,
        IEnumerable<ItemResponse> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(items);

        Id = id;
        SubmittedAtUtc = submittedAtUtc;
        _items = items.ToList();

        if (_items.Count == 0)
        {
            throw new ArgumentException("Una respuesta de adicionales debe contener al menos un ítem.", nameof(items));
        }

        // Proposed totals (sum of all items)
        TotalProposedBase = _items.Sum(i => i.BaseAmount);
        TotalProposedIva = _items.Sum(i => i.IvaAmount);
        TotalProposedAmount = _items.Sum(i => i.TotalAmount);

        // Authorized totals (sum of only approved items)
        var approved = _items.Where(i => i.Decision == CustomerDecision.Aprobar).ToList();
        TotalAuthorizedBase = approved.Sum(i => i.BaseAmount);
        TotalAuthorizedIva = approved.Sum(i => i.IvaAmount);
        TotalAuthorizedAmount = approved.Sum(i => i.TotalAmount);
    }
}
