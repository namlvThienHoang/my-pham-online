namespace BeautyCommerce.Domain.Entities;

using BeautyCommerce.Domain.Common;
using BeautyCommerce.Domain.Enums;

/// <summary>
/// Inventory lot for FEFO (First Expired, First Out)
/// </summary>
public class InventoryLot : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public DateTime ManufactureDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal UnitCost { get; set; }
    public string? SupplierName { get; set; }
    public InventoryStatus Status { get; set; } = InventoryStatus.Available;
    
    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual ProductVariant? Variant { get; set; }
}

/// <summary>
/// Stock movement tracking
/// </summary>
public class StockMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? LotId { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public StockMovementType Type { get; set; }
    public string? ReferenceType { get; set; } // Order, Return, Adjustment, etc.
    public Guid? ReferenceId { get; set; }
    public string? Reason { get; set; }
    public Guid PerformedBy { get; set; }
    
    // Navigation properties
    public virtual Product Product { get; set; } = null!;
}

public enum StockMovementType
{
    Inbound = 0,
    Outbound = 1,
    Reservation = 2,
    Cancellation = 3,
    Adjustment = 4,
    Damaged = 5,
    Expired = 6,
    Return = 7
}

/// <summary>
/// Cart entity
/// </summary>
public class Cart : BaseEntity
{
    public Guid UserId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal Total { get; set; }
    public string? CurrencyCode { get; set; } = "VND";
    public DateTime? ExpiresAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    public virtual ICollection<CartVoucher> Vouchers { get; set; } = new List<CartVoucher>();
}

/// <summary>
/// Cart item entity
/// </summary>
public class CartItem : BaseEntity
{
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public string? CustomData { get; set; } // JSON for custom options
    
    // Navigation properties
    public virtual Cart Cart { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

/// <summary>
/// Cart voucher association
/// </summary>
public class CartVoucher : BaseEntity
{
    public Guid CartId { get; set; }
    public Guid VoucherId { get; set; }
    public decimal DiscountAmount { get; set; }
    
    // Navigation properties
    public virtual Cart Cart { get; set; } = null!;
    public virtual Voucher Voucher { get; set; } = null!;
}

/// <summary>
/// Wishlist entity
/// </summary>
public class Wishlist : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = "My Wishlist";
    public bool IsPublic { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
}

/// <summary>
/// Wishlist item entity
/// </summary>
public class WishlistItem : BaseEntity
{
    public Guid WishlistId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime? AddedAt { get; set; }
    
    // Navigation properties
    public virtual Wishlist Wishlist { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
