namespace BeautyCommerce.Domain.Enums;

/// <summary>
/// Order status enum (PostgreSQL ENUM)
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    AwaitingPayment = 1,
    PaymentPending = 2,
    Paid = 3,
    Confirmed = 4,
    Processing = 5,
    PartiallyShipped = 6,
    Shipped = 7,
    OutForDelivery = 8,
    Delivered = 9,
    Completed = 10,
    Cancelled = 11,
    Refunding = 12,
    Refunded = 13,
    ReturnRequested = 14,
    ReturnApproved = 15,
    ReturnRejected = 16
}

/// <summary>
/// Payment status enum
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Authorized = 1,
    Captured = 2,
    Failed = 3,
    Refunded = 4,
    PartiallyRefunded = 5,
    Cancelled = 6
}

/// <summary>
/// Payment method enum
/// </summary>
public enum PaymentMethod
{
    CreditCard = 0,
    DebitCard = 1,
    BankTransfer = 2,
    COD = 3,
    Wallet = 4,
    GiftCard = 5,
    StoreCredit = 6
}

/// <summary>
/// Inventory status enum
/// </summary>
public enum InventoryStatus
{
    Available = 0,
    Reserved = 1,
    Committed = 2,
    Damaged = 3,
    Expired = 4,
    InTransit = 5
}

/// <summary>
/// Shipment status enum
/// </summary>
public enum ShipmentStatus
{
    Pending = 0,
    PickedUp = 1,
    InTransit = 2,
    OutForDelivery = 3,
    Delivered = 4,
    Failed = 5,
    Returned = 6,
    Lost = 7
}

/// <summary>
/// User role enum
/// </summary>
public enum UserRole
{
    Customer = 0,
    Admin = 1,
    Staff = 2,
    Vendor = 3
}

/// <summary>
/// Gender enum for skin profile
/// </summary>
public enum Gender
{
    Male = 0,
    Female = 1,
    Other = 2,
    PreferNotToSay = 3
}

/// <summary>
/// Skin type enum
/// </summary>
public enum SkinType
{
    Normal = 0,
    Dry = 1,
    Oily = 2,
    Combination = 3,
    Sensitive = 4
}

/// <summary>
/// Voucher type enum
/// </summary>
public enum VoucherType
{
    Percentage = 0,
    FixedAmount = 1,
    FreeShipping = 2,
    BuyXGetY = 3
}

/// <summary>
/// Notification type enum
/// </summary>
public enum NotificationType
{
    Email = 0,
    SMS = 1,
    Push = 2,
    InApp = 3
}

/// <summary>
/// Return status enum
/// </summary>
public enum ReturnStatus
{
    Requested = 0,
    Approved = 1,
    Rejected = 2,
    PickedUp = 3,
    Received = 4,
    Inspected = 5,
    Refunded = 6
}
