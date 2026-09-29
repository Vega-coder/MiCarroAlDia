using MiCarroAlDia.Application.AdditionalResponses;
using MiCarroAlDia.Application.CustomerTracking;
using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Infrastructure.Persistence.InMemory;
using Xunit;

namespace MiCarroAlDia.Tests;

public class ApplicationUseCaseTests
{
    private readonly InMemoryDatabase _db;
    private readonly TestTimeProvider _timeProvider;
    private readonly InMemoryWorkshopRepository _workshopRepo;
    private readonly InMemoryWorkOrderRepository _orderRepo;
    private readonly InMemoryAdditionalQuoteRepository _quoteRepo;
    private readonly InMemoryCustomerAccessLinkRepository _linkRepo;

    public ApplicationUseCaseTests()
    {
        var fixedNow = new DateTimeOffset(2026, 9, 29, 14, 0, 0, TimeSpan.Zero);
        _timeProvider = new TestTimeProvider(fixedNow);
        _db = new InMemoryDatabase(_timeProvider);

        _workshopRepo = new InMemoryWorkshopRepository(_db);
        _orderRepo = new InMemoryWorkOrderRepository(_db);
        _quoteRepo = new InMemoryAdditionalQuoteRepository(_db);
        _linkRepo = new InMemoryCustomerAccessLinkRepository(_db);
    }

    [Fact]
    public async Task GetCustomerTracking_ValidToken_ReturnsCompleteTrackingDto()
    {
        var useCase = new GetCustomerTrackingUseCase(_linkRepo, _orderRepo, _workshopRepo, _quoteRepo, _timeProvider);

        var result = await useCase.ExecuteAsync("demo-activa");

        Assert.NotNull(result);
        Assert.Equal("Autofrenos del Norte", result.WorkshopName);
        Assert.Equal("ABC-123", result.VehiclePlate);
        Assert.Equal("Carlos Mario Gómez", result.CustomerName);
        Assert.Equal(VehicleProgressState.Diagnostico, result.CurrentProgressState);
        Assert.Equal(6, result.ProgressSteps.Count);
        Assert.True(result.HasAdditionalQuote);
        Assert.NotNull(result.Quote);
        Assert.Equal(QuoteStatus.Pendiente, result.Quote.Status);
        Assert.Equal(3, result.Quote.Items.Count);
    }

    [Fact]
    public async Task GetCustomerTracking_InvalidToken_ReturnsNullSafely()
    {
        var useCase = new GetCustomerTrackingUseCase(_linkRepo, _orderRepo, _workshopRepo, _quoteRepo, _timeProvider);

        var result = await useCase.ExecuteAsync("token-que-no-existe-xyz");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCustomerTracking_MultiTenantIsolation_ResolvesOnlyOwningWorkshop()
    {
        var useCase = new GetCustomerTrackingUseCase(_linkRepo, _orderRepo, _workshopRepo, _quoteRepo, _timeProvider);

        var resultTallerSur = await useCase.ExecuteAsync("demo-taller-sur");

        Assert.NotNull(resultTallerSur);
        Assert.Equal("Serviteca y Frenos del Sur", resultTallerSur.WorkshopName);
        Assert.Equal("SUR-555", resultTallerSur.VehiclePlate);
        Assert.NotEqual("Autofrenos del Norte", resultTallerSur.WorkshopName);
    }

    [Fact]
    public async Task SubmitCustomerResponse_ValidDecisions_SavesAtomicallyAndFreezesTotals()
    {
        var submitUseCase = new SubmitCustomerResponseUseCase(_linkRepo, _orderRepo, _quoteRepo, _timeProvider);

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

        var result = await submitUseCase.ExecuteAsync(command);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(357_000m, result.TotalAuthorizedAmount); // 200k + 100k + 19% IVA = 357.000
        Assert.Equal(416_500m, result.TotalProposedAmount);   // 350k + 19% IVA = 416.500

        // Verificar que en el repositorio la cotización ahora tiene estado Respondida
        var savedQuote = await _quoteRepo.GetByIdAsync("COT-101");
        Assert.NotNull(savedQuote);
        Assert.Equal(QuoteStatus.Respondida, savedQuote.GetStatus(_timeProvider.GetUtcNow()));
        Assert.NotNull(savedQuote.Response);
        Assert.Equal(3, savedQuote.Response.Items.Count);
    }

    [Fact]
    public async Task SubmitCustomerResponse_RejectingSafetyWithoutConfirmation_ReturnsFailure()
    {
        var submitUseCase = new SubmitCustomerResponseUseCase(_linkRepo, _orderRepo, _quoteRepo, _timeProvider);

        var command = new SubmitCustomerResponseCommand
        {
            Token = "demo-activa",
            QuoteId = "COT-101",
            Decisions = new List<ItemDecisionInput>
            {
                new() { QuoteItemId = "ITM-101-1", Decision = CustomerDecision.Rechazar, SecurityConfirmed = false }, // FRENOS rechazado sin confirmación
                new() { QuoteItemId = "ITM-101-2", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false },
                new() { QuoteItemId = "ITM-101-3", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false }
            }
        };

        var result = await submitUseCase.ExecuteAsync(command);

        Assert.False(result.Success);
        Assert.Contains("seguridad", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitCustomerResponse_AfterExpiration_ReturnsFailure()
    {
        var submitUseCase = new SubmitCustomerResponseUseCase(_linkRepo, _orderRepo, _quoteRepo, _timeProvider);

        var command = new SubmitCustomerResponseCommand
        {
            Token = "demo-vencida",
            QuoteId = "COT-103",
            Decisions = new List<ItemDecisionInput>
            {
                new() { QuoteItemId = "ITM-103-1", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false },
                new() { QuoteItemId = "ITM-103-2", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false }
            }
        };

        var result = await submitUseCase.ExecuteAsync(command);

        Assert.False(result.Success);
        Assert.Contains("vencido", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitCustomerResponse_AlreadyResponded_ReturnsFailure()
    {
        var submitUseCase = new SubmitCustomerResponseUseCase(_linkRepo, _orderRepo, _quoteRepo, _timeProvider);

        var command = new SubmitCustomerResponseCommand
        {
            Token = "demo-respondida",
            QuoteId = "COT-102",
            Decisions = new List<ItemDecisionInput>
            {
                new() { QuoteItemId = "ITM-102-1", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false },
                new() { QuoteItemId = "ITM-102-2", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false },
                new() { QuoteItemId = "ITM-102-3", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false }
            }
        };

        var result = await submitUseCase.ExecuteAsync(command);

        Assert.False(result.Success);
        Assert.Contains("ya fue respondida", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
