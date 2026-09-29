namespace MiCarroAlDia.Domain.Rules;

/// <summary>
/// Centraliza el cálculo de precios, IVA del 19% y totales en pesos colombianos sin centavos.
/// Aplica MidpointRounding.AwayFromZero como política de redondeo uniforme.
/// </summary>
public static class PricingCalculator
{
    public const decimal IvaRate = 0.19m;

    public static decimal CalculateBase(int quantity, decimal unitPrice)
    {
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad no puede ser negativa.");
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "El precio unitario no puede ser negativo.");

        return Math.Round(quantity * unitPrice, 0, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateIva(decimal baseAmount)
    {
        if (baseAmount < 0) throw new ArgumentOutOfRangeException(nameof(baseAmount), "La base gravable no puede ser negativa.");

        return Math.Round(baseAmount * IvaRate, 0, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateTotal(decimal baseAmount, decimal ivaAmount)
    {
        return baseAmount + ivaAmount;
    }

    public static (decimal BaseAmount, decimal IvaAmount, decimal TotalAmount) CalculateItemAmounts(int quantity, decimal unitPrice)
    {
        var baseAmount = CalculateBase(quantity, unitPrice);
        var ivaAmount = CalculateIva(baseAmount);
        var totalAmount = CalculateTotal(baseAmount, ivaAmount);
        return (baseAmount, ivaAmount, totalAmount);
    }
}
