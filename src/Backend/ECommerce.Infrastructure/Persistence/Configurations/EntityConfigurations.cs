using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ECommerce.Core.Entities;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .HasColumnName("row_version");
        
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);
        
        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");
        
        // Global query filter for soft delete
        builder.HasQueryFilter(e => !e.IsDeleted);
        
        // Index for soft delete and common queries
        builder.HasIndex(e => e.IsDeleted);
        builder.HasIndex(e => e.CreatedAt);
    }
}

public class AdminUserConfiguration : BaseEntityConfiguration<AdminUser>
{
    public override void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        base.Configure(builder);
        
        builder.ToTable("admin_users");
        
        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("email");
        
        builder.HasIndex(e => e.Email).IsUnique();
        
        builder.Property(e => e.PasswordHash)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("password_hash");
        
        builder.Property(e => e.FirstName)
            .HasMaxLength(50)
            .HasColumnName("first_name");
        
        builder.Property(e => e.LastName)
            .HasMaxLength(50)
            .HasColumnName("last_name");
        
        builder.Property(e => e.IsMfaEnabled)
            .HasColumnName("is_mfa_enabled")
            .HasDefaultValue(false);
        
        builder.Property(e => e.MfaSecret)
            .HasColumnName("mfa_secret");
        
        builder.Property(e => e.MfaBackupCodes)
            .HasColumnName("mfa_backup_codes")
            .HasColumnType("jsonb");
        
        builder.Property(e => e.LastMfaVerification)
            .HasColumnName("last_mfa_verification");
        
        builder.Property(e => e.IpWhitelist)
            .HasColumnName("ip_whitelist")
            .HasColumnType("jsonb");
        
        builder.Property(e => e.Role)
            .HasMaxLength(50)
            .HasColumnName("role")
            .HasDefaultValue("Admin");
        
        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);
    }
}

public class CustomerConfiguration : BaseEntityConfiguration<Customer>
{
    public override void Configure(EntityTypeBuilder<Customer> builder)
    {
        base.Configure(builder);
        
        builder.ToTable("customers");
        
        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("email");
        
        builder.HasIndex(e => e.Email).IsUnique();
        
        builder.Property(e => e.FirstName)
            .HasMaxLength(50)
            .HasColumnName("first_name");
        
        builder.Property(e => e.LastName)
            .HasMaxLength(50)
            .HasColumnName("last_name");
        
        builder.Property(e => e.Phone)
            .HasMaxLength(20)
            .HasColumnName("phone");
        
        builder.Property(e => e.DateOfBirth)
            .HasColumnName("date_of_birth");
        
        builder.Property(e => e.TotalSpent)
            .HasColumnName("total_spent")
            .HasDefaultValue(0);
        
        builder.Property(e => e.OrderCount)
            .HasColumnName("order_count")
            .HasDefaultValue(0);
        
        builder.Property(e => e.LastOrderDate)
            .HasColumnName("last_order_date");
        
        // Indexes for segmentation queries
        builder.HasIndex(e => e.TotalSpent);
        builder.HasIndex(e => e.OrderCount);
        builder.HasIndex(e => e.LastOrderDate);
    }
}

public class CustomerSegmentConfiguration : BaseEntityConfiguration<CustomerSegment>
{
    public override void Configure(EntityTypeBuilder<CustomerSegment> builder)
    {
        base.Configure(builder);
        
        builder.ToTable("customer_segments");
        
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("name");
        
        builder.Property(e => e.Description)
            .HasMaxLength(500)
            .HasColumnName("description");
        
        builder.Property(e => e.Rules)
            .IsRequired()
            .HasColumnName("rules")
            .HasColumnType("jsonb");
        
        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);
        
        builder.HasIndex(e => e.IsActive);
    }
}

