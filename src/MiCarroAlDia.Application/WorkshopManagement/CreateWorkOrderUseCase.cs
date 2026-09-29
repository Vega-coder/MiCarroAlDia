using MiCarroAlDia.Application.Abstractions;
using MiCarroAlDia.Domain.Entities;
using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Domain.Exceptions;

namespace MiCarroAlDia.Application.WorkshopManagement;

public class CreateWorkOrderUseCase
{
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ICustomerAccessLinkRepository _accessLinkRepository;
    private readonly TimeProvider _timeProvider;

    public CreateWorkOrderUseCase(
        IWorkshopRepository workshopRepository,
        IWorkOrderRepository workOrderRepository,
        ICustomerAccessLinkRepository accessLinkRepository,
        TimeProvider timeProvider)
    {
        _workshopRepository = workshopRepository;
        _workOrderRepository = workOrderRepository;
        _accessLinkRepository = accessLinkRepository;
        _timeProvider = timeProvider;
    }

    public async Task<(WorkOrder Order, CustomerAccessLink Link)> ExecuteAsync(CreateWorkOrderCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.WorkshopId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.VehiclePlate);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.VehicleModel);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CustomerName);

        var workshop = await _workshopRepository.GetByIdAsync(command.WorkshopId, ct);
        if (workshop == null)
        {
            throw new DomainNotFoundException($"El taller '{command.WorkshopId}' no existe.");
        }

        var normalizedPlate = command.VehiclePlate.Trim().ToUpperInvariant();
        var nowUtc = _timeProvider.GetUtcNow();
        var shortSuffix = Guid.NewGuid().ToString("N")[..6];
        var orderId = $"wo-{normalizedPlate.Replace("-", "").ToLowerInvariant()}-{shortSuffix}";

        var order = new WorkOrder(
            orderId,
            command.WorkshopId,
            normalizedPlate,
            command.VehicleModel.Trim(),
            command.CustomerName.Trim(),
            VehicleProgressState.Recibido,
            nowUtc,
            command.InitialNote ?? "Recepción inicial del vehículo en el taller para revisión."
        );

        var token = $"t-{normalizedPlate.Replace("-", "").ToLowerInvariant()}-{shortSuffix}";
        var link = new CustomerAccessLink(token, command.WorkshopId, order.Id, nowUtc, true);

        await _workOrderRepository.SaveAsync(order, ct);
        await _accessLinkRepository.AddAsync(link, ct);

        return (order, link);
    }
}
