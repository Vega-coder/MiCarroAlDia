using MiCarroAlDia.Application.AdditionalResponses;
using MiCarroAlDia.Application.CustomerTracking;
using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Infrastructure.Persistence.InMemory;
using Xunit;

namespace MiCarroAlDia.Tests;

public class ReloadAndProofStatusTests
{
    private readonly InMemoryDatabase _db;
    private readonly TestTimeProvider _timeProvider;
    private readonly InMemoryWorkshopRepository _workshopRepo;
    private readonly InMemoryWorkOrderRepository _orderRepo;
    private readonly InMemoryAdditionalQuoteRepository _quoteRepo;
    private readonly InMemoryCustomerAccessLinkRepository _linkRepo;

    public ReloadAndProofStatusTests()
    {
        var fixedNow = new DateTimeOffset(2026, 9, 29, 10, 0, 0, TimeSpan.Zero);
        _timeProvider = new TestTimeProvider(fixedNow);
        _db = new InMemoryDatabase(_timeProvider);

        _workshopRepo = new InMemoryWorkshopRepository(_db);
        _orderRepo = new InMemoryWorkOrderRepository(_db);
        _quoteRepo = new InMemoryAdditionalQuoteRepository(_db);
        _linkRepo = new InMemoryCustomerAccessLinkRepository(_db);
    }

    [Fact]
    public async Task ReloadingAfterSubmission_ReturnsProofWithoutErrors()
    {
        var trackingUseCase = new GetCustomerTrackingUseCase(_linkRepo, _orderRepo, _workshopRepo, _quoteRepo, _timeProvider);
        var submitUseCase = new SubmitCustomerResponseUseCase(_linkRepo, _orderRepo, _quoteRepo, _timeProvider);

        // 1. Antes de responder: estado Pendiente
        var initialTracking = await trackingUseCase.ExecuteAsync("demo-activa");
        Assert.NotNull(initialTracking?.Quote);
        Assert.Equal(QuoteStatus.Pendiente, initialTracking.Quote.Status);

        // 2. Enviar respuesta
        var command = new SubmitCustomerResponseCommand
        {
            Token = "demo-activa",
            QuoteId = "COT-101",
            Decisions = new List<ItemDecisionInput>
            {
                new() { QuoteItemId = "ITM-101-1", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false },
                new() { QuoteItemId = "ITM-101-2", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false },
                new() { QuoteItemId = "ITM-101-3", Decision = CustomerDecision.Rechazar, SecurityConfirmed = false }
            }
        };
        var submitResult = await submitUseCase.ExecuteAsync(command);
        Assert.True(submitResult.Success);

        // 3. Simular recarga (GET) posterior
        var reloadedTracking = await trackingUseCase.ExecuteAsync("demo-activa");
        Assert.NotNull(reloadedTracking?.Quote);
        Assert.Equal(QuoteStatus.Respondida, reloadedTracking.Quote.Status);
        Assert.NotNull(reloadedTracking.Quote.Response);
        Assert.Equal(357_000m, reloadedTracking.Quote.Response.TotalAuthorizedAmount);
        Assert.False(string.IsNullOrWhiteSpace(reloadedTracking.Quote.Response.SubmittedAtFormatted));
    }

    [Fact]
    public async Task AnsweredQuote_RemainsRespondida_EvenAfter48HoursPass()
    {
        var trackingUseCase = new GetCustomerTrackingUseCase(_linkRepo, _orderRepo, _workshopRepo, _quoteRepo, _timeProvider);

        // demo-respondida fue respondida hace horas.
        // Avanzar el reloj 100 horas adicionales (muy por encima de las 48 horas continuas)
        _timeProvider.Advance(TimeSpan.FromHours(100));

        var tracking = await trackingUseCase.ExecuteAsync("demo-respondida");

        Assert.NotNull(tracking?.Quote);
        // Debe permanecer como Respondida y nunca pasar a Vencida
        Assert.Equal(QuoteStatus.Respondida, tracking.Quote.Status);
        Assert.NotNull(tracking.Quote.Response);
    }

    [Fact]
    public async Task SecondSubmission_DoesNotModifyOriginalResponse()
    {
        var submitUseCase = new SubmitCustomerResponseUseCase(_linkRepo, _orderRepo, _quoteRepo, _timeProvider);

        // Primer envío
        var firstCommand = new SubmitCustomerResponseCommand
        {
            Token = "demo-activa",
            QuoteId = "COT-101",
            Decisions = new List<ItemDecisionInput>
            {
                new() { QuoteItemId = "ITM-101-1", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false },
                new() { QuoteItemId = "ITM-101-2", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false },
                new() { QuoteItemId = "ITM-101-3", Decision = CustomerDecision.Rechazar, SecurityConfirmed = false }
            }
        };

        var firstResult = await submitUseCase.ExecuteAsync(firstCommand);
        Assert.True(firstResult.Success);

        var quoteAfterFirst = await _quoteRepo.GetByIdAsync("COT-101");
        var originalSubmittedAt = quoteAfterFirst!.Response!.SubmittedAtUtc;
        var originalAuthorized = quoteAfterFirst.Response.TotalAuthorizedAmount;

        // Avanzar 2 horas e intentar segundo envío con cambios
        _timeProvider.Advance(TimeSpan.FromHours(2));

        var secondCommand = new SubmitCustomerResponseCommand
        {
            Token = "demo-activa",
            QuoteId = "COT-101",
            Decisions = new List<ItemDecisionInput>
            {
                new() { QuoteItemId = "ITM-101-1", Decision = CustomerDecision.Rechazar, SecurityConfirmed = true },
                new() { QuoteItemId = "ITM-101-2", Decision = CustomerDecision.Rechazar, SecurityConfirmed = true },
                new() { QuoteItemId = "ITM-101-3", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false }
            }
        };

        var secondResult = await submitUseCase.ExecuteAsync(secondCommand);

        // El segundo envío debe ser rechazado
        Assert.False(secondResult.Success);
        Assert.Contains("ya fue respondida", secondResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Los datos guardados originalmente permanecen intactos
        var quoteAfterSecond = await _quoteRepo.GetByIdAsync("COT-101");
        Assert.Equal(originalSubmittedAt, quoteAfterSecond!.Response!.SubmittedAtUtc);
        Assert.Equal(originalAuthorized, quoteAfterSecond.Response.TotalAuthorizedAmount);
    }
}
