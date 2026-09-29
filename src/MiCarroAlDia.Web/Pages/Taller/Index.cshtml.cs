using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiCarroAlDia.Application.WorkshopManagement;
using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Domain.Exceptions;

namespace MiCarroAlDia.Web.Pages.Taller;

public class IndexModel : PageModel
{
    private readonly GetWorkshopDashboardUseCase _dashboardUseCase;
    private readonly AdvanceWorkOrderProgressUseCase _advanceProgressUseCase;
    private readonly CreateWorkOrderUseCase _createOrderUseCase;
    private readonly CreateAdditionalQuoteUseCase _createQuoteUseCase;

    public WorkshopDashboardDto Dashboard { get; private set; } = new();

    public IndexModel(
        GetWorkshopDashboardUseCase dashboardUseCase,
        AdvanceWorkOrderProgressUseCase advanceProgressUseCase,
        CreateWorkOrderUseCase createOrderUseCase,
        CreateAdditionalQuoteUseCase createQuoteUseCase)
    {
        _dashboardUseCase = dashboardUseCase;
        _advanceProgressUseCase = advanceProgressUseCase;
        _createOrderUseCase = createOrderUseCase;
        _createQuoteUseCase = createQuoteUseCase;
    }

    public async Task OnGetAsync()
    {
        Dashboard = await _dashboardUseCase.ExecuteAsync("taller-autofrenos");
        ViewData["Title"] = "Panel de Taller — Autofrenos del Norte";
        ViewData["WorkshopName"] = Dashboard.WorkshopName;
        ViewData["WorkshopPhone"] = Dashboard.WorkshopPhone;
    }

    public async Task<IActionResult> OnPostAdvanceProgressAsync(string orderId, string? note)
    {
        try
        {
            var updated = await _advanceProgressUseCase.ExecuteAsync(new AdvanceProgressCommand
            {
                WorkOrderId = orderId,
                Note = note
            });

            TempData["GlobalMessage"] = $"¡Vehículo {updated.VehiclePlate} avanzado a «{updated.CurrentProgressState.ToFriendlyName()}»!";
        }
        catch (DomainException ex)
        {
            TempData["InfoFeedback"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateOrderAsync(string vehiclePlate, string vehicleModel, string customerName, string? initialNote)
    {
        if (string.IsNullOrWhiteSpace(vehiclePlate) || string.IsNullOrWhiteSpace(vehicleModel) || string.IsNullOrWhiteSpace(customerName))
        {
            TempData["InfoFeedback"] = "Debe diligenciar la placa, modelo y nombre del cliente.";
            return RedirectToPage();
        }

        try
        {
            var (order, link) = await _createOrderUseCase.ExecuteAsync(new CreateWorkOrderCommand
            {
                WorkshopId = "taller-autofrenos",
                VehiclePlate = vehiclePlate,
                VehicleModel = vehicleModel,
                CustomerName = customerName,
                InitialNote = initialNote
            });

            TempData["GlobalMessage"] = $"¡Orden para {order.VehiclePlate} creada con éxito! Enlace WhatsApp: /t/{link.Token}";
        }
        catch (DomainException ex)
        {
            TempData["InfoFeedback"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateQuoteAsync(string orderId, string description, int type, int category, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(description) || unitPrice <= 0)
        {
            TempData["InfoFeedback"] = "Debe ingresar una descripción válida y un precio unitario mayor a cero.";
            return RedirectToPage();
        }

        try
        {
            await _createQuoteUseCase.ExecuteAsync(new CreateAdditionalQuoteCommand
            {
                WorkOrderId = orderId,
                Items = new List<CreateQuoteItemDto>
                {
                    new()
                    {
                        Description = description,
                        Type = (ItemType)type,
                        Category = (ItemCategory)category,
                        Quantity = quantity > 0 ? quantity : 1,
                        UnitPrice = unitPrice
                    }
                }
            });

            TempData["GlobalMessage"] = "¡Cotización de adicionales publicada con éxito (plazo de 48h activado)!";
        }
        catch (DomainException ex)
        {
            TempData["InfoFeedback"] = ex.Message;
        }

        return RedirectToPage();
    }
}
