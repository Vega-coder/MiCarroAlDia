using MiCarroAlDia.Domain.Enums;

namespace MiCarroAlDia.Domain.Entities;

public class ItemResponse
{
    public string QuoteItemId { get; private set; }
    public string Description { get; private set; }
    public ItemType Type { get; private set; }
    public ItemCategory Category { get; private set; }
    public CustomerDecision Decision { get; private set; }
    public bool SecurityRejectionConfirmed { get; private set; }

    // Snapshot of amounts at the moment of authorization
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal BaseAmount { get; private set; }
    public decimal IvaAmount { get; private set; }
    public decimal TotalAmount { get; private set; }

    private ItemResponse() { QuoteItemId = null!; Description = null!; }

    public ItemResponse(
        string quoteItemId,
        string description,
        ItemType type,
        ItemCategory category,
        CustomerDecision decision,
        bool securityRejectionConfirmed,
        int quantity,
        decimal unitPrice,
        decimal baseAmount,
        decimal ivaAmount,
        decimal totalAmount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(quoteItemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        QuoteItemId = quoteItemId;
        Description = description;
        Type = type;
        Category = category;
        Decision = decision;
        SecurityRejectionConfirmed = securityRejectionConfirmed;
        Quantity = quantity;
        UnitPrice = unitPrice;
        BaseAmount = baseAmount;
        IvaAmount = ivaAmount;
        TotalAmount = totalAmount;
    }
}
