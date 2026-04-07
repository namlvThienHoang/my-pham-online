namespace BeautyCommerce.Infrastructure.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BeautyCommerce.Domain.Entities;

/// <summary>
/// User entity configuration
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");
        
        builder.Property(u => u.PasswordHash)
            .IsRequired();
        
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);
        
        builder.Property(u => u.MfaSecret)
            .HasMaxLength(256);
        
        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);
        
        // Timestamps and versioning
        builder.Property(u => u.CreatedAt)
            .IsRequired();
        
        builder.Property(u => u.UpdatedAt)
            .IsRequired();
        
        builder.Property(u => u.RowVersion)
            .IsRowVersion();
        
        // Soft delete
        builder.Property(u => u.DeletedAt);
        builder.HasIndex(u => u.DeletedAt);
    }
}

/// <summary>
/// UserAddress entity configuration
/// </summary>
public class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.ToTable("user_addresses");
        
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.FullName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(a => a.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.Property(a => a.AddressLine1)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(a => a.AddressLine2)
            .HasMaxLength(256);
        
        builder.Property(a => a.Ward)
            .HasMaxLength(100);
        
        builder.Property(a => a.District)
            .HasMaxLength(100);
        
        builder.Property(a => a.City)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(a => a.Country)
            .IsRequired()
            .HasMaxLength(2)
            .HasDefaultValue("VN");
        
        builder.Property(a => a.PostalCode)
            .HasMaxLength(20);
        
        builder.HasOne(a => a.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(a => new { a.UserId, a.IsDefault });
    }
}

/// <summary>
/// Product entity configuration
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.HasIndex(p => p.Sku)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(p => p.Description);
        
        builder.Property(p => p.ShortDescription)
            .HasMaxLength(500);
        
        builder.Property(p => p.Price)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.Property(p => p.CompareAtPrice)
            .HasPrecision(18, 2);
        
        builder.Property(p => p.Cost)
            .HasPrecision(18, 2);
        
        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.HasIndex(p => p.Slug)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");
        
        builder.Property(p => p.MetaTitle)
            .HasMaxLength(256);
        
        builder.Property(p => p.MetaDescription)
            .HasMaxLength(1000);
        
        builder.Property(p => p.MetaKeywords)
            .HasMaxLength(500);
        
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Full-text search vector (PostgreSQL specific)
        // This will be configured via raw SQL in migration
    }
}

/// <summary>
/// Order entity configuration
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.HasIndex(o => o.OrderNumber)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");
        
        builder.Property(o => o.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("VND");
        
        builder.Property(o => o.Subtotal)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.Property(o => o.DiscountAmount)
            .HasPrecision(18, 2);
        
        builder.Property(o => o.TaxAmount)
            .HasPrecision(18, 2);
        
        builder.Property(o => o.ShippingAmount)
            .HasPrecision(18, 2);
        
        builder.Property(o => o.Total)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.Property(o => o.CustomerEmail)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(o => o.CustomerName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(o => o.CustomerPhone)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.Property(o => o.ShippingAddressLine1)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(o => o.VoucherCode)
            .HasMaxLength(50);
        
        builder.Property(o => o.GiftCardCode)
            .HasMaxLength(50);
        
        builder.HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(o => o.Shipment)
            .WithOne(s => s.Order)
            .HasForeignKey<Shipment>(s => s.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Index for user orders with cursor pagination
        builder.HasIndex(o => new { o.UserId, o.CreatedAt });
        
        // Partitioning hint (will be applied in migration)
        // PARTITION BY RANGE (created_at)
    }
}

/// <summary>
/// OutboxMessage entity configuration
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(m => m.Content)
            .IsRequired();
        
        builder.Property(m => m.Error)
            .HasMaxLength(1000);
        
        builder.HasIndex(m => m.ProcessedAt)
            .HasFilter("processed_at IS NULL");
        
        builder.HasIndex(m => m.IsDeadLetter);
        
        builder.HasIndex(m => new { m.WorkerId, m.LeaseExpiresAt })
            .HasFilter("processed_at IS NULL AND is_dead_letter = false");
    }
}

/// <summary>
/// OrderSagaState entity configuration
/// </summary>
public class OrderSagaStateConfiguration : IEntityTypeConfiguration<OrderSagaState>
{
    public void Configure(EntityTypeBuilder<OrderSagaState> builder)
    {
        builder.ToTable("order_saga_state");
        
        builder.HasKey(s => s.Id);
        
        builder.HasIndex(s => s.OrderId)
            .IsUnique();
        
        builder.Property(s => s.CurrentStep)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(s => s.LastError)
            .HasMaxLength(1000);
        
        builder.HasOne(s => s.Order)
            .WithOne(o => o.SagaState)
            .HasForeignKey<OrderSagaState>(s => s.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(s => s.CompensationLogs)
            .WithOne(l => l.SagaState)
            .HasForeignKey(l => l.SagaStateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// SagaCompensationLog entity configuration
/// </summary>
public class SagaCompensationLogConfiguration : IEntityTypeConfiguration<SagaCompensationLog>
{
    public void Configure(EntityTypeBuilder<SagaCompensationLog> builder)
    {
        builder.ToTable("saga_compensation_log");
        
        builder.HasKey(l => l.Id);
        
        builder.Property(l => l.StepName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(l => l.Action)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(l => l.Error)
            .HasMaxLength(1000);
        
        builder.HasIndex(l => new { l.SagaStateId, l.StepName });
    }
}
