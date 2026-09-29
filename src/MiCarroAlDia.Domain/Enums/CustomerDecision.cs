namespace MiCarroAlDia.Domain.Enums;

public enum CustomerDecision
{
    SinSeleccionar = 0,
    Aprobar = 1,
    Rechazar = 2
}

public static class CustomerDecisionExtensions
{
    public static string ToFriendlyName(this CustomerDecision decision) => decision switch
    {
        CustomerDecision.Aprobar => "Aprobado",
        CustomerDecision.Rechazar => "Rechazado",
        _ => "Sin seleccionar"
    };
}
