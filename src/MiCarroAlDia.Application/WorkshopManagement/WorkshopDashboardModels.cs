using MiCarroAlDia.Domain.Enums;

namespace MiCarroAlDia.Application.WorkshopManagement;

public class WorkshopDashboardDto
{
    public string WorkshopId { get; set; } = string.Empty;
    public string WorkshopName { get; set; } = string.Empty;
    public string WorkshopPhone { get; set; } = string.Empty;
    public string WorkshopAddress { get; set; } = string.Empty;

    public int TotalActiveOrders { get; set; }
    public int InProgressCount { get; set; }
    public int PendingQuotesCount { get; set; }
    public int CompletedCount { get; set; }

    public List<WorkshopOrderCardDto> Orders { get; set; } = new();
}

public class WorkshopOrderCardDto
{
    public string WorkOrderId { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    public VehicleProgressState CurrentState { get; set; }
    public int CurrentStepNumber { get; set; }
    public string CurrentStateName { get; set; } = string.Empty;
    public bool CanAdvanceProgress { get; set; }
    public VehicleProgressState? NextState { get; set; }
    public string? NextStateName { get; set; }

    public string? CustomerToken { get; set; }
    public string? CustomerAccessUrl { get; set; }

    // Cotización
    public string? QuoteId { get; set; }
    public QuoteStatus? QuoteStatus { get; set; }
    public string QuoteStatusBadge { get; set; } = "Sin cotización";
    public string? QuoteHoursRemainingText { get; set; }
    public string? TotalProposedCop { get; set; }
    public string? TotalAuthorizedCop { get; set; }
    public int? ItemsApprovedCount { get; set; }
    public int? ItemsRejectedCount { get; set; }
    public string? RespondedAtFormatted { get; set; }
    public bool CanAddQuote { get; set; }

    public List<ProgressHistoryItemDto> History { get; set; } = new();
}

public class ProgressHistoryItemDto
{
    public string StateName { get; set; } = string.Empty;
    public string TimestampFormatted { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public class CreateWorkOrderCommand
{
    public string WorkshopId { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? InitialNote { get; set; }
}

public class AdvanceProgressCommand
{
    public string WorkOrderId { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class CreateAdditionalQuoteCommand
{
    public string WorkOrderId { get; set; } = string.Empty;
    public List<CreateQuoteItemDto> Items { get; set; } = new();
}

public class CreateQuoteItemDto
{
    public string Description { get; set; } = string.Empty;
    public ItemType Type { get; set; }
    public ItemCategory Category { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
