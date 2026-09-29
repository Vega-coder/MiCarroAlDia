using Microsoft.AspNetCore.Mvc.Testing;
using MiCarroAlDia.Application.WorkshopManagement;
using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Domain.Exceptions;
using MiCarroAlDia.Infrastructure.Persistence.InMemory;
using System.Net;
using Xunit;

namespace MiCarroAlDia.Tests;

public class WorkshopManagementTests
{
    private readonly InMemoryDatabase _db;
    private readonly InMemoryWorkshopRepository _workshopRepo;
    private readonly InMemoryWorkOrderRepository _orderRepo;
    private readonly InMemoryAdditionalQuoteRepository _quoteRepo;
    private readonly InMemoryCustomerAccessLinkRepository _linkRepo;
    private readonly TestTimeProvider _timeProvider;

    public WorkshopManagementTests()
    {
        _timeProvider = new TestTimeProvider(new DateTimeOffset(2026, 9, 29, 14, 0, 0, TimeSpan.FromHours(-5)));
        _db = new InMemoryDatabase(_timeProvider);
        _workshopRepo = new InMemoryWorkshopRepository(_db);
        _orderRepo = new InMemoryWorkOrderRepository(_db);
        _quoteRepo = new InMemoryAdditionalQuoteRepository(_db);
        _linkRepo = new InMemoryCustomerAccessLinkRepository(_db);
    }

    [Fact]
    public async Task GetWorkshopDashboard_ReturnsCorrectOrdersAndCounters()
    {
        var useCase = new GetWorkshopDashboardUseCase(_workshopRepo, _orderRepo, _quoteRepo, _linkRepo, _timeProvider);

        var result = await useCase.ExecuteAsync("taller-autofrenos");

        Assert.NotNull(result);
        Assert.Equal("Autofrenos del Norte", result.WorkshopName);
        Assert.True(result.TotalActiveOrders >= 4);
        Assert.Contains(result.Orders, o => o.VehiclePlate == "ABC-123");
    }

    [Fact]
    public async Task AdvanceWorkOrderProgress_ValidNextState_AdvancesSuccessfully()
    {
        var useCase = new AdvanceWorkOrderProgressUseCase(_orderRepo, _timeProvider);

        // OT-104 (Kia Picanto) is in ListoParaEntregar (step 5)
        var result = await useCase.ExecuteAsync(new AdvanceProgressCommand
        {
            WorkOrderId = "OT-104",
            Note = "Cliente retira el vehículo a conformidad."
        });

        Assert.Equal(VehicleProgressState.Entregado, result.CurrentProgressState);
    }

    [Fact]
    public async Task AdvanceWorkOrderProgress_WhenAlreadyDelivered_ThrowsDomainValidationException()
    {
        var useCase = new AdvanceWorkOrderProgressUseCase(_orderRepo, _timeProvider);

        // Advance to delivered first
        await useCase.ExecuteAsync(new AdvanceProgressCommand { WorkOrderId = "OT-104" });

        // Attempting to advance delivered order must throw
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            useCase.ExecuteAsync(new AdvanceProgressCommand { WorkOrderId = "OT-104" }));
    }

    [Fact]
    public async Task CreateWorkOrder_CreatesOrderAndWhatsAppLink()
    {
        var useCase = new CreateWorkOrderUseCase(_workshopRepo, _orderRepo, _linkRepo, _timeProvider);

        var (order, link) = await useCase.ExecuteAsync(new CreateWorkOrderCommand
        {
            WorkshopId = "taller-autofrenos",
            VehiclePlate = "FGH-789",
            VehicleModel = "Mazda CX-30 2024",
            CustomerName = "Andrés Restrepo",
            InitialNote = "Revisión de 10.000 km y alineación."
        });

        Assert.NotNull(order);
        Assert.Equal("FGH-789", order.VehiclePlate);
        Assert.Equal(VehicleProgressState.Recibido, order.CurrentProgressState);
        Assert.NotNull(link);
        Assert.StartsWith("t-fgh789", link.Token);
        Assert.True(link.IsActive);
    }

    [Fact]
    public async Task CreateAdditionalQuote_ValidItems_SavesQuoteSuccessfully()
    {
        var useCase = new CreateAdditionalQuoteUseCase(_orderRepo, _quoteRepo, _timeProvider);

        // OT-104 has no quote
        var quote = await useCase.ExecuteAsync(new CreateAdditionalQuoteCommand
        {
            WorkOrderId = "OT-104",
            Items = new List<CreateQuoteItemDto>
            {
                new()
                {
                    Description = "Cambio de líquido de frenos DOT 4",
                    Type = ItemType.Repuesto,
                    Category = ItemCategory.Seguridad,
                    Quantity = 1,
                    UnitPrice = 85000
                }
            }
        });

        Assert.NotNull(quote);
        Assert.Single(quote.Items);
        Assert.Equal(85000, quote.Items[0].BaseAmount);
        Assert.Equal(quote.PublishedAtUtc.AddHours(48), quote.ExpiresAtUtc);
    }

    [Fact]
    public async Task CreateAdditionalQuote_WhenQuoteAlreadyExists_ThrowsDomainConflictException()
    {
        var useCase = new CreateAdditionalQuoteUseCase(_orderRepo, _quoteRepo, _timeProvider);

        // OT-101 already has an active quote
        await Assert.ThrowsAsync<DomainConflictException>(() =>
            useCase.ExecuteAsync(new CreateAdditionalQuoteCommand
            {
                WorkOrderId = "OT-101",
                Items = new List<CreateQuoteItemDto>
                {
                    new() { Description = "Item de prueba", UnitPrice = 10000, Quantity = 1 }
                }
            }));
    }
}

public class WorkshopHttpIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WorkshopHttpIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Taller_ReturnsSuccessAndShowsDashboard()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Taller");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Panel de Gestión de Taller", content);
        Assert.Contains("Autofrenos del Norte", content);
        Assert.Contains("ABC-123", content);
    }

    [Fact]
    public async Task Get_Marcela_RedirectsToTaller()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Marcela");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Taller", response.Headers.Location?.OriginalString);
    }
}
