namespace BeautyEcommerce.Domain.Entities;

using BeautyEcommerce.Domain.Common;

/// <summary>
/// Flash Sale entity with queue management
/// </summary>
public class FlashSale : BaseEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public int MaxQuantityPerCustomer { get; set; } = 1;
    public int TotalLimit { get; set; } // Total items available for flash sale
    public int SoldCount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal? FixedDiscountAmount { get; set; }
    public FlashSaleStatus Status { get; set; } = FlashSaleStatus.Scheduled;
    
    // Queue management (Redis Stream)
    public string? QueueKey { get; set; }
    public int QueueSize { get; set; }
    
    // Navigation properties
    public virtual ICollection<FlashSaleProduct> Products { get; set; } = new List<FlashSaleProduct>();
}

/// <summary>
/// Product association for flash sale
/// </summary>
public class FlashSaleProduct : BaseEntity
{
    public Guid FlashSaleId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal FlashSalePrice { get; set; }
    public int AvailableQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public int Position { get; set; }
    
    // Navigation properties
    public virtual FlashSale FlashSale { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

public enum FlashSaleStatus
{
    Scheduled = 0,
    Active = 1,
    Ended = 2,
    Cancelled = 3
}

/// <summary>
/// Flash sale queue entry (for waiting room pattern)
/// </summary>
public class FlashSaleQueueEntry
{
    public string Id { get; set; } = string.Empty; // Redis stream entry ID
    public Guid UserId { get; set; }
    public Guid FlashSaleId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime EnqueuedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
}
