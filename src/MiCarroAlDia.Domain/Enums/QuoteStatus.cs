namespace MiCarroAlDia.Domain.Enums;

public enum QuoteStatus
{
    Pendiente = 1,
    Respondida = 2,
    Vencida = 3
}

public static class QuoteStatusExtensions
{
    public static string ToFriendlyName(this QuoteStatus status) => status switch
    {
        QuoteStatus.Pendiente => "Pendiente por responder",
        QuoteStatus.Respondida => "Respuesta registrada",
        QuoteStatus.Vencida => "Plazo vencido",
        _ => status.ToString()
    };
}