public class CustomerNoteConfiguration : BaseEntityConfiguration<CustomerNote>
{
    public override void Configure(EntityTypeBuilder<CustomerNote> builder)
    {
        base.Configure(builder);
        
        builder.ToTable("customer_notes");
        
        builder.Property(e => e.CustomerId)
            .IsRequired()
            .HasColumnName("customer_id");
        
        builder.Property(e => e.CreatedByAdminId)
            .IsRequired()
            .HasColumnName("created_by_admin_id");
        
        builder.Property(e => e.Content)
            .IsRequired()
            .HasColumnName("content");
        
        builder.Property(e => e.IsPrivate)
            .HasColumnName("is_private")
            .HasDefaultValue(true);
        
        builder.HasOne(e => e.Customer)
            .WithMany(c => c.Notes)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(e => e.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(e => e.CustomerId);
        builder.HasIndex(e => e.CreatedAt);
    }
}

public class CustomerTagConfiguration : BaseEntityConfiguration<CustomerTag>
{
    public override void Configure(EntityTypeBuilder<CustomerTag> builder)
    {
        base.Configure(builder);
        
        builder.ToTable("customer_tags");
        
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("name");
        
        builder.Property(e => e.Color)
            .HasMaxLength(100)
            .HasColumnName("color");
        
        builder.HasIndex(e => e.Name).IsUnique();
    }
}

public class ProductConfiguration : BaseEntityConfiguration<Product>
{
    public override void Configure(EntityTypeBuilder<Product> builder)
    {
        base.Configure(builder);
        
        builder.ToTable("products");
        
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");
        
        builder.Property(e => e.Description)
            .HasMaxLength(500)
            .HasColumnName("description");
        
        builder.Property(e => e.Sku)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("sku");
        
        builder.HasIndex(e => e.Sku).IsUnique();
        
        builder.Property(e => e.Price)
            .HasColumnName("price")
            .HasPrecision(18, 2);
        
        builder.Property(e => e.CompareAtPrice)
            .HasColumnName("compare_at_price")
            .HasPrecision(18, 2);
        
        builder.Property(e => e.StockQuantity)
            .HasColumnName("stock_quantity")
            .HasDefaultValue(0);
        
        builder.Property(e => e.LowStockThreshold)
            .HasColumnName("low_stock_threshold")
            .HasDefaultValue(10);
        
        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);
        
        builder.Property(e => e.IsFeatured)
            .HasColumnName("is_featured")
            .HasDefaultValue(false);
        
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.IsFeatured);
        builder.HasIndex(e => e.StockQuantity);
    }
}

public class InventoryLotConfiguration : BaseEntityConfiguration<InventoryLot>
{
    public override void Configure(EntityTypeBuilder<InventoryLot> builder)
    {
        base.Configure(builder);
        
        builder.ToTable("inventory_lots");
        
        builder.Property(e => e.ProductId)
            .IsRequired()
            .HasColumnName("product_id");
        
        builder.Property(e => e.BatchNumber)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("batch_number");
        
        builder.Property(e => e.Quantity)
            .IsRequired()
            .HasColumnName("quantity");
        
        builder.Property(e => e.ReservedQuantity)
            .IsRequired()
            .HasColumnName("reserved_quantity")
            .HasDefaultValue(0);
        
        builder.Property(e => e.ManufactureDate)
            .IsRequired()
            .HasColumnName("manufacture_date");
        
        builder.Property(e => e.ExpiryDate)
            .IsRequired()
            .HasColumnName("expiry_date");
        
        builder.Property(e => e.UnitCost)
            .IsRequired()
            .HasColumnName("unit_cost")
            .HasPrecision(18, 2);
        
        builder.HasOne(e => e.Product)
            .WithMany(p => p.InventoryLots)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.ExpiryDate);
        builder.HasIndex(e => e.BatchNumber);
    }
}

