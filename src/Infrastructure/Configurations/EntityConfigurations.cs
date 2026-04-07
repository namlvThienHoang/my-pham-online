namespace BeautyEcommerce.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BeautyEcommerce.Domain.Entities;

/// <summary>
/// Configuration for OutboxMessage entity with distributed lock support
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.Error)
            .HasMaxLength(4000);

        builder.Property(x => x.WorkerId)
            .HasColumnName("worker_id");

        builder.Property(x => x.LeaseExpiresAt)
            .HasColumnName("lease_expires_at");

        builder.Property(x => x.IsDeadLetter)
            .HasColumnName("is_dead_letter")
            .HasDefaultValue(false);

        // Index for efficient polling with SKIP LOCKED
        builder.HasIndex(x => new { x.ProcessedAt, x.IsDeadLetter })
            .HasDatabaseName("ix_outbox_messages_unprocessed")
            .IsFiltered("processed_at IS NULL AND is_dead_letter = false");

        // Index for lease expiration
        builder.HasIndex(x => x.LeaseExpiresAt)
            .HasDatabaseName("ix_outbox_messages_lease");
    }
}

/// <summary>
/// Configuration for OrderSagaState entity
/// </summary>
public class OrderSagaStateConfiguration : IEntityTypeConfiguration<OrderSagaState>
{
    public void Configure(EntityTypeBuilder<OrderSagaState> builder)
    {
        builder.ToTable("order_saga_state");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.CurrentStep)
            .HasColumnName("current_step")
            .HasMaxLength(100);

        builder.Property(x => x.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(4000);

        builder.HasIndex(x => x.OrderId)
            .HasDatabaseName("ix_order_saga_state_order_id")
            .IsUnique();

        builder.HasOne(x => x.Order)
            .WithOne(x => x.SagaState)
            .HasForeignKey<OrderSagaState>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for SagaCompensationLog entity
/// </summary>
public class SagaCompensationLogConfiguration : IEntityTypeConfiguration<SagaCompensationLog>
{
    public void Configure(EntityTypeBuilder<SagaCompensationLog> builder)
    {
        builder.ToTable("saga_compensation_log");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SagaStateId)
            .HasColumnName("saga_state_id")
            .IsRequired();

        builder.Property(x => x.StepName)
            .HasColumnName("step_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RequestData)
            .HasColumnName("request_data");

        builder.Property(x => x.ResponseData)
            .HasColumnName("response_data");

        builder.Property(x => x.Error)
            .HasColumnName("error")
            .HasMaxLength(4000);

        builder.HasIndex(x => new { x.SagaStateId, x.StepName })
            .HasDatabaseName("ix_saga_compensation_log_saga_step");

        builder.HasOne(x => x.SagaState)
            .WithMany(x => x.CompensationLogs)
            .HasForeignKey(x => x.SagaStateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for User entity
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(32);

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.MfaSecret)
            .HasMaxLength(256);

        builder.HasIndex(x => x.Email)
            .HasDatabaseName("ix_users_email")
            .IsUnique()
            .IsFiltered("deleted_at IS NULL");

        builder.HasMany(x => x.Addresses)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Orders)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Configuration for Product entity with Elasticsearch sync trigger
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Sku)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Price)
            .HasPrecision(18, 2);

        builder.Property(x => x.Cost)
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.Sku)
            .HasDatabaseName("ix_products_sku")
            .IsUnique()
            .IsFiltered("deleted_at IS NULL");

        builder.HasIndex(x => x.Slug)
            .HasDatabaseName("ix_products_slug")
            .IsUnique()
            .IsFiltered("deleted_at IS NULL");

        builder.HasIndex(x => x.CategoryId)
            .HasDatabaseName("ix_products_category_id");

        builder.HasIndex(x => x.IsPublished)
            .HasDatabaseName("ix_products_published")
            .IsFiltered("is_published = true AND deleted_at IS NULL");

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Brand)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Configuration for Order entity with partitioning support
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.CurrencyCode)
            .HasMaxLength(3);

        builder.Property(x => x.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(x => x.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.ShippingAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Total)
            .HasPrecision(18, 2);

        builder.Property(x => x.WalletAmountUsed)
            .HasPrecision(18, 2);

        builder.Property(x => x.GiftCardAmountUsed)
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.OrderNumber)
            .HasDatabaseName("ix_orders_order_number")
            .IsUnique()
            .IsFiltered("deleted_at IS NULL");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_orders_user_id");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_orders_status");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_orders_created_at");

        // Composite index for user order history with cursor pagination
        builder.HasIndex(x => new { x.UserId, x.CreatedAt, x.Id })
            .HasDatabaseName("ix_orders_user_created");

        builder.HasOne(x => x.User)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Configuration for InventoryLot entity with FEFO support
/// </summary>
public class InventoryLotConfiguration : IEntityTypeConfiguration<InventoryLot>
{
    public void Configure(EntityTypeBuilder<InventoryLot> builder)
    {
        builder.ToTable("inventory_lots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LotNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 2);

        builder.HasIndex(x => new { x.ProductId, x.VariantId })
            .HasDatabaseName("ix_inventory_lots_product_variant");

        builder.HasIndex(x => x.ExpiryDate)
            .HasDatabaseName("ix_inventory_lots_expiry");

        // Partial index for available lots
        builder.HasIndex(x => new { x.ProductId, x.VariantId, x.AvailableQuantity })
            .HasDatabaseName("ix_inventory_lots_available")
            .IsFiltered("available_quantity > 0 AND status = 'Available' AND deleted_at IS NULL");

        builder.HasOne(x => x.Product)
            .WithMany(x => x.InventoryLots)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for StockMovement entity with partitioning
/// </summary>
public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.ReferenceType)
            .HasMaxLength(100);

        builder.HasIndex(x => x.ProductId)
            .HasDatabaseName("ix_stock_movements_product_id");

        builder.HasIndex(x => x.ReferenceId)
            .HasDatabaseName("ix_stock_movements_reference");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_stock_movements_created_at");

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for AuditLog entity with partitioning
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.EntityId)
            .HasDatabaseName("ix_audit_logs_entity");

        builder.HasIndex(x => x.EntityType)
            .HasDatabaseName("ix_audit_logs_entity_type");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_audit_logs_created_at");

        builder.HasIndex(x => x.PerformedBy)
            .HasDatabaseName("ix_audit_logs_performed_by");
    }
}
