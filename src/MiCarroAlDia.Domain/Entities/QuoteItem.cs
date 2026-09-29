using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Domain.Rules;

namespace MiCarroAlDia.Domain.Entities;

public class QuoteItem
{
    public string Id { get; private set; }
    public string Description { get; private set; }
    public ItemType Type { get; private set; }
    public ItemCategory Category { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal BaseAmount => PricingCalculator.CalculateBase(Quantity, UnitPrice);
    public decimal IvaAmount => PricingCalculator.CalculateIva(BaseAmount);
    public decimal TotalAmount => PricingCalculator.CalculateTotal(BaseAmount, IvaAmount);

    private QuoteItem() { Id = null!; Description = null!; }

    public QuoteItem(
        string id,
        string description,
        ItemType type,
        ItemCategory category,
        int quantity,
        decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad debe ser mayor a cero.");
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "El precio unitario no puede ser negativo.");

        Id = id;
        Description = description;
        Type = type;
        Category = category;
        Quantity = quantity;
        UnitPrice = Math.Round(unitPrice, 0, MidpointRounding.AwayFromZero);
    }
}
