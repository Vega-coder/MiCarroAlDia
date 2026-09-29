using MiCarroAlDia.Domain.Entities;
using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Domain.Exceptions;
using Xunit;

namespace MiCarroAlDia.Tests;

public class AdditionalQuoteDomainTests
{
    private readonly DateTimeOffset _baseTime = new(2026, 9, 25, 8, 0, 0, TimeSpan.Zero); // Viernes 8:00 AM UTC

    private AdditionalQuote CreateSampleQuote()
    {
        var itemFrenos = new QuoteItem("item-1", "Pastillas de freno", ItemType.Repuesto, ItemCategory.Seguridad, 1, 200_000m);
        var itemPlumillas = new QuoteItem("item-2", "Plumillas", ItemType.Repuesto, ItemCategory.General, 1, 50_000m);

        return new AdditionalQuote(
            id: "COT-1",
            workOrderId: "OT-1",
            publishedAtUtc: _baseTime,
            items: new[] { itemFrenos, itemPlumillas }
        );
    }

    [Fact]
    public void RejectingSafetyItem_WithoutConfirmation_ThrowsDomainValidationException()
    {
        var quote = CreateSampleQuote();
        var decisions = new[]
        {
            ("item-1", CustomerDecision.Rechazar, false), // Rechazo de seguridad SIN confirmación
            ("item-2", CustomerDecision.Aprobar, false)
        };

        var ex = Assert.Throws<DomainValidationException>(() =>
            quote.SubmitCustomerResponse(decisions, _baseTime.AddHours(2)));

        Assert.Contains("seguridad", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectingSafetyItem_WithExplicitConfirmation_Succeeds()
    {
        var quote = CreateSampleQuote();
        var decisions = new[]
        {
            ("item-1", CustomerDecision.Rechazar, true), // Rechazo de seguridad CON confirmación explícita
            ("item-2", CustomerDecision.Aprobar, false)
        };

        quote.SubmitCustomerResponse(decisions, _baseTime.AddHours(2));

        Assert.NotNull(quote.Response);
        var safetyResp = quote.Response.Items.First(i => i.QuoteItemId == "item-1");
        Assert.Equal(CustomerDecision.Rechazar, safetyResp.Decision);
        Assert.True(safetyResp.SecurityRejectionConfirmed);
    }

    [Fact]
    public void RejectingGeneralItem_DoesNotRequireSafetyConfirmation()
    {
        var quote = CreateSampleQuote();
        var decisions = new[]
        {
            ("item-1", CustomerDecision.Aprobar, false),
            ("item-2", CustomerDecision.Rechazar, false) // Plumillas (General) no requiere confirmación
        };

        quote.SubmitCustomerResponse(decisions, _baseTime.AddHours(2));

        Assert.NotNull(quote.Response);
        var plumillasResp = quote.Response.Items.First(i => i.QuoteItemId == "item-2");
        Assert.Equal(CustomerDecision.Rechazar, plumillasResp.Decision);
        Assert.False(plumillasResp.SecurityRejectionConfirmed);
    }

    [Fact]
    public void Expiration_48HoursContinuous_IncludesWeekend()
    {
        var quote = CreateSampleQuote();

        // 48 horas continuas desde viernes 8:00 AM vencen exactamente domingo 8:00 AM
        Assert.Equal(_baseTime.AddHours(48), quote.ExpiresAtUtc);

        // A las 47h y 59m: sigue vigente
        var rightBeforeExpiry = _baseTime.AddHours(47).AddMinutes(59);
        Assert.False(quote.IsExpired(rightBeforeExpiry));
        Assert.Equal(QuoteStatus.Pendiente, quote.GetStatus(rightBeforeExpiry));

        // A las 48h exactas o después: vencida
        var rightAtExpiry = _baseTime.AddHours(48);
        Assert.True(quote.IsExpired(rightAtExpiry));
        Assert.Equal(QuoteStatus.Vencida, quote.GetStatus(rightAtExpiry));

        var afterExpiry = _baseTime.AddHours(49);
        Assert.True(quote.IsExpired(afterExpiry));
        Assert.Equal(QuoteStatus.Vencida, quote.GetStatus(afterExpiry));
    }

    [Fact]
    public void SubmittingResponse_After48Hours_ThrowsDomainValidationException()
    {
        var quote = CreateSampleQuote();
        var decisions = new[]
        {
            ("item-1", CustomerDecision.Aprobar, false),
            ("item-2", CustomerDecision.Aprobar, false)
        };

        var expiredTime = _baseTime.AddHours(48).AddSeconds(1);

        var ex = Assert.Throws<DomainValidationException>(() =>
            quote.SubmitCustomerResponse(decisions, expiredTime));

        Assert.Contains("vencido", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OnceResponded_QuoteRemainsRespondida_EvenAfter48Hours()
    {
        var quote = CreateSampleQuote();
        var decisions = new[]
        {
            ("item-1", CustomerDecision.Aprobar, false),
            ("item-2", CustomerDecision.Aprobar, false)
        };

        // Respondida a las 10 horas
        quote.SubmitCustomerResponse(decisions, _baseTime.AddHours(10));
        Assert.Equal(QuoteStatus.Respondida, quote.GetStatus(_baseTime.AddHours(10)));

        // Transcurridas 72 horas (>48h)
        var laterTime = _baseTime.AddHours(72);
        Assert.Equal(QuoteStatus.Respondida, quote.GetStatus(laterTime));
    }

    [Fact]
    public void SubmittingSecondTime_ThrowsDomainConflictException()
    {
        var quote = CreateSampleQuote();
        var decisions = new[]
        {
            ("item-1", CustomerDecision.Aprobar, false),
            ("item-2", CustomerDecision.Aprobar, false)
        };

        quote.SubmitCustomerResponse(decisions, _baseTime.AddHours(5));

        // Segundo intento de envío
        var ex = Assert.Throws<DomainConflictException>(() =>
            quote.SubmitCustomerResponse(decisions, _baseTime.AddHours(6)));

        Assert.Contains("ya fue respondida", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
