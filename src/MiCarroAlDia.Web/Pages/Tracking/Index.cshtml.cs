using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiCarroAlDia.Application.AdditionalResponses;
using MiCarroAlDia.Application.CustomerTracking;
using MiCarroAlDia.Domain.Enums;

namespace MiCarroAlDia.Web.Pages.Tracking;

public class IndexModel : PageModel
{
    private readonly GetCustomerTrackingUseCase _getTrackingUseCase;
    private readonly SubmitCustomerResponseUseCase _submitResponseUseCase;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        GetCustomerTrackingUseCase getTrackingUseCase,
        SubmitCustomerResponseUseCase submitResponseUseCase,
        ILogger<IndexModel> logger)
    {
        _getTrackingUseCase = getTrackingUseCase;
        _submitResponseUseCase = submitResponseUseCase;
        _logger = logger;
    }

    public CustomerTrackingDto? Tracking { get; private set; }
    public bool IsNotFound { get; private set; }
    public string CurrentToken { get; private set; } = string.Empty;

    [BindProperty]
    public AdditionalResponseInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? token, [FromQuery] string? t)
    {
        var resolvedToken = !string.IsNullOrWhiteSpace(token) ? token : t;
        CurrentToken = resolvedToken ?? string.Empty;

        if (string.IsNullOrWhiteSpace(CurrentToken))
        {
            IsNotFound = true;
            return Page();
        }

        Tracking = await _getTrackingUseCase.ExecuteAsync(CurrentToken);
        if (Tracking == null)
        {
            IsNotFound = true;
            return Page();
        }

        ViewData["WorkshopName"] = Tracking.WorkshopName;
        ViewData["WorkshopPhone"] = Tracking.WorkshopPhone;

        // Inicializar el modelo del formulario si la cotización está pendiente
        if (Tracking.Quote != null && Tracking.Quote.Status == QuoteStatus.Pendiente)
        {
            Input.QuoteId = Tracking.Quote.QuoteId;
            Input.Token = CurrentToken;
            Input.Items = Tracking.Quote.Items.Select(item => new ItemDecisionFormModel
            {
                QuoteItemId = item.Id,
                Description = item.Description,
                IsSafetyCritical = item.IsSafetyCritical,
                TotalAmount = item.TotalAmount,
                Decision = CustomerDecision.SinSeleccionar,
                SecurityConfirmed = false
            }).ToList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? token, [FromQuery] string? t)
    {
        var resolvedToken = !string.IsNullOrWhiteSpace(Input.Token)
            ? Input.Token
            : (!string.IsNullOrWhiteSpace(token) ? token : t);

        CurrentToken = resolvedToken ?? string.Empty;

        if (string.IsNullOrWhiteSpace(CurrentToken))
        {
            IsNotFound = true;
            return Page();
        }

        var command = new SubmitCustomerResponseCommand
        {
            Token = CurrentToken,
            QuoteId = Input.QuoteId,
            Decisions = Input.Items.Select(i => new ItemDecisionInput
            {
                QuoteItemId = i.QuoteItemId,
                Decision = i.Decision,
                SecurityConfirmed = i.SecurityConfirmed
            }).ToList()
        };

        var result = await _submitResponseUseCase.ExecuteAsync(command);

        if (!result.Success)
        {
            var tracking = await _getTrackingUseCase.ExecuteAsync(CurrentToken);
            
            // Si la cotización ya estaba respondida (ej. doble envío o reintento), redirigir al comprobante sin error
            if (tracking?.Quote != null && tracking.Quote.Status == QuoteStatus.Respondida)
            {
                TempData["InfoFeedback"] = "Esta cotización ya cuenta con una respuesta definitiva registrada. A continuación se presenta tu comprobante guardado.";
                return RedirectToPage("/Tracking/Index", new { token = CurrentToken });
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Ocurrió un error al procesar tu respuesta.");
            Tracking = tracking;
            if (tracking != null)
            {
                ViewData["WorkshopName"] = tracking.WorkshopName;
                ViewData["WorkshopPhone"] = tracking.WorkshopPhone;
            }
            return Page();
        }

        TempData["SuccessFeedback"] = "¡Tus decisiones han sido registradas y confirmadas con éxito!";
        return RedirectToPage("/Tracking/Index", new { token = CurrentToken });
    }
}

public class AdditionalResponseInputModel
{
    public string Token { get; set; } = string.Empty;
    public string QuoteId { get; set; } = string.Empty;
    public List<ItemDecisionFormModel> Items { get; set; } = new();
}

public class ItemDecisionFormModel
{
    public string QuoteItemId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSafetyCritical { get; set; }
    public decimal TotalAmount { get; set; }
    public CustomerDecision Decision { get; set; }
    public bool SecurityConfirmed { get; set; }
}
