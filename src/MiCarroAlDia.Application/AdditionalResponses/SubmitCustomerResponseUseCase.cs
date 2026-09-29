using MiCarroAlDia.Application.Abstractions;
using MiCarroAlDia.Application.Common;
using MiCarroAlDia.Domain.Exceptions;

namespace MiCarroAlDia.Application.AdditionalResponses;

public class SubmitCustomerResponseUseCase
{
    private readonly ICustomerAccessLinkRepository _linkRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IAdditionalQuoteRepository _quoteRepository;
    private readonly TimeProvider _timeProvider;

    public SubmitCustomerResponseUseCase(
        ICustomerAccessLinkRepository linkRepository,
        IWorkOrderRepository workOrderRepository,
        IAdditionalQuoteRepository quoteRepository,
        TimeProvider timeProvider)
    {
        _linkRepository = linkRepository;
        _workOrderRepository = workOrderRepository;
        _quoteRepository = quoteRepository;
        _timeProvider = timeProvider;
    }

    public async Task<SubmitCustomerResponseResult> ExecuteAsync(
        SubmitCustomerResponseCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return SubmitCustomerResponseResult.Failure("El enlace de acceso proporcionado es inválido o no existe.");
        }

        var link = await _linkRepository.GetByTokenAsync(command.Token.Trim(), ct);
        if (link == null || !link.IsActive)
        {
            return SubmitCustomerResponseResult.Failure("El enlace de acceso ha expirado o no es válido.");
        }

        var workOrder = await _workOrderRepository.GetByIdAsync(link.WorkOrderId, ct);
        if (workOrder == null || workOrder.WorkshopId != link.WorkshopId)
        {
            return SubmitCustomerResponseResult.Failure("No se encontró la orden de trabajo asociada a este enlace.");
        }

        var quote = await _quoteRepository.GetByIdAsync(command.QuoteId, ct);
        if (quote == null || quote.WorkOrderId != workOrder.Id)
        {
            return SubmitCustomerResponseResult.Failure("La cotización solicitada no corresponde al vehículo asignado.");
        }

        var nowUtc = _timeProvider.GetUtcNow();

        try
        {
            var domainDecisions = command.Decisions.Select(d => (
                d.QuoteItemId,
                d.Decision,
                d.SecurityConfirmed
            ));

            quote.SubmitCustomerResponse(domainDecisions, nowUtc);

            // Persistencia atómica de la respuesta
            await _quoteRepository.SaveResponseAsync(quote, ct);

            var resp = quote.Response!;
            return SubmitCustomerResponseResult.Ok(
                quoteId: quote.Id,
                submittedAtFormatted: resp.SubmittedAtUtc.ToFriendlyColombiaDate(),
                totalAuthorizedAmount: resp.TotalAuthorizedAmount,
                totalAuthorizedAmountFormatted: resp.TotalAuthorizedAmount.ToColombianCurrency(),
                totalProposedAmount: resp.TotalProposedAmount,
                totalProposedAmountFormatted: resp.TotalProposedAmount.ToColombianCurrency()
            );
        }
        catch (DomainException ex)
        {
            return SubmitCustomerResponseResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return SubmitCustomerResponseResult.Failure($"Ocurrió un error al procesar tu respuesta: {ex.Message}");
        }
    }
}
