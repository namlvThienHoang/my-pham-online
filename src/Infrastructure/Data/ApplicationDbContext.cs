namespace BeautyCommerce.Infrastructure.Data;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using BeautyCommerce.Domain.Entities;
using BeautyCommerce.Domain.Enums;

/// <summary>
/// Application DbContext with soft delete, row versioning, and audit support
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserProvider? _currentUserProvider;
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserProvider currentUserProvider)
        : base(options)
    {
        _currentUserProvider = currentUserProvider;
    }

    // User & Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<SkinProfile> SkinProfiles => Set<SkinProfile>();

    // Product Catalog
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductQa> ProductQas => Set<ProductQa>();

    // Inventory
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    // Cart & Wishlist
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    // Orders
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    // Promotions
    public DbSet<Voucher> Vouchers => Set<Voucher>();

    // Reviews
    public DbSet<Review> Reviews => Set<Review>();

    // Wallet & Gift Cards
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();

    // Infrastructure (Outbox, Saga, Audit)
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<OrderSagaState> OrderSagaStates => Set<OrderSagaState>();
    public DbSet<SagaCompensationLog> SagaCompensationLogs => Set<SagaCompensationLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Other
    public DbSet<StockAlertSubscription> StockAlertSubscriptions => Set<StockAlertSubscription>();
    public DbSet<CodConfirmAttempt> CodConfirmAttempts => Set<CodConfirmAttempt>();
    public DbSet<UserRecentlyViewed> UserRecentlyViewed => Set<UserRecentlyViewed>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure PostgreSQL enums
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(OrderStatus))
                    property.SetColumnType("order_status");
                else if (property.ClrType == typeof(PaymentStatus))
                    property.SetColumnType("payment_status");
                else if (property.ClrType == typeof(PaymentMethod))
                    property.SetColumnType("payment_method");
                else if (property.ClrType == typeof(InventoryStatus))
                    property.SetColumnType("inventory_status");
                else if (property.ClrType == typeof(ShipmentStatus))
                    property.SetColumnType("shipment_status");
                else if (property.ClrType == typeof(UserRole))
                    property.SetColumnType("user_role");
                else if (property.ClrType == typeof(SagaStatus))
                    property.SetColumnType("saga_status");
            }
        }

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.DeletedAt));
                var nullConstant = Expression.Constant(null, typeof(DateTime?));
                var condition = Expression.Equal(property, nullConstant);
                var lambda = Expression.Lambda(condition, parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        var userId = _currentUserProvider?.GetCurrentUserId();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.RowVersion = 0;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.RowVersion++;
            }
        }
    }
}

/// <summary>
/// Interface to get current user context
/// </summary>
public interface ICurrentUserProvider
{
    Guid? GetCurrentUserId();
    string? GetCurrentUserEmail();
}
