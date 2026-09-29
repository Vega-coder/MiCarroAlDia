using MiCarroAlDia.Domain.Entities;
using MiCarroAlDia.Domain.Enums;
using MiCarroAlDia.Domain.Exceptions;
using Xunit;

namespace MiCarroAlDia.Tests;

public class WorkOrderDomainTests
{
    [Fact]
    public void WorkOrder_AdvancesProgress_InExactSequentialOrder()
    {
        var now = DateTimeOffset.UtcNow;
        var order = new WorkOrder(
            id: "OT-1",
            workshopId: "W1",
            vehiclePlate: "ABC-123",
            vehicleModel: "Renault Sandero",
            customerName: "Carlos Gómez",
            initialProgressState: VehicleProgressState.Recibido,
            createdAtUtc: now
        );

        Assert.Equal(VehicleProgressState.Recibido, order.CurrentProgressState);
        Assert.Single(order.ProgressHistory);

        // 1 -> 2 Diagnostico
        order.AdvanceProgress(VehicleProgressState.Diagnostico, now.AddHours(1), "Diagnóstico iniciado.");
        Assert.Equal(VehicleProgressState.Diagnostico, order.CurrentProgressState);

        // 2 -> 3 Reparacion
        order.AdvanceProgress(VehicleProgressState.Reparacion, now.AddHours(2), "Reparación iniciada.");
        Assert.Equal(VehicleProgressState.Reparacion, order.CurrentProgressState);

        // 3 -> 4 ControlDeCalidad
        order.AdvanceProgress(VehicleProgressState.ControlDeCalidad, now.AddHours(3), "Pruebas de ruta.");
        Assert.Equal(VehicleProgressState.ControlDeCalidad, order.CurrentProgressState);

        // 4 -> 5 ListoParaEntregar
        order.AdvanceProgress(VehicleProgressState.ListoParaEntregar, now.AddHours(4), "Vehículo lavado.");
        Assert.Equal(VehicleProgressState.ListoParaEntregar, order.CurrentProgressState);

        // 5 -> 6 Entregado
        order.AdvanceProgress(VehicleProgressState.Entregado, now.AddHours(5), "Vehículo entregado.");
        Assert.Equal(VehicleProgressState.Entregado, order.CurrentProgressState);
        Assert.Equal(6, order.ProgressHistory.Count);
    }

    [Fact]
    public void WorkOrder_AttemptingToSkipState_ThrowsDomainValidationException()
    {
        var now = DateTimeOffset.UtcNow;
        var order = new WorkOrder(
            id: "OT-1",
            workshopId: "W1",
            vehiclePlate: "ABC-123",
            vehicleModel: "Renault Sandero",
            customerName: "Carlos Gómez",
            initialProgressState: VehicleProgressState.Recibido,
            createdAtUtc: now
        );

        // Intentar saltar de Recibido (1) directamente a Reparacion (3)
        var ex = Assert.Throws<DomainValidationException>(() =>
            order.AdvanceProgress(VehicleProgressState.Reparacion, now.AddHours(1), "Salto indebido"));

        Assert.Contains("secuencia exacta", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
