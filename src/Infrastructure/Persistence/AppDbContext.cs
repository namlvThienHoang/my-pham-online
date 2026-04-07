namespace BeautyEcommerce.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Common;
using System.Linq.Expressions;

/// <summary>
/// Main database context with all entities and configurations
/// </summary>
public class AppDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    // User & Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<SkinProfile> SkinProfiles => Set<SkinProfile>();

    // Product Catalog
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();

    // Inventory
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    // Cart & Wishlist
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<CartVoucher> CartVouchers => Set<CartVoucher>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    // Order
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Shipment> Shipments => Set<Shipment>();

    // Promotion
    public DbSet<Voucher> Vouchers => Set<Voucher>();

    // Review & Q&A
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ProductQa> ProductQas => Set<ProductQa>();

    // Wallet & Gift Card
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();

    // Alerts & Notifications
    public DbSet<StockAlertSubscription> StockAlertSubscriptions => Set<StockAlertSubscription>();
    public DbSet<CodConfirmAttempt> CodConfirmAttempts => Set<CodConfirmAttempt>();

    // User Activity
    public DbSet<UserRecentlyViewed> UserRecentlyVieweds => Set<UserRecentlyViewed>();

    // Infrastructure
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<OrderSagaState> OrderSagaStates => Set<OrderSagaState>();
    public DbSet<SagaCompensationLog> SagaCompensationLogs => Set<SagaCompensationLog>();

    // Audit & Invoice
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // PostgreSQL ENUM types
        modelBuilder.HasPostgresEnum("order_status", typeof(OrderStatus));
        modelBuilder.HasPostgresEnum("payment_status", typeof(PaymentStatus));
        modelBuilder.HasPostgresEnum("payment_method", typeof(PaymentMethod));
        modelBuilder.HasPostgresEnum("inventory_status", typeof(InventoryStatus));
        modelBuilder.HasPostgresEnum("shipment_status", typeof(ShipmentStatus));
        modelBuilder.HasPostgresEnum("user_role", typeof(UserRole));
        modelBuilder.HasPostgresEnum("gender", typeof(Gender));
        modelBuilder.HasPostgresEnum("skin_type", typeof(SkinType));
        modelBuilder.HasPostgresEnum("voucher_type", typeof(VoucherType));
        modelBuilder.HasPostgresEnum("notification_type", typeof(NotificationType));
        modelBuilder.HasPostgresEnum("return_status", typeof(ReturnStatus));
        modelBuilder.HasPostgresEnum("refund_reason", typeof(RefundReason));
        modelBuilder.HasPostgresEnum("refund_status", typeof(RefundStatus));
        modelBuilder.HasPostgresEnum("stock_movement_type", typeof(StockMovementType));
        modelBuilder.HasPostgresEnum("wallet_transaction_type", typeof(WalletTransactionType));
        modelBuilder.HasPostgresEnum("invoice_status", typeof(InvoiceStatus));
        modelBuilder.HasPostgresEnum("saga_status", typeof(SagaStatus));
        modelBuilder.HasPostgresEnum("address_type", typeof(AddressType));

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.DeletedAt));
                var nullCheck = Expression.Equal(property, Expression.Constant(null, typeof(DateTime?)));
                var lambda = Expression.Lambda(nullCheck, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.RowVersion++;
                    break;
                case EntityState.Deleted:
                    // Convert to soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Service to get current user information
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
