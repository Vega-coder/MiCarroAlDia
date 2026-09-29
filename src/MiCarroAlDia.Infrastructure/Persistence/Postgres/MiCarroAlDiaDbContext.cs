using Microsoft.EntityFrameworkCore;
using MiCarroAlDia.Domain.Entities;

namespace MiCarroAlDia.Infrastructure.Persistence.Postgres;

public class MiCarroAlDiaDbContext : DbContext
{
    public DbSet<Workshop> Workshops => Set<Workshop>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<AdditionalQuote> Quotes => Set<AdditionalQuote>();
    public DbSet<CustomerAccessLink> AccessLinks => Set<CustomerAccessLink>();

    public MiCarroAlDiaDbContext(DbContextOptions<MiCarroAlDiaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Workshop>(b =>
        {
            b.ToTable("workshops");
            b.HasKey(w => w.Id);
            b.Property(w => w.Id).HasMaxLength(100);
            b.Property(w => w.Name).HasMaxLength(200).IsRequired();
            b.Property(w => w.Phone).HasMaxLength(50).IsRequired();
            b.Property(w => w.Address).HasMaxLength(300);
            b.Property(w => w.City).HasMaxLength(100);
        });

        modelBuilder.Entity<WorkOrder>(b =>
        {
            b.ToTable("work_orders");
            b.HasKey(w => w.Id);
            b.Property(w => w.Id).HasMaxLength(100);
            b.Property(w => w.WorkshopId).HasMaxLength(100).IsRequired();
            b.Property(w => w.VehiclePlate).HasMaxLength(20).IsRequired();
            b.Property(w => w.VehicleModel).HasMaxLength(200).IsRequired();
            b.Property(w => w.CustomerName).HasMaxLength(200).IsRequired();
            b.Property(w => w.CurrentProgressState).IsRequired();

            b.OwnsMany(w => w.ProgressHistory, h =>
            {
                h.ToJson("progress_history");
            });
        });

        modelBuilder.Entity<AdditionalQuote>(b =>
        {
            b.ToTable("additional_quotes");
            b.HasKey(q => q.Id);
            b.Property(q => q.Id).HasMaxLength(100);
            b.Property(q => q.WorkOrderId).HasMaxLength(100).IsRequired();
            b.Property(q => q.PublishedAtUtc).IsRequired();

            b.OwnsMany(q => q.Items, it =>
            {
                it.ToJson("items");
            });

            b.OwnsOne(q => q.Response, r =>
            {
                r.ToJson("response");
                r.OwnsMany(x => x.Items);
            });
        });

        modelBuilder.Entity<CustomerAccessLink>(b =>
        {
            b.ToTable("customer_access_links");
            b.HasKey(l => l.Token);
            b.Property(l => l.Token).HasMaxLength(100);
            b.Property(l => l.WorkshopId).HasMaxLength(100).IsRequired();
            b.Property(l => l.WorkOrderId).HasMaxLength(100).IsRequired();
            b.Property(l => l.CreatedAtUtc).IsRequired();
            b.Property(l => l.IsActive).IsRequired();
        });
    }
}
