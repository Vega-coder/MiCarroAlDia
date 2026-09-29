using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiCarroAlDia.Application.Abstractions;
using MiCarroAlDia.Infrastructure.Persistence.InMemory;

namespace MiCarroAlDia.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ICustomerAccessLinkRepository _linkRepository;
    private readonly IWorkOrderRepository _orderRepository;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IAdditionalQuoteRepository _quoteRepository;
    private readonly InMemoryDatabase? _inMemoryDb;
    private readonly TimeProvider _timeProvider;

    public IndexModel(
        ICustomerAccessLinkRepository linkRepository,
        IWorkOrderRepository orderRepository,
        IWorkshopRepository workshopRepository,
        IAdditionalQuoteRepository quoteRepository,
        TimeProvider timeProvider,
        InMemoryDatabase? inMemoryDb = null)
    {
        _linkRepository = linkRepository;
        _orderRepository = orderRepository;
        _workshopRepository = workshopRepository;
        _quoteRepository = quoteRepository;
        _timeProvider = timeProvider;
        _inMemoryDb = inMemoryDb;
    }

    [BindProperty]
    public string? CustomToken { get; set; }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(CustomToken))
        {
            return Page();
        }

        return RedirectToPage("/Tracking/Index", new { token = CustomToken.Trim() });
    }

    public IActionResult OnPostReset()
    {
        _inMemoryDb?.Reset(_timeProvider);
        TempData["GlobalMessage"] = "¡Datos de prueba reiniciados correctamente al estado original!";
        return RedirectToPage("/Index");
    }
}