public class OrderConfiguration : BaseEntityConfiguration<Order>
{
    public override void Configure(EntityTypeBuilder<Order> builder)
    {
        base.Configure(builder);
        
        builder.ToTable("orders");
        
        builder.Property(e => e.OrderNumber)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("order_number");
        
        builder.HasIndex(e => e.OrderNumber).IsUnique();
        
        builder.Property(e => e.CustomerId)
            .IsRequired()
            .HasColumnName("customer_id");
        
        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("status")
            .HasDefaultValue("Pending");
        
        builder.Property(e => e.PaymentMethod)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("payment_method");
        
        builder.Property(e => e.PaymentStatus)
            .HasMaxLength(50)
            .HasColumnName("payment_status");
        
        builder.Property(e => e.Subtotal)
            .IsRequired()
            .HasColumnName("subtotal")
            .HasPrecision(18, 2);
        
        builder.Property(e => e.TaxAmount)
            .IsRequired()
            .HasColumnName("tax_amount")
            .HasPrecision(18, 2);
        
        builder.Property(e => e.ShippingCost)
            .IsRequired()
            .HasColumnName("shipping_cost")
            .HasPrecision(18, 2);
        
        builder.Property(e => e.DiscountAmount)
            .IsRequired()
            .HasColumnName("discount_amount")
            .HasPrecision(18, 2);
        
        builder.Property(e => e.TotalAmount)
            .IsRequired()
            .HasColumnName("total_amount")
            .HasPrecision(18, 2);
        
        builder.Property(e => e.ShippingAddress)
            .HasMaxLength(500)
            .HasColumnName("shipping_address");
        
        builder.Property(e => e.InternalNotes)
            .HasMaxLength(500)
            .HasColumnName("internal_notes");
        
        builder.Property(e => e.ConfirmedAt)
            .HasColumnName("confirmed_at");
        
        builder.Property(e => e.ShippedAt)
            .HasColumnName("shipped_at");
        
        builder.Property(e => e.DeliveredAt)
            .HasColumnName("delivered_at");
        
        builder.Property(e => e.CancelledAt)
            .HasColumnName("cancelled_at");
        
        builder.HasOne(e => e.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes for filtering and sorting
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CustomerId);
        builder.HasIndex(e => e.PaymentMethod);
        builder.HasIndex(e => e.CreatedAt);
    }
}

public class OrderItemConfiguration : BaseEntityConfiguration<OrderItem>
{
    public override void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        base.Configure(builder);
        
        builder.ToTable("order_items");
        
        builder.Property(e => e.OrderId)
            .IsRequired()
            .HasColumnName("order_id");
        
        builder.Property(e => e.ProductId)
            .IsRequired()
            .HasColumnName("product_id");
        
        builder.Property(e => e.Quantity)
            .IsRequired()
            .HasColumnName("quantity");
        
        builder.Property(e => e.UnitPrice)
            .IsRequired()
            .HasColumnName("unit_price")
            .HasPrecision(18, 2);
        
        builder.Property(e => e.TotalPrice)
            .IsRequired()
            .HasColumnName("total_price")
            .HasPrecision(18, 2);
        
        builder.HasOne(e => e.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(e => e.OrderId);
        builder.HasIndex(e => e.ProductId);
    }
}

public class RefundConfiguration : BaseEntityConfiguration<Refund>
{
    public override void Configure(EntityTypeBuilder<Refund> builder)
    {
        base.Configure(builder);
        
        builder.ToTable("refunds");
        
        builder.Property(e => e.OrderId)
            .IsRequired()
            .HasColumnName("order_id");
        
        builder.Property(e => e.Amount)
            .IsRequired()
            .HasColumnName("amount")
            .HasPrecision(18, 2);
        
        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("reason");
        
        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("status")
            .HasDefaultValue("Pending");
        
        builder.Property(e => e.AdminNotes)
            .HasMaxLength(500)
            .HasColumnName("admin_notes");
        
        builder.HasOne(e => e.Order)
            .WithMany(o => o.Refunds)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(e => e.OrderId);
        builder.HasIndex(e => e.Status);
    }
}

