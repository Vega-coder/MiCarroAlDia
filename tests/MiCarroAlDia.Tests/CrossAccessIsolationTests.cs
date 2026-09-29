using MiCarroAlDia.Application.AdditionalResponses;
using MiCarroAlDia.Application.CustomerTracking;
using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Infrastructure.Persistence.InMemory;
using Xunit;

namespace MiCarroAlDia.Tests;

public class CrossAccessIsolationTests
{
    private readonly InMemoryDatabase _db;
    private readonly TestTimeProvider _timeProvider;
    private readonly InMemoryWorkshopRepository _workshopRepo;
    private readonly InMemoryWorkOrderRepository _orderRepo;
    private readonly InMemoryAdditionalQuoteRepository _quoteRepo;
    private readonly InMemoryCustomerAccessLinkRepository _linkRepo;

    public CrossAccessIsolationTests()
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
    public async Task TokenForCarA_CannotSubmitForQuoteOfCarB_SameWorkshop()
    {
        var useCase = new SubmitCustomerResponseUseCase(_linkRepo, _orderRepo, _quoteRepo, _timeProvider);

        // El token "demo-activa" pertenece al Renault Sandero (OT-101).
        // Intentamos enviar respuestas a la cotización "COT-102" (Chevrolet Onix, OT-102) del MISMO taller.
        var crossAttackCommand = new SubmitCustomerResponseCommand
        {
            Token = "demo-activa",
            QuoteId = "COT-102", // Cotización ajena del mismo taller
            Decisions = new List<ItemDecisionInput>
            {
                new() { QuoteItemId = "ITM-102-1", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false },
                new() { QuoteItemId = "ITM-102-2", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false },
                new() { QuoteItemId = "ITM-102-3", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false }
            }
        };

        var result = await useCase.ExecuteAsync(crossAttackCommand);

        Assert.False(result.Success);
        Assert.Contains("no corresponde al vehículo asignado", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TokenForCarA_CannotSubmitForQuoteOfDifferentWorkshop()
    {
        var useCase = new SubmitCustomerResponseUseCase(_linkRepo, _orderRepo, _quoteRepo, _timeProvider);

        // Token de Autofrenos del Norte intentando responder orden de Serviteca del Sur
        var crossWorkshopCommand = new SubmitCustomerResponseCommand
        {
            Token = "demo-activa",
            QuoteId = "COT-201-INEXISTENTE",
            Decisions = new List<ItemDecisionInput>()
        };

        var result = await useCase.ExecuteAsync(crossWorkshopCommand);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task TokenForCarA_OnlyResolvesCarA_NeverCarB()
    {
        var trackingUseCase = new GetCustomerTrackingUseCase(_linkRepo, _orderRepo, _workshopRepo, _quoteRepo, _timeProvider);

        var tracking = await trackingUseCase.ExecuteAsync("demo-activa");

        Assert.NotNull(tracking);
        Assert.Equal("ABC-123", tracking.VehiclePlate);
        Assert.Equal("Renault Sandero Dynamique 1.6", tracking.VehicleModel);
        Assert.Equal("Carlos Mario Gómez", tracking.CustomerName);
        Assert.Equal("Autofrenos del Norte", tracking.WorkshopName);
    }
}
