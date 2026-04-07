namespace BeautyEcommerce.Domain.Entities;

using BeautyEcommerce.Domain.Common;
using BeautyEcommerce.Domain.Enums;

/// <summary>
/// Outbox message for reliable event publishing
/// </summary>
public class OutboxMessage : BaseEntity
{
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // JSON
    public DateTime? ProcessedAt { get; set; }
    public DateTime? ErrorAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public Guid? WorkerId { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }
    public bool IsDeadLetter { get; set; }
}

/// <summary>
/// Order Saga state machine tracking
/// </summary>
public class OrderSagaState : BaseEntity
{
    public Guid OrderId { get; set; }
    public SagaStatus Status { get; set; } = SagaStatus.Pending;
    public string CurrentStep { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string? LastError { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CompensatedAt { get; set; }
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual ICollection<SagaCompensationLog> CompensationLogs { get; set; } = new List<SagaCompensationLog>();
}

public enum SagaStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Compensating = 3,
    Compensated = 4,
    Failed = 5
}

/// <summary>
/// Saga compensation log for idempotency
/// </summary>
public class SagaCompensationLog : BaseEntity
{
    public Guid SagaStateId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? RequestData { get; set; } // JSON
    public string? ResponseData { get; set; } // JSON
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual OrderSagaState SagaState { get; set; } = null!;
}

/// <summary>
/// Voucher/Promotion entity
/// </summary>
public class Voucher : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public VoucherType Type { get; set; }
    public decimal Value { get; set; } // Percentage or fixed amount
    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int? MaxUsagePerUser { get; set; }
    public int TotalUsageLimit { get; set; }
    public int UsageCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsPublic { get; set; }
    public string? ApplicableCategories { get; set; } // JSON array of category IDs
    public string? ApplicableProducts { get; set; } // JSON array of product IDs
    public bool IsStackable { get; set; }
    public string? UserSegments { get; set; } // JSON array of user segments
    
    // Navigation properties
    public virtual ICollection<CartVoucher> CartVouchers { get; set; } = new List<CartVoucher>();
}

/// <summary>
/// Review entity with media support
/// </summary>
public class Review : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? OrderItemId { get; set; } // Verified purchase
    public int Rating { get; set; } // 1-5
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }
    public bool IsApproved { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
    public string? Images { get; set; } // JSON array of image URLs
    public string? Videos { get; set; } // JSON array of video URLs
    public string? SellerResponse { get; set; }
    public DateTime? SellerRespondedAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

/// <summary>
/// Product Q&A entity
/// </summary>
public class ProductQa : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid? UserId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public Guid? AnsweredBy { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public bool IsApproved { get; set; }
    public int HelpfulCount { get; set; }
    
    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual User? User { get; set; }
}

/// <summary>
/// Wallet/Store credit entity
/// </summary>
public class Wallet : BaseEntity
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    public decimal PendingBalance { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}

/// <summary>
/// Wallet transaction entity
/// </summary>
public class WalletTransaction : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? RefundId { get; set; }
    public WalletTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}

public enum WalletTransactionType
{
    Credit = 0,
    Debit = 1,
    Refund = 2,
    Adjustment = 3
}

/// <summary>
/// Gift card entity
/// </summary>
public class GiftCard : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal OriginalAmount { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? PurchasedBy { get; set; }
    public Guid? RecipientId { get; set; }
    public string? Message { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? RedeemedAt { get; set; }
}

/// <summary>
/// Stock alert subscription (back-in-stock)
/// </summary>
public class StockAlertSubscription : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsNotified { get; set; }
    public DateTime? NotifiedAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

/// <summary>
/// COD confirmation attempts tracking
/// </summary>
public class CodConfirmAttempt : BaseEntity
{
    public Guid OrderId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? RecordingUrl { get; set; }
    public int AttemptNumber { get; set; }
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
}

/// <summary>
/// Recently viewed items
/// </summary>
public class UserRecentlyViewed : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
    public string? SessionId { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

/// <summary>
/// Audit log for entity versioning
/// </summary>
public class AuditLog : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty; // Created, Updated, Deleted
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public Guid PerformedBy { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Invoice entity
/// </summary>
public class Invoice : BaseEntity
{
    public Guid OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? PdfUrl { get; set; }
    public DateTime? SentAt { get; set; }
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
}

public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    Sent = 2,
    Paid = 3,
    Cancelled = 4
}
