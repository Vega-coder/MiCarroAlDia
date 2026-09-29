using MiCarroAlDia.Application.Abstractions;
using MiCarroAlDia.Domain.Entities;
using MiCarroAlDia.Domain.Exceptions;

namespace MiCarroAlDia.Infrastructure.Persistence.InMemory;

public class InMemoryWorkshopRepository : IWorkshopRepository
{
    private readonly InMemoryDatabase _db;

    public InMemoryWorkshopRepository(InMemoryDatabase db)
    {
        _db = db;
    }

    public Task<Workshop?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        _db.Workshops.TryGetValue(id, out var workshop);
        return Task.FromResult(workshop);
    }
}

public class InMemoryWorkOrderRepository : IWorkOrderRepository
{
    private readonly InMemoryDatabase _db;

    public InMemoryWorkOrderRepository(InMemoryDatabase db)
    {
        _db = db;
    }

    public Task<WorkOrder?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        _db.WorkOrders.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Task<IReadOnlyList<WorkOrder>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<WorkOrder> list = _db.WorkOrders.Values.ToList();
        return Task.FromResult(list);
    }

    public Task SaveAsync(WorkOrder order, CancellationToken ct = default)
    {
        lock (_db.GetLock())
        {
            _db.WorkOrders[order.Id] = order;
        }
        return Task.CompletedTask;
    }
}

public class InMemoryAdditionalQuoteRepository : IAdditionalQuoteRepository
{
    private readonly InMemoryDatabase _db;

    public InMemoryAdditionalQuoteRepository(InMemoryDatabase db)
    {
        _db = db;
    }

    public Task<AdditionalQuote?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        _db.Quotes.TryGetValue(id, out var quote);
        return Task.FromResult(quote);
    }

    public Task<AdditionalQuote?> GetByWorkOrderIdAsync(string workOrderId, CancellationToken ct = default)
    {
        var quote = _db.Quotes.Values.FirstOrDefault(q => q.WorkOrderId == workOrderId);
        return Task.FromResult(quote);
    }

    public Task SaveResponseAsync(AdditionalQuote quote, CancellationToken ct = default)
    {
        lock (_db.GetLock())
        {
            if (!_db.Quotes.ContainsKey(quote.Id))
            {
                throw new DomainNotFoundException($"No se encontró la cotización con id '{quote.Id}'.");
            }

            _db.Quotes[quote.Id] = quote;
        }

        return Task.CompletedTask;
    }

    public Task AddAsync(AdditionalQuote quote, CancellationToken ct = default)
    {
        lock (_db.GetLock())
        {
            _db.Quotes[quote.Id] = quote;
        }

        return Task.CompletedTask;
    }
}

public class InMemoryCustomerAccessLinkRepository : ICustomerAccessLinkRepository
{
    private readonly InMemoryDatabase _db;

    public InMemoryCustomerAccessLinkRepository(InMemoryDatabase db)
    {
        _db = db;
    }

    public Task<CustomerAccessLink?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        _db.AccessLinks.TryGetValue(token, out var link);
        return Task.FromResult(link);
    }

    public Task<CustomerAccessLink?> GetByWorkOrderIdAsync(string workOrderId, CancellationToken ct = default)
    {
        var link = _db.AccessLinks.Values.FirstOrDefault(l => l.WorkOrderId == workOrderId && l.IsActive);
        return Task.FromResult(link);
    }

    public Task<IReadOnlyList<CustomerAccessLink>> GetAllActiveAsync(CancellationToken ct = default)
    {
        IReadOnlyList<CustomerAccessLink> list = _db.AccessLinks.Values.Where(l => l.IsActive).ToList();
        return Task.FromResult(list);
    }

    public Task AddAsync(CustomerAccessLink link, CancellationToken ct = default)
    {
        lock (_db.GetLock())
        {
            _db.AccessLinks[link.Token] = link;
        }

        return Task.CompletedTask;
    }
}
