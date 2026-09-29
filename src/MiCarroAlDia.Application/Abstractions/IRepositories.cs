using MiCarroAlDia.Domain.Entities;

namespace MiCarroAlDia.Application.Abstractions;

public interface IWorkshopRepository
{
    Task<Workshop?> GetByIdAsync(string id, CancellationToken ct = default);
}

public interface IWorkOrderRepository
{
    Task<WorkOrder?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<WorkOrder>> GetAllAsync(CancellationToken ct = default);
    Task SaveAsync(WorkOrder order, CancellationToken ct = default);
}

public interface IAdditionalQuoteRepository
{
    Task<AdditionalQuote?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<AdditionalQuote?> GetByWorkOrderIdAsync(string workOrderId, CancellationToken ct = default);
    Task SaveResponseAsync(AdditionalQuote quote, CancellationToken ct = default);
    Task AddAsync(AdditionalQuote quote, CancellationToken ct = default);
}

public interface ICustomerAccessLinkRepository
{
    Task<CustomerAccessLink?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<CustomerAccessLink?> GetByWorkOrderIdAsync(string workOrderId, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerAccessLink>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(CustomerAccessLink link, CancellationToken ct = default);
}
