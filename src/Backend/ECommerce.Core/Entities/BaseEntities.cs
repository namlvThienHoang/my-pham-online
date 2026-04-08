using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Ulid.NewUlid().ToGuid(); // UUID v7 compatible
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}

public class AdminUser : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? FirstName { get; set; }
    
    [MaxLength(50)]
    public string? LastName { get; set; }
    
    public bool IsMfaEnabled { get; set; } = false;
    public string? MfaSecret { get; set; }
    public string[]? MfaBackupCodes { get; set; }
    public DateTime? LastMfaVerification { get; set; }
    
    public string[] IpWhitelist { get; set; } = Array.Empty<string>();
    public string Role { get; set; } = "Admin";
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public class Customer : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? FirstName { get; set; }
    
    [MaxLength(50)]
    public string? LastName { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    public decimal TotalSpent { get; set; }
    public int OrderCount { get; set; }
    public DateTime? LastOrderDate { get; set; }
    
    // Navigation
    public ICollection<CustomerSegment> Segments { get; set; } = new List<CustomerSegment>();
    public ICollection<CustomerNote> Notes { get; set; } = new List<CustomerNote>();
    public ICollection<CustomerTag> Tags { get; set; } = new List<CustomerTag>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class CustomerSegment : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    // Rule-based segmentation (JSON)
    public string Rules { get; set; } = "{}";
    /*
     * Example:
     * {
     *   "minTotalSpent": 1000000,
     *   "minOrderCount": 5,
     *   "lastOrderDays": 30,
     *   "birthMonth": 12
     * }
     */
    
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}

public class CustomerNote : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid CreatedByAdminId { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public bool IsPrivate { get; set; } = true;
    
    // Navigation
    public Customer Customer { get; set; } = null!;
    public AdminUser CreatedByAdmin { get; set; } = null!;
}

public class CustomerTag : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Color { get; set; }
    
    // Navigation
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}

public class Product : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(200)]
    public string Sku { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    
    // Navigation
    public ICollection<InventoryLot> InventoryLots { get; set; } = new List<InventoryLot>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class InventoryLot : BaseEntity
{
    public Guid ProductId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string BatchNumber { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public DateTime ManufactureDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal UnitCost { get; set; }
    
    // Navigation
    public Product Product { get; set; } = null!;
}

public class Order : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;
    
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Processing, Shipped, Delivered, Cancelled
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentStatus { get; set; }
    
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    
    [MaxLength(500)]
    public string? ShippingAddress { get; set; }
    
    [MaxLength(500)]
    public string? InternalNotes { get; set; }
    
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    
    // Navigation
    public Customer Customer { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    
    // Navigation
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class Refund : BaseEntity
{
    public Guid OrderId { get; set; }
    
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Completed
    
    [MaxLength(500)]
    public string? AdminNotes { get; set; }
    
    // Navigation
    public Order Order { get; set; } = null!;
}

public class Voucher : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? Description { get; set; }
    
    public string Type { get; set; } = "Percentage"; // Percentage, Fixed
    public decimal Value { get; set; }
    public decimal? MinOrderValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FlashSale : BaseEntity
{
    public Guid ProductId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public decimal DiscountPercentage { get; set; }
    public int MaxQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public Product Product { get; set; } = null!;
}

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty; // Create, Update, Delete
    
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    
    public Guid? EntityId { get; set; }
    
    // Masked data for sensitive fields
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    // Navigation
    public AdminUser? User { get; set; }
}

// Materialized view helper (query model, not mapped to table directly)
public class OrderSummary
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}
