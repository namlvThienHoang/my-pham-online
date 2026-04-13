namespace BeautyEcommerce.Domain.Entities;

using BeautyEcommerce.Domain.Common;

/// <summary>
/// Product expiry tracking for purchased products
/// Helps users track when products expire and need replacement (increases repeat purchases)
/// </summary>
public class UserProduct : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? OrderItemId { get; private set; } // Link to original purchase
    public string Status { get; private set; } = "InUse"; // InUse, Finished, Expired, Discontinued
    public DateTime PurchasedDate { get; private set; }
    public DateTime OpenedDate { get; private set; } // When product was first opened
    public int ShelfLifeMonths { get; private set; } // Total shelf life from manufacturing
    public int PaO_Months { get; private set; } // Period After Opening (PAO) - e.g., 6M, 12M
    public DateTime? ExpiryDate { get; private set; } // Calculated: OpenedDate + PaO
    public DateTime? FinishedDate { get; private set; }
    public int InitialQuantity { get; private set; } // e.g., 50ml, 30 units
    public int CurrentQuantity { get; private set; }
    public string? QuantityUnit { get; private set; } // ml, g, units
    public decimal EstimatedDailyUsage { get; private set; } // For auto-calculating depletion
    public DateTime? LastUsedDate { get; private set; }
    public int UsageCount { get; private set; } // Number of times used
    public string? Notes { get; private set; }
    public bool IsExpiryAlertEnabled { get; private set; } = true;
    public bool IsReplenishEnabled { get; private set; } = false;
    public int ReplenishThresholdDays { get; private set; } = 7; // Alert X days before expiry/empty
    
    // Navigation properties
    public virtual User? User { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual OrderItem? OrderItem { get; private set; }
    public virtual ICollection<UsageLog> UsageLogs { get; private set; } = new List<UsageLog>();
    public virtual ICollection<ExpiryAlert> ExpiryAlerts { get; private set; } = new List<ExpiryAlert>();
    
    private UserProduct() { }
    
    public static UserProduct Create(
        Guid userId,
        Guid productId,
        DateTime purchasedDate,
        int shelfLifeMonths,
        int paOMonths,
        int initialQuantity,
        string? quantityUnit = null,
        Guid? orderItemId = null)
    {
        var userProduct = new UserProduct
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            OrderItemId = orderItemId,
            Status = "InUse",
            PurchasedDate = purchasedDate,
            OpenedDate = purchasedDate, // Default to purchase date, can be updated
            ShelfLifeMonths = shelfLifeMonths,
            PaO_Months = paOMonths,
            ExpiryDate = purchasedDate.AddMonths(paOMonths),
            InitialQuantity = initialQuantity,
            CurrentQuantity = initialQuantity,
            QuantityUnit = quantityUnit,
            IsExpiryAlertEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        return userProduct;
    }
    
    public void MarkAsOpened(DateTime openedDate)
    {
        OpenedDate = openedDate;
        ExpiryDate = openedDate.AddMonths(PaO_Months);
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void RecordUsage(int quantityUsed = 1, string? notes = null)
    {
        CurrentQuantity = Math.Max(0, CurrentQuantity - quantityUsed);
        UsageCount++;
        LastUsedDate = DateTime.UtcNow;
        
        if (CurrentQuantity <= 0)
        {
            Status = "Finished";
            FinishedDate = DateTime.UtcNow;
        }
        
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void EnableAutoReplenish(int thresholdDays = 7)
    {
        IsReplenishEnabled = true;
        ReplenishThresholdDays = thresholdDays;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void DisableAutoReplenish()
    {
        IsReplenishEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsExpired()
    {
        Status = "Expired";
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Daily usage log for products
/// </summary>
public class UsageLog : BaseEntity
{
    public Guid UserProductId { get; private set; }
    public DateTime UsageDate { get; private set; }
    public int QuantityUsed { get; private set; }
    public string? ApplicationArea { get; private set; } // Face, Body, Hands, etc.
    public string? TimeOfDay { get; private set; } // Morning, Evening
    public string? Notes { get; private set; }
    public int? SkinReactionRating { get; private set; } // 1-5, how skin reacted
    
    // Navigation properties
    public virtual UserProduct? UserProduct { get; private set; }
    
    private UsageLog() { }
    
    public static UsageLog Create(
        Guid userProductId,
        DateTime usageDate,
        int quantityUsed,
        string? applicationArea = null,
        string? timeOfDay = null)
    {
        return new UsageLog
        {
            Id = Guid.NewGuid(),
            UserProductId = userProductId,
            UsageDate = usageDate,
            QuantityUsed = quantityUsed,
            ApplicationArea = applicationArea,
            TimeOfDay = timeOfDay,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Expiry alert notifications
/// </summary>
public class ExpiryAlert : BaseEntity
{
    public Guid UserProductId { get; private set; }
    public Guid UserId { get; private set; }
    public string AlertType { get; private set; } = string.Empty; // ExpiryWarning, LowStock, ReplenishReminder
    public string Message { get; private set; } = string.Empty;
    public DateTime TriggerDate { get; private set; }
    public DateTime? SentDate { get; private set; }
    public string Status { get; private set; } = "Pending"; // Pending, Sent, Dismissed, Actioned
    public string NotificationChannel { get; private set; } = "Push"; // Push, Email, SMS
    public string? ActionUrl { get; private set; } // Deep link to product/reorder page
    public DateTime? DismissedDate { get; private set; }
    public DateTime? ActionedDate { get; private set; }
    
    // Navigation properties
    public virtual UserProduct? UserProduct { get; private set; }
    public virtual User? User { get; private set; }
    
    private ExpiryAlert() { }
    
    public static ExpiryAlert Create(
        Guid userProductId,
        Guid userId,
        string alertType,
        string message,
        DateTime triggerDate,
        string notificationChannel = "Push",
        string? actionUrl = null)
    {
        return new ExpiryAlert
        {
            Id = Guid.NewGuid(),
            UserProductId = userProductId,
            UserId = userId,
            AlertType = alertType,
            Message = message,
            TriggerDate = triggerDate,
            NotificationChannel = notificationChannel,
            ActionUrl = actionUrl,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void MarkAsSent()
    {
        SentDate = DateTime.UtcNow;
        Status = "Sent";
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsDismissed()
    {
        DismissedDate = DateTime.UtcNow;
        Status = "Dismissed";
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsActioned()
    {
        ActionedDate = DateTime.UtcNow;
        Status = "Actioned";
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Auto-replenishment subscription settings
/// </summary>
public class ReplenishmentSubscription : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? UserProductId { get; private set; } // Link to user's product tracking
    public string Status { get; private set; } = "Active"; // Active, Paused, Cancelled
    public int FrequencyDays { get; private set; } // Deliver every X days
    public int Quantity { get; private set; } // Quantity per delivery
    public decimal Price { get; private set; } // Subscription price
    public decimal DiscountPercentage { get; private set; } // Subscriber discount
    public DateTime NextDeliveryDate { get; private set; }
    public DateTime? LastDeliveryDate { get; private set; }
    public int TotalDeliveries { get; private set; }
    public int RemainingDeliveries { get; private set; } // 0 = unlimited
    public string? ShippingAddressJson { get; private set; }
    public Guid? PaymentMethodId { get; private set; }
    public bool IsNotificationEnabled { get; private set; } = true;
    public int NotificationDaysBefore { get; private set; } = 3; // Notify X days before delivery
    public string? Notes { get; private set; }
    public DateTime? CancelledDate { get; private set; }
    public string? CancellationReason { get; private set; }
    
    // Navigation properties
    public virtual User? User { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual UserProduct? UserProduct { get; private set; }
    public virtual ICollection<ReplenishmentOrder> ReplenishmentOrders { get; private set; } = new List<ReplenishmentOrder>();
    
    private ReplenishmentSubscription() { }
    
    public static ReplenishmentSubscription Create(
        Guid userId,
        Guid productId,
        int frequencyDays,
        int quantity,
        decimal price,
        decimal discountPercentage = 10,
        Guid? userProductId = null,
        int remainingDeliveries = 0) // 0 = unlimited
    {
        return new ReplenishmentSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            UserProductId = userProductId,
            Status = "Active",
            FrequencyDays = frequencyDays,
            Quantity = quantity,
            Price = price,
            DiscountPercentage = discountPercentage,
            NextDeliveryDate = DateTime.UtcNow.AddDays(frequencyDays),
            TotalDeliveries = 0,
            RemainingDeliveries = remainingDeliveries,
            IsNotificationEnabled = true,
            NotificationDaysBefore = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void Pause()
    {
        Status = "Paused";
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Resume()
    {
        Status = "Active";
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Cancel(string reason)
    {
        Status = "Cancelled";
        CancelledDate = DateTime.UtcNow;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void RecordDelivery()
    {
        LastDeliveryDate = DateTime.UtcNow;
        NextDeliveryDate = DateTime.UtcNow.AddDays(FrequencyDays);
        TotalDeliveries++;
        
        if (RemainingDeliveries > 0)
        {
            RemainingDeliveries--;
        }
        
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateNextDelivery(DateTime newDate)
    {
        NextDeliveryDate = newDate;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Individual replenishment order from subscription
/// </summary>
public class ReplenishmentOrder : BaseEntity
{
    public Guid SubscriptionId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }
    public decimal DiscountApplied { get; private set; }
    public string Status { get; private set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Failed
    public DateTime ScheduledDate { get; private set; }
    public DateTime? ProcessedDate { get; private set; }
    public DateTime? ShippedDate { get; private set; }
    public DateTime? DeliveredDate { get; private set; }
    public string? FailureReason { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? Carrier { get; private set; }
    public string? ShippingAddressJson { get; private set; }
    public Guid? RelatedOrderId { get; private set; } // Link to actual Order entity
    
    // Navigation properties
    public virtual ReplenishmentSubscription? Subscription { get; private set; }
    public virtual User? User { get; private set; }
    public virtual Product? Product { get; private set; }
    
    private ReplenishmentOrder() { }
    
    public static ReplenishmentOrder Create(
        Guid subscriptionId,
        Guid userId,
        Guid productId,
        int quantity,
        decimal unitPrice,
        decimal discountApplied,
        DateTime scheduledDate)
    {
        return new ReplenishmentOrder
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            UserId = userId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice,
            DiscountApplied = discountApplied,
            Status = "Pending",
            ScheduledDate = scheduledDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void MarkAsProcessed()
    {
        Status = "Processing";
        ProcessedDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsShipped(string trackingNumber, string carrier)
    {
        Status = "Shipped";
        ShippedDate = DateTime.UtcNow;
        TrackingNumber = trackingNumber;
        Carrier = carrier;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsDelivered()
    {
        Status = "Delivered";
        DeliveredDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsFailed(string reason)
    {
        Status = "Failed";
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}
