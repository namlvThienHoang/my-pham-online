namespace BeautyEcommerce.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Common;
using System.Reflection;

public class AppDbContext : DbContext, IUnitOfWork
{
    private readonly ICurrentUserService? _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    // Auth & Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Product Catalog
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductQuestion> ProductQuestions => Set<ProductQuestion>();

    // Inventory
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();

    // Cart & Order
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    // Payment
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();

    // Shipment
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentTracking> ShipmentTrackings => Set<ShipmentTracking>();

    // Promotion
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<FlashSale> FlashSales => Set<FlashSale>();

    // Review
    public DbSet<Review> Reviews => Set<Review>();

    // User features
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<SkinProfile> SkinProfiles => Set<SkinProfile>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<RecentlyViewed> RecentlyVieweds => Set<RecentlyViewed>();
    public DbSet<StockAlertSubscription> StockAlertSubscriptions => Set<StockAlertSubscription>();

    // Wallet & Gift Card
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();

    // Return & Refund
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();

    // Notification
    public DbSet<Notification> Notifications => Set<Notification>();

    // Audit & System
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<OrderSagaState> OrderSagaStates => Set<OrderSagaState>();
    public DbSet<SagaCompensationLog> SagaCompensationLogs => Set<SagaCompensationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Apply global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDelete.DeletedAt));
                var nullConstant = Expression.Constant(null);
                var lambda = Expression.Lambda(Expression.Equal(property, nullConstant), parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditInfo()
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        var currentUserId = _currentUserService?.UserId;
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                if (currentUserId.HasValue)
                    entry.Entity.CreatedBy = currentUserId.Value;
            }
            
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                if (currentUserId.HasValue)
                    entry.Entity.UpdatedBy = currentUserId.Value;
            }
        }
    }

    // Unit of Work implementation
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.RollbackTransactionAsync(cancellationToken);
    }
}
