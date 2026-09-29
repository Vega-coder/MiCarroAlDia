namespace MiCarroAlDia.Domain.Enums;

public enum VehicleProgressState
{
    Recibido = 1,
    Diagnostico = 2,
    Reparacion = 3,
    ControlDeCalidad = 4,
    ListoParaEntregar = 5,
    Entregado = 6
}

public static class VehicleProgressStateExtensions
{
    public static string ToFriendlyName(this VehicleProgressState state) => state switch
    {
        VehicleProgressState.Recibido => "Recibido",
        VehicleProgressState.Diagnostico => "Diagnóstico",
        VehicleProgressState.Reparacion => "Reparación",
        VehicleProgressState.ControlDeCalidad => "Control de calidad",
        VehicleProgressState.ListoParaEntregar => "Listo para entregar",
        VehicleProgressState.Entregado => "Entregado",
        _ => state.ToString()
    };

    public static string ToFriendlyDescription(this VehicleProgressState state) => state switch
    {
        VehicleProgressState.Recibido => "Tu vehículo ingresó a nuestras instalaciones y se encuentra en registro inicial.",
        VehicleProgressState.Diagnostico => "Nuestros técnicos están inspeccionando detalladamente los sistemas mecánicos.",
        VehicleProgressState.Reparacion => "Los trabajos autorizados están en ejecución activa por el equipo técnico.",
        VehicleProgressState.ControlDeCalidad => "Realizamos pruebas de ruta e inspección final de seguridad para certificar los trabajos.",
        VehicleProgressState.ListoParaEntregar => "¡Tu vehículo está listo para que lo recojas en nuestro taller!",
        VehicleProgressState.Entregado => "Vehículo entregado a satisfacción del cliente.",
        _ => string.Empty
    };
}
