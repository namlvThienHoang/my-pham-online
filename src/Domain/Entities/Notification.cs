namespace BeautyEcommerce.Domain.Entities;

using BeautyEcommerce.Domain.Common;

/// <summary>
/// Notification entity for email, SMS, push notifications
/// </summary>
public class Notification : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? TemplateName { get; set; }
    public string? TemplateData { get; set; } // JSON
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int RetryCount { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Provider { get; set; } // SES, SendGrid, Twilio, etc.
    public string? ProviderMessageId { get; set; }
    public string? Channel { get; set; } // Email, SMS, Push, InApp
    public string? Recipient { get; set; } // Email address or phone number
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}

public enum NotificationType
{
    OrderConfirmation = 0,
    OrderShipped = 1,
    OrderDelivered = 2,
    OrderCancelled = 3,
    PaymentSuccess = 4,
    PaymentFailed = 5,
    RefundProcessed = 6,
    ReturnApproved = 7,
    ReturnRejected = 8,
    PasswordReset = 9,
    EmailVerification = 10,
    Welcome = 11,
    Promotion = 12,
    BackInStock = 13,
    ReviewRequest = 14,
    LowStockAlert = 15,
    SystemAlert = 16
}

public enum NotificationStatus
{
    Pending = 0,
    Sending = 1,
    Sent = 2,
    Delivered = 3,
    Failed = 4,
    Read = 5
}

/// <summary>
/// Email template entity
/// </summary>
public class EmailTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? BodyText { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CssStyles { get; set; }
    public string[]? Variables { get; set; } // Array of variable names
}

/// <summary>
/// SMS log for tracking sent messages
/// </summary>
public class SmsLog : BaseEntity
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public SmsStatus Status { get; set; } = SmsStatus.Pending;
    public int RetryCount { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Provider { get; set; } // Twilio, Vonage, etc.
    public string? ProviderMessageId { get; set; }
    public decimal Cost { get; set; }
    public string CurrencyCode { get; set; } = "VND";
}

public enum SmsStatus
{
    Pending = 0,
    Sending = 1,
    Sent = 2,
    Delivered = 3,
    Failed = 4,
    Undeliverable = 5
}

/// <summary>
/// Push notification subscription (for web push)
/// </summary>
public class PushSubscription : BaseEntity
{
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? VapidPublicKey { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
