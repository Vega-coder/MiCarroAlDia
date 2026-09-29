using MiCarroAlDia.Application.AdditionalResponses;
using MiCarroAlDia.Domain.Entities;
using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Domain.Exceptions;
using MiCarroAlDia.Infrastructure.Persistence.InMemory;
using Xunit;

namespace MiCarroAlDia.Tests;

public class SubmissionIntegrityAndConcurrencyTests
{
    private readonly InMemoryDatabase _db;
    private readonly TestTimeProvider _timeProvider;
    private readonly InMemoryWorkshopRepository _workshopRepo;
    private readonly InMemoryWorkOrderRepository _orderRepo;
    private readonly InMemoryAdditionalQuoteRepository _quoteRepo;
    private readonly InMemoryCustomerAccessLinkRepository _linkRepo;

    public SubmissionIntegrityAndConcurrencyTests()
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
    public async Task TwoSimultaneousSubmissions_OnlyOneSucceeds_SecondIsRejectedAndNoPartialWrites()
    {
        var useCase = new SubmitCustomerResponseUseCase(_linkRepo, _orderRepo, _quoteRepo, _timeProvider);

        var commandA = new SubmitCustomerResponseCommand
        {
            Token = "demo-activa",
            QuoteId = "COT-101",
            Decisions = new List<ItemDecisionInput>
            {
                new() { QuoteItemId = "ITM-101-1", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false },
                new() { QuoteItemId = "ITM-101-2", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false },
                new() { QuoteItemId = "ITM-101-3", Decision = CustomerDecision.Aprobar, SecurityConfirmed = false }
            }
        };

        var commandB = new SubmitCustomerResponseCommand
        {
            Token = "demo-activa",
            QuoteId = "COT-101",
            Decisions = new List<ItemDecisionInput>
            {
                new() { QuoteItemId = "ITM-101-1", Decision = CustomerDecision.Rechazar, SecurityConfirmed = true },
                new() { QuoteItemId = "ITM-101-2", Decision = CustomerDecision.Rechazar, SecurityConfirmed = true },
                new() { QuoteItemId = "ITM-101-3", Decision = CustomerDecision.Rechazar, SecurityConfirmed = false }
            }
        };

        // Ejecutar 2 envíos de forma concurrente
        var taskA = Task.Run(() => useCase.ExecuteAsync(commandA));
        var taskB = Task.Run(() => useCase.ExecuteAsync(commandB));

        var results = await Task.WhenAll(taskA, taskB);

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count(r => !r.Success);

        // Exactamente uno debe tener éxito y el otro debe fallar por conflicto
        Assert.Equal(1, successCount);
        Assert.Equal(1, failureCount);

        var failedResult = results.First(r => !r.Success);
        Assert.Contains("ya fue respondida", failedResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Verificar que en la base de datos hay una única respuesta coherente y sin escrituras parciales
        var quote = await _quoteRepo.GetByIdAsync("COT-101");
        Assert.NotNull(quote);
        Assert.NotNull(quote.Response);
        Assert.Equal(3, quote.Response.Items.Count);
    }

    [Fact]
    public void DuplicateItemIds_ThrowsDomainValidationException()
    {
        var item1 = new QuoteItem("ITM-1", "Pastillas", ItemType.Repuesto, ItemCategory.Seguridad, 1, 100_000m);
        var item2 = new QuoteItem("ITM-2", "Mano de obra", ItemType.ManoDeObra, ItemCategory.General, 1, 50_000m);
        var quote = new AdditionalQuote("COT-TEST", "OT-1", _timeProvider.GetUtcNow(), new[] { item1, item2 });

        var duplicatedDecisions = new[]
        {
            ("ITM-1", CustomerDecision.Aprobar, false),
            ("ITM-1", CustomerDecision.Rechazar, false) // Duplicado de ITM-1
        };

        var ex = Assert.Throws<DomainValidationException>(() =>
            quote.SubmitCustomerResponse(duplicatedDecisions, _timeProvider.GetUtcNow()));

        Assert.Contains("duplicadas", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OmittedItemIds_ThrowsDomainValidationException()
    {
        var item1 = new QuoteItem("ITM-1", "Pastillas", ItemType.Repuesto, ItemCategory.Seguridad, 1, 100_000m);
        var item2 = new QuoteItem("ITM-2", "Mano de obra", ItemType.ManoDeObra, ItemCategory.General, 1, 50_000m);
        var quote = new AdditionalQuote("COT-TEST", "OT-1", _timeProvider.GetUtcNow(), new[] { item1, item2 });

        var incompleteDecisions = new[]
        {
            ("ITM-1", CustomerDecision.Aprobar, false)
            // Se omitió ITM-2
        };

        var ex = Assert.Throws<DomainValidationException>(() =>
            quote.SubmitCustomerResponse(incompleteDecisions, _timeProvider.GetUtcNow()));

        Assert.Contains("todas y cada una", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ForeignItemIds_ThrowsDomainValidationException()
    {
        var item1 = new QuoteItem("ITM-1", "Pastillas", ItemType.Repuesto, ItemCategory.Seguridad, 1, 100_000m);
        var item2 = new QuoteItem("ITM-2", "Mano de obra", ItemType.ManoDeObra, ItemCategory.General, 1, 50_000m);
        var quote = new AdditionalQuote("COT-TEST", "OT-1", _timeProvider.GetUtcNow(), new[] { item1, item2 });

        var foreignDecisions = new[]
        {
            ("ITM-1", CustomerDecision.Aprobar, false),
            ("ITM-AJENO-999", CustomerDecision.Aprobar, false) // Identificador de otra cotización
        };

        var ex = Assert.Throws<DomainValidationException>(() =>
            quote.SubmitCustomerResponse(foreignDecisions, _timeProvider.GetUtcNow()));

        Assert.Contains("no corresponden", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClientTampering_ServerCalculatesAmounts_IgnoringManipulatedClientValues()
    {
        var useCase = new SubmitCustomerResponseUseCase(_linkRepo, _orderRepo, _quoteRepo, _timeProvider);

        // El cliente solo envía id, decisión y confirmación de seguridad.
        // Los precios no se reciben del navegador; se obtienen del servidor.
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

        var result = await useCase.ExecuteAsync(command);

        Assert.True(result.Success);
        // Debe calcular exactamente con los precios de servidor:
        // ITM-101-1: 200k + IVA 38k = 238k
        // ITM-101-2: 100k + IVA 19k = 119k
        // Total autorizado = 357.000
        Assert.Equal(357_000m, result.TotalAuthorizedAmount);

        var quote = await _quoteRepo.GetByIdAsync("COT-101");
        Assert.NotNull(quote?.Response);
        var item1 = quote.Response.Items.First(i => i.QuoteItemId == "ITM-101-1");
        Assert.Equal(200_000m, item1.UnitPrice);
        Assert.Equal(200_000m, item1.BaseAmount);
        Assert.Equal(38_000m, item1.IvaAmount);
        Assert.Equal(238_000m, item1.TotalAmount);
    }
}
