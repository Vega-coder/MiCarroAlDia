using MiCarroAlDia.Domain.Entities;
using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Domain.Rules;
using Xunit;

namespace MiCarroAlDia.Tests;

public class PricingCalculatorTests
{
    [Fact]
    public void CalculateItemAmounts_ComputesBaseIvaAndTotalWithoutCents()
    {
        // 1 item de $200.000 con IVA del 19%
        var (baseAmount, ivaAmount, totalAmount) = PricingCalculator.CalculateItemAmounts(1, 200_000m);

        Assert.Equal(200_000m, baseAmount);
        Assert.Equal(38_000m, ivaAmount);
        Assert.Equal(238_000m, totalAmount);
    }

    [Fact]
    public void CalculateItemAmounts_Plumillas50k_CalculatesCorrectIva()
    {
        // 1 item de $50.000 -> Base $50.000, IVA $9.500, Total $59.500
        var (baseAmount, ivaAmount, totalAmount) = PricingCalculator.CalculateItemAmounts(1, 50_000m);

        Assert.Equal(50_000m, baseAmount);
        Assert.Equal(9_500m, ivaAmount);
        Assert.Equal(59_500m, totalAmount);
    }

    [Fact]
    public void FullExample_CalculatesProposedAndAuthorizedTotals_MatchingSpecExactly()
    {
        // Ejemplo oficial del documento:
        // Ítem 1: Repuesto frenos 1 x $200.000 -> Base $200.000, IVA $38.000, Total $238.000
        // Ítem 2: Mano de obra 1 x $100.000 -> Base $100.000, IVA $19.000, Total $119.000
        // Ítem 3: Plumillas 1 x $50.000 -> Base $50.000, IVA $9.500, Total $59.500

        var item1 = new QuoteItem("1", "Repuesto frenos", ItemType.Repuesto, ItemCategory.Seguridad, 1, 200_000m);
        var item2 = new QuoteItem("2", "Mano de obra frenos", ItemType.ManoDeObra, ItemCategory.Seguridad, 1, 100_000m);
        var item3 = new QuoteItem("3", "Plumillas", ItemType.Repuesto, ItemCategory.General, 1, 50_000m);

        var quote = new AdditionalQuote(
            id: "Q1",
            workOrderId: "WO1",
            publishedAtUtc: DateTimeOffset.UtcNow,
            items: new[] { item1, item2, item3 }
        );

        var (propBase, propIva, propTotal) = quote.CalculateProposedTotals();

        // Total propuesto esperado: Base $350.000, IVA $66.500, Total $416.500
        Assert.Equal(350_000m, propBase);
        Assert.Equal(66_500m, propIva);
        Assert.Equal(416_500m, propTotal);

        // Cliente aprueba ítems 1 y 2, y rechaza ítem 3 (plumillas)
        var decisions = new[]
        {
            ("1", CustomerDecision.Aprobar, false),
            ("2", CustomerDecision.Aprobar, false),
            ("3", CustomerDecision.Rechazar, false)
        };

        quote.SubmitCustomerResponse(decisions, DateTimeOffset.UtcNow);

        Assert.NotNull(quote.Response);
        // Total autorizado esperado: Base $300.000, IVA $57.000, Total $357.000
        Assert.Equal(300_000m, quote.Response.TotalAuthorizedBase);
        Assert.Equal(57_000m, quote.Response.TotalAuthorizedIva);
        Assert.Equal(357_000m, quote.Response.TotalAuthorizedAmount);
    }
}
