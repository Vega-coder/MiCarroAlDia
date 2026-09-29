namespace MiCarroAlDia.Domain.Enums;

public enum ItemType
{
    Repuesto = 1,
    ManoDeObra = 2
}

public static class ItemTypeExtensions
{
    public static string ToFriendlyName(this ItemType type) => type switch
    {
        ItemType.Repuesto => "Repuesto",
        ItemType.ManoDeObra => "Mano de obra",
        _ => type.ToString()
    };
}
