using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Entities;
using ECommerce.Infrastructure.Persistence.Interceptors;

namespace ECommerce.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly AuditLogInterceptor _auditLogInterceptor;
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, AuditLogInterceptor auditLogInterceptor)
        : base(options)
    {
        _auditLogInterceptor = auditLogInterceptor;
    }
    
    // Admin & Auth
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    
    // Customers & CRM
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerSegment> CustomerSegments => Set<CustomerSegment>();
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();
    public DbSet<CustomerTag> CustomerTags => Set<CustomerTag>();
    
    // Products & Inventory
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();
    
    // Orders
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Refund> Refunds => Set<Refund>();
    
    // Marketing
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<FlashSale> FlashSales => Set<FlashSale>();
    
    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        // Materialized view for order summaries (query only, not mapped)
        modelBuilder.Entity<OrderSummary>()
            .HasNoKey()
            .ToView(null);
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditLogInterceptor);
        base.OnConfiguring(optionsBuilder);
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update UpdatedAt timestamp
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified);
        
        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        
        return await base.SaveChangesAsync(cancellationToken);
    }
    
    // Method to execute raw SQL for materialized view refresh
    public async Task RefreshMaterializedViewsAsync(CancellationToken cancellationToken = default)
    {
        await Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW CONCURRENTLY order_summaries", cancellationToken);
    }
    
    // Method to query materialized view
    public IQueryable<OrderSummary> OrderSummaries => 
        Set<OrderSummary>().FromSqlRaw(@"
            SELECT 
                o.id as OrderId,
                o.order_number as OrderNumber,
                o.customer_id as CustomerId,
                COALESCE(c.first_name || ' ' || c.last_name, c.email) as CustomerName,
                o.status as Status,
                o.total_amount as TotalAmount,
                o.created_at as CreatedAt,
                COUNT(oi.id) as ItemCount
            FROM orders o
            INNER JOIN customers c ON o.customer_id = c.id
            LEFT JOIN order_items oi ON o.id = oi.order_id
            WHERE o.is_deleted = false
            GROUP BY o.id, o.order_number, o.customer_id, c.first_name, c.last_name, c.email, o.status, o.total_amount, o.created_at
        ");
}
