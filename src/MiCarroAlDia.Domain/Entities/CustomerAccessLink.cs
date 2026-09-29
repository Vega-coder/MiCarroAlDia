namespace MiCarroAlDia.Domain.Entities;

public class CustomerAccessLink
{
    public string Token { get; private set; }
    public string WorkshopId { get; private set; }
    public string WorkOrderId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public bool IsActive { get; private set; }

    private CustomerAccessLink() { Token = null!; WorkshopId = null!; WorkOrderId = null!; }

    public CustomerAccessLink(string token, string workshopId, string workOrderId, DateTimeOffset createdAtUtc, bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(workshopId);
        ArgumentException.ThrowIfNullOrWhiteSpace(workOrderId);

        Token = token;
        WorkshopId = workshopId;
        WorkOrderId = workOrderId;
        CreatedAtUtc = createdAtUtc;
        IsActive = isActive;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