public class VoucherConfiguration : BaseEntityConfiguration<Voucher>
{
    public override void Configure(EntityTypeBuilder<Voucher> builder)
    {
        base.Configure(builder);
        
        builder.ToTable("vouchers");
        
        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("code");
        
        builder.HasIndex(e => e.Code).IsUnique();
        
        builder.Property(e => e.Description)
            .HasMaxLength(200)
            .HasColumnName("description");
        
        builder.Property(e => e.Type)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("type")
            .HasDefaultValue("Percentage");
        
        builder.Property(e => e.Value)
            .IsRequired()
            .HasColumnName("value")
            .HasPrecision(18, 2);
        
        builder.Property(e => e.MinOrderValue)
            .HasColumnName("min_order_value")
            .HasPrecision(18, 2);
        
        builder.Property(e => e.MaxDiscount)
            .HasColumnName("max_discount")
            .HasPrecision(18, 2);
        
        builder.Property(e => e.UsageLimit)
            .IsRequired()
            .HasColumnName("usage_limit");
        
        builder.Property(e => e.UsedCount)
            .IsRequired()
            .HasColumnName("used_count")
            .HasDefaultValue(0);
        
        builder.Property(e => e.ValidFrom)
            .IsRequired()
            .HasColumnName("valid_from");
        
        builder.Property(e => e.ValidTo)
            .IsRequired()
            .HasColumnName("valid_to");
        
        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasColumnName("is_active")
            .HasDefaultValue(true);
        
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.ValidFrom);
        builder.HasIndex(e => e.ValidTo);
    }
}

public class FlashSaleConfiguration : BaseEntityConfiguration<FlashSale>
{
    public override void Configure(EntityTypeBuilder<FlashSale> builder)
    {
        base.Configure(builder);
        
        builder.ToTable("flash_sales");
        
        builder.Property(e => e.ProductId)
            .IsRequired()
            .HasColumnName("product_id");
        
        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("title");
        
        builder.Property(e => e.DiscountPercentage)
            .IsRequired()
            .HasColumnName("discount_percentage")
            .HasPrecision(5, 2);
        
        builder.Property(e => e.MaxQuantity)
            .IsRequired()
            .HasColumnName("max_quantity");
        
        builder.Property(e => e.SoldQuantity)
            .IsRequired()
            .HasColumnName("sold_quantity")
            .HasDefaultValue(0);
        
        builder.Property(e => e.StartAt)
            .IsRequired()
            .HasColumnName("start_at");
        
        builder.Property(e => e.EndAt)
            .IsRequired()
            .HasColumnName("end_at");
        
        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasColumnName("is_active")
            .HasDefaultValue(true);
        
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.StartAt);
        builder.HasIndex(e => e.EndAt);
    }
}

public class AuditLogConfiguration : BaseEntityConfiguration<AuditLog>
{
    public override void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        base.Configure(builder);
        
        builder.ToTable("audit_logs");
        
        builder.Property(e => e.UserId)
            .HasColumnName("user_id");
        
        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("action");
        
        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("entity_type");
        
        builder.Property(e => e.EntityId)
            .HasColumnName("entity_id");
        
        builder.Property(e => e.OldValues)
            .HasColumnName("old_values")
            .HasColumnType("jsonb");
        
        builder.Property(e => e.NewValues)
            .HasColumnName("new_values")
            .HasColumnType("jsonb");
        
        builder.Property(e => e.IpAddress)
            .HasMaxLength(45)
            .HasColumnName("ip_address");
        
        builder.Property(e => e.UserAgent)
            .HasMaxLength(500)
            .HasColumnName("user_agent");
        
        builder.HasOne(e => e.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Indexes for searching
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.EntityType);
        builder.HasIndex(e => e.EntityId);
        builder.HasIndex(e => e.Action);
        builder.HasIndex(e => e.CreatedAt);
    }
}
