using Microsoft.EntityFrameworkCore;
using MiCarroAlDia.Application.Abstractions;
using MiCarroAlDia.Domain.Entities;
using MiCarroAlDia.Domain.Exceptions;

namespace MiCarroAlDia.Infrastructure.Persistence.Postgres;

public class PostgresWorkshopRepository : IWorkshopRepository
{
    private readonly MiCarroAlDiaDbContext _context;

    public PostgresWorkshopRepository(MiCarroAlDiaDbContext context)
    {
        _context = context;
    }

    public Task<Workshop?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return _context.Workshops.FirstOrDefaultAsync(w => w.Id == id, ct);
    }
}

public class PostgresWorkOrderRepository : IWorkOrderRepository
{
    private readonly MiCarroAlDiaDbContext _context;

    public PostgresWorkOrderRepository(MiCarroAlDiaDbContext context)
    {
        _context = context;
    }

    public Task<WorkOrder?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return _context.WorkOrders.FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task<IReadOnlyList<WorkOrder>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.WorkOrders.ToListAsync(ct);
    }
}

public class PostgresAdditionalQuoteRepository : IAdditionalQuoteRepository
{
    private readonly MiCarroAlDiaDbContext _context;

    public PostgresAdditionalQuoteRepository(MiCarroAlDiaDbContext context)
    {
        _context = context;
    }

    public Task<AdditionalQuote?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return _context.Quotes.FirstOrDefaultAsync(q => q.Id == id, ct);
    }

    public Task<AdditionalQuote?> GetByWorkOrderIdAsync(string workOrderId, CancellationToken ct = default)
    {
        return _context.Quotes.FirstOrDefaultAsync(q => q.WorkOrderId == workOrderId, ct);
    }

    public async Task SaveResponseAsync(AdditionalQuote quote, CancellationToken ct = default)
    {
        var existing = await _context.Quotes.FirstOrDefaultAsync(q => q.Id == quote.Id, ct);
        if (existing == null)
        {
            throw new DomainNotFoundException($"No se encontró la cotización '{quote.Id}'.");
        }

        // Si ya fue rastreado por EF Core, SaveChangesAsync actualiza las columnas JSON
        _context.Entry(existing).CurrentValues.SetValues(quote);
        await _context.SaveChangesAsync(ct);
    }
}

public class PostgresCustomerAccessLinkRepository : ICustomerAccessLinkRepository
{
    private readonly MiCarroAlDiaDbContext _context;

    public PostgresCustomerAccessLinkRepository(MiCarroAlDiaDbContext context)
    {
        _context = context;
    }

    public Task<CustomerAccessLink?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        return _context.AccessLinks.FirstOrDefaultAsync(l => l.Token == token, ct);
    }

    public async Task<IReadOnlyList<CustomerAccessLink>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.AccessLinks.Where(l => l.IsActive).ToListAsync(ct);
    }
}
