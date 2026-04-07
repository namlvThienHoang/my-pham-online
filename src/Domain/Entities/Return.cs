namespace BeautyEcommerce.Domain.Entities;

using BeautyEcommerce.Domain.Common;
using BeautyEcommerce.Domain.Enums;

/// <summary>
/// Return/Refund request entity (supports partial returns)
/// </summary>
public class ReturnRequest : BaseEntity, IAggregateRoot
{
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public ReturnStatus Status { get; set; } = ReturnStatus.Requested;
    public decimal TotalRefundAmount { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    
    // Return details
    public ReturnReason Reason { get; set; }
    public string? Description { get; set; }
    public bool IsPartialReturn { get; set; }
    
    // Pickup/Delivery info
    public string? PickupAddress { get; set; }
    public DateTime? ScheduledPickupDate { get; set; }
    public string? TrackingNumber { get; set; }
    public string? CarrierCode { get; set; }
    
    // Inspection results
    public string? InspectionNotes { get; set; }
    public Guid? InspectedBy { get; set; }
    public DateTime? InspectedAt { get; set; }
    public bool ItemsReceivedInGoodCondition { get; set; }
    
    // Refund processing
    public RefundMethod RefundMethod { get; set; } = RefundMethod.OriginalPayment;
    public string? RefundTransactionId { get; set; }
    public DateTime? RefundProcessedAt { get; set; }
    
    // Media evidence
    public string? Images { get; set; } // JSON array of image URLs
    public string? Videos { get; set; } // JSON array of video URLs
    
    // Timeline
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual ICollection<ReturnItem> Items { get; set; } = new List<ReturnItem>();
    public virtual Refund? Refund { get; set; }
}

/// <summary>
/// Individual item in a return request
/// </summary>
public class ReturnItem : BaseEntity
{
    public Guid ReturnRequestId { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal RefundAmount { get; set; }
    public ReturnItemStatus Status { get; set; } = ReturnItemStatus.Pending;
    public string? Condition { get; set; } // Condition of returned item
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual ReturnRequest ReturnRequest { get; set; } = null!;
    public virtual OrderItem OrderItem { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

public enum ReturnReason
{
    DefectiveProduct = 0,
    WrongItem = 1,
    DamagedInTransit = 2,
    NotAsDescribed = 3,
    AllergicReaction = 4,
    ExpiredProduct = 5,
    ChangedMind = 6,
    Other = 7
}

public enum ReturnItemStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    PickedUp = 3,
    Received = 4,
    Inspected = 5,
    Refunded = 6
}

public enum RefundMethod
{
    OriginalPayment = 0,
    Wallet = 1,
    StoreCredit = 2,
    GiftCard = 3,
    BankTransfer = 4
}
