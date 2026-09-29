using System.Globalization;

namespace MiCarroAlDia.Application.Common;

public static class FormattingExtensions
{
    private static readonly CultureInfo ColombiaCulture = new("es-CO");
    private static readonly TimeSpan ColombiaOffset = TimeSpan.FromHours(-5);

    public static string ToColombianCurrency(this decimal amount)
    {
        // Pesos colombianos sin centavos, ej: $357.000
        return string.Format(ColombiaCulture, "${0:N0}", amount);
    }

    public static DateTimeOffset ToColombiaTime(this DateTimeOffset utc)
    {
        return utc.ToOffset(ColombiaOffset);
    }

    public static string ToFriendlyColombiaDate(this DateTimeOffset utc)
    {
        var col = utc.ToOffset(ColombiaOffset);
        return col.ToString("d 'de' MMMM 'de' yyyy, hh:mm tt", ColombiaCulture);
    }

    public static string ToShortColombiaDate(this DateTimeOffset utc)
    {
        var col = utc.ToOffset(ColombiaOffset);
        return col.ToString("dd/MM/yyyy hh:mm tt", ColombiaCulture);
    }
}
