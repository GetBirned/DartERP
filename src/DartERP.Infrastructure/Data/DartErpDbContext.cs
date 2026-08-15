using DartERP.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Data;

public class DartErpDbContext : DbContext
{
    public DartErpDbContext(DbContextOptions<DartErpDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseOrderStatusHistory> PurchaseOrderStatusHistories => Set<PurchaseOrderStatusHistory>();
    public DbSet<PurchaseOrderAttachment> PurchaseOrderAttachments => Set<PurchaseOrderAttachment>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderStatusHistory> WorkOrderStatusHistories => Set<WorkOrderStatusHistory>();
    public DbSet<SerializedItem> SerializedItems => Set<SerializedItem>();
    public DbSet<QualityInspection> QualityInspections => Set<QualityInspection>();
    public DbSet<Disposition> Dispositions => Set<Disposition>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DartErpDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
