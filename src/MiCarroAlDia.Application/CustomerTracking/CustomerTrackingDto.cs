using MiCarroAlDia.Domain.Enums;

namespace MiCarroAlDia.Application.CustomerTracking;

public class CustomerTrackingDto
{
    public required string WorkshopName { get; init; }
    public required string WorkshopPhone { get; init; }
    public required string WorkshopAddress { get; init; }
    public required string WorkshopCity { get; init; }

    public required string VehiclePlate { get; init; }
    public required string VehicleModel { get; init; }
    public required string CustomerName { get; init; }

    public VehicleProgressState CurrentProgressState { get; init; }
    public required string CurrentProgressStateName { get; init; }
    public required string CurrentProgressStateDescription { get; init; }

    public required IReadOnlyList<VehicleProgressStepDto> ProgressSteps { get; init; }

    public bool HasAdditionalQuote { get; init; }
    public AdditionalQuoteDetailsDto? Quote { get; init; }
}

public class VehicleProgressStepDto
{
    public VehicleProgressState State { get; init; }
    public required string Name { get; init; }
    public bool IsCompleted { get; init; }
    public bool IsCurrent { get; init; }
    public string? ReachedAtFormatted { get; init; }
    public string? Note { get; init; }
}

public class AdditionalQuoteDetailsDto
{
    public required string QuoteId { get; init; }
    public QuoteStatus Status { get; init; }
    public required string StatusFriendlyName { get; init; }
    public required string PublishedAtFormatted { get; init; }
    public required string ExpiresAtFormatted { get; init; }
    public bool IsExpired { get; init; }
    public required string RemainingTimeText { get; init; }

    public decimal TotalProposedBase { get; init; }
    public required string TotalProposedBaseFormatted { get; init; }
    public decimal TotalProposedIva { get; init; }
    public required string TotalProposedIvaFormatted { get; init; }
    public decimal TotalProposedAmount { get; init; }
    public required string TotalProposedAmountFormatted { get; init; }

    public required IReadOnlyList<QuoteItemDisplayDto> Items { get; init; }
    public QuoteResponseDisplayDto? Response { get; init; }
}

public class QuoteItemDisplayDto
{
    public required string Id { get; init; }
    public required string Description { get; init; }
    public ItemType Type { get; init; }
    public required string TypeName { get; init; }
    public ItemCategory Category { get; init; }
    public bool IsSafetyCritical { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public required string UnitPriceFormatted { get; init; }
    public decimal BaseAmount { get; init; }
    public required string BaseAmountFormatted { get; init; }
    public decimal IvaAmount { get; init; }
    public required string IvaAmountFormatted { get; init; }
    public decimal TotalAmount { get; init; }
    public required string TotalAmountFormatted { get; init; }
}

public class QuoteResponseDisplayDto
{
    public required string SubmittedAtFormatted { get; init; }
    public decimal TotalAuthorizedBase { get; init; }
    public required string TotalAuthorizedBaseFormatted { get; init; }
    public decimal TotalAuthorizedIva { get; init; }
    public required string TotalAuthorizedIvaFormatted { get; init; }
    public decimal TotalAuthorizedAmount { get; init; }
    public required string TotalAuthorizedAmountFormatted { get; init; }
    public required IReadOnlyList<ItemResponseDisplayDto> Items { get; init; }
}

public class ItemResponseDisplayDto
{
    public required string QuoteItemId { get; init; }
    public required string Description { get; init; }
    public required string TypeName { get; init; }
    public bool IsSafetyCritical { get; init; }
    public CustomerDecision Decision { get; init; }
    public required string DecisionFriendlyName { get; init; }
    public bool SecurityRejectionConfirmed { get; init; }
    public int Quantity { get; init; }
    public required string UnitPriceFormatted { get; init; }
    public required string TotalAmountFormatted { get; init; }
}
