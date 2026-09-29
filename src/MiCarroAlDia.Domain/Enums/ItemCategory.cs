namespace MiCarroAlDia.Domain.Enums;

public enum ItemCategory
{
    General = 1,
    Seguridad = 2 // Frenos, dirección, suspensión, llantas
}

public static class ItemCategoryExtensions
{
    public static bool IsSafetyCritical(this ItemCategory category) => category == ItemCategory.Seguridad;
}
