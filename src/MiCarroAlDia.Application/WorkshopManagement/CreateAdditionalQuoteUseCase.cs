using MiCarroAlDia.Application.Abstractions;
using MiCarroAlDia.Domain.Entities;
using MiCarroAlDia.Domain.Exceptions;

namespace MiCarroAlDia.Application.WorkshopManagement;

public class CreateAdditionalQuoteUseCase
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IAdditionalQuoteRepository _quoteRepository;
    private readonly TimeProvider _timeProvider;

    public CreateAdditionalQuoteUseCase(
        IWorkOrderRepository workOrderRepository,
        IAdditionalQuoteRepository quoteRepository,
        TimeProvider timeProvider)
    {
        _workOrderRepository = workOrderRepository;
        _quoteRepository = quoteRepository;
        _timeProvider = timeProvider;
    }

    public async Task<AdditionalQuote> ExecuteAsync(CreateAdditionalQuoteCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.WorkOrderId);

        if (command.Items == null || command.Items.Count == 0)
        {
            throw new DomainValidationException("Debe agregar al menos un ítem a la cotización de adicionales.");
        }

        var order = await _workOrderRepository.GetByIdAsync(command.WorkOrderId, ct);
        if (order == null)
        {
            throw new DomainNotFoundException($"La orden de trabajo '{command.WorkOrderId}' no existe.");
        }

        var existingQuote = await _quoteRepository.GetByWorkOrderIdAsync(command.WorkOrderId, ct);
        if (existingQuote != null)
        {
            throw new DomainConflictException("Ya existe una cotización emitida para esta orden de trabajo.");
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var shortSuffix = Guid.NewGuid().ToString("N")[..6];
        var quoteId = $"quote-{order.VehiclePlate.Replace("-", "").ToLowerInvariant()}-{shortSuffix}";

        var quoteItems = command.Items.Select((item, index) => new QuoteItem(
            $"item-{shortSuffix}-{index + 1}",
            item.Description.Trim(),
            item.Type,
            item.Category,
            item.Quantity > 0 ? item.Quantity : 1,
            item.UnitPrice
        )).ToList();

        var quote = new AdditionalQuote(quoteId, order.Id, nowUtc, quoteItems);

        await _quoteRepository.AddAsync(quote, ct);
        return quote;
    }
}
