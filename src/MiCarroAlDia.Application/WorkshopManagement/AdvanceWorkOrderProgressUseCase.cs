using MiCarroAlDia.Application.Abstractions;
using MiCarroAlDia.Domain.Entities;
using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Domain.Exceptions;

namespace MiCarroAlDia.Application.WorkshopManagement;

public class AdvanceWorkOrderProgressUseCase
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly TimeProvider _timeProvider;

    public AdvanceWorkOrderProgressUseCase(
        IWorkOrderRepository workOrderRepository,
        TimeProvider timeProvider)
    {
        _workOrderRepository = workOrderRepository;
        _timeProvider = timeProvider;
    }

    public async Task<WorkOrder> ExecuteAsync(AdvanceProgressCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.WorkOrderId);

        var order = await _workOrderRepository.GetByIdAsync(command.WorkOrderId, ct);
        if (order == null)
        {
            throw new DomainNotFoundException($"No se encontró la orden de trabajo con id '{command.WorkOrderId}'.");
        }

        if ((int)order.CurrentProgressState >= 6)
        {
            throw new DomainValidationException("El vehículo ya alcanzó el estado final 'Entregado' y no puede avanzar más.");
        }

        var nextState = (VehicleProgressState)((int)order.CurrentProgressState + 1);
        var nowUtc = _timeProvider.GetUtcNow();
        var note = !string.IsNullOrWhiteSpace(command.Note) 
            ? command.Note 
            : $"Avance a {nextState.ToFriendlyName().ToLowerInvariant()} registrado por asesor de servicio.";

        order.AdvanceProgress(nextState, nowUtc, note);

        await _workOrderRepository.SaveAsync(order, ct);
        return order;
    }
}
