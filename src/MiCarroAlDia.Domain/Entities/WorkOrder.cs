using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Domain.Exceptions;

namespace MiCarroAlDia.Domain.Entities;

public class WorkOrder
{
    private readonly List<VehicleProgressHistory> _progressHistory = new();

    public string Id { get; private set; }
    public string WorkshopId { get; private set; }
    public string VehiclePlate { get; private set; }
    public string VehicleModel { get; private set; }
    public string CustomerName { get; private set; }
    public VehicleProgressState CurrentProgressState { get; private set; }
    public IReadOnlyList<VehicleProgressHistory> ProgressHistory => _progressHistory.AsReadOnly();

    private WorkOrder() 
    { 
        Id = null!; 
        WorkshopId = null!; 
        VehiclePlate = null!; 
        VehicleModel = null!; 
        CustomerName = null!; 
    }

    public WorkOrder(
        string id,
        string workshopId,
        string vehiclePlate,
        string vehicleModel,
        string customerName,
        VehicleProgressState initialProgressState,
        DateTimeOffset createdAtUtc,
        string? initialNote = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(workshopId);
        ArgumentException.ThrowIfNullOrWhiteSpace(vehiclePlate);
        ArgumentException.ThrowIfNullOrWhiteSpace(vehicleModel);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);

        Id = id;
        WorkshopId = workshopId;
        VehiclePlate = vehiclePlate;
        VehicleModel = vehicleModel;
        CustomerName = customerName;
        CurrentProgressState = initialProgressState;

        _progressHistory.Add(new VehicleProgressHistory(
            initialProgressState,
            createdAtUtc,
            initialNote ?? $"Ingreso del vehículo registrado en estado {initialProgressState.ToFriendlyName().ToLowerInvariant()}."
        ));
    }

    public void AdvanceProgress(VehicleProgressState nextState, DateTimeOffset timestampUtc, string note)
    {
        // Enforce strict sequential progression
        if ((int)nextState != (int)CurrentProgressState + 1)
        {
            throw new DomainValidationException(
                $"Transición de estado inválida: no se puede pasar de '{CurrentProgressState.ToFriendlyName()}' a '{nextState.ToFriendlyName()}'. El flujo debe seguir la secuencia exacta establecida.");
        }

        CurrentProgressState = nextState;
        _progressHistory.Add(new VehicleProgressHistory(nextState, timestampUtc, note));
    }
}
