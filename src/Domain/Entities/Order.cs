namespace BeautyEcommerce.Domain.Entities;

using BeautyEcommerce.Domain.Common;
using BeautyEcommerce.Domain.Enums;

/// <summary>
/// Order entity with 17 states as per spec
/// </summary>
public class Order : BaseEntity, IAggregateRoot
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal Total { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? PaymentIntentId { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool IsCod { get; set; }
    public bool CodConfirmed { get; set; }
    public int CodConfirmAttempts { get; set; }
    public DateTime? CodConfirmedAt { get; set; }
    
    // Customer info snapshot
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    // Shipping address snapshot
    public string ShippingAddressLine1 { get; set; } = string.Empty;
    public string? ShippingAddressLine2 { get; set; }
    public string ShippingWard { get; set; } = string.Empty;
    public string ShippingDistrict { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = "VN";
    public string? ShippingPostalCode { get; set; }
    
    // Billing address snapshot
    public string? BillingAddressLine1 { get; set; }
    public string? BillingAddressLine2 { get; set; }
    public string? BillingWard { get; set; }
    public string? BillingDistrict { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingCountry { get; set; }
    public string? BillingPostalCode { get; set; }
    
    // Notes
    public string? CustomerNote { get; set; }
    public string? InternalNote { get; set; }
    
    // Fulfillment
    public Guid? ShipmentId { get; set; }
    public string? TrackingNumber { get; set; }
    public string? CarrierCode { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    
    // Vouchers and credits
    public string? VoucherCode { get; set; }
    public decimal WalletAmountUsed { get; set; }
    public decimal GiftCardAmountUsed { get; set; }
    public string? GiftCardCode { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual Shipment? Shipment { get; set; }
    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    public virtual OrderSagaState? SagaState { get; set; }
}

/// <summary>
/// Order item entity
/// </summary>
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? LotId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public int FulfilledQuantity { get; set; }
    public int ReturnedQuantity { get; set; }
    public int CancelledQuantity { get; set; }
    public string? CustomData { get; set; }
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

/// <summary>
/// Payment entity with idempotency support
/// </summary>
public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public string? TransactionId { get; set; }
    public string? PaymentGatewayId { get; set; }
    public string? GatewayResponse { get; set; } // JSON
    public string? IdempotencyKey { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
}

/// <summary>
/// Refund entity (supports partial refunds)
/// </summary>
public class Refund : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid? PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public RefundReason Reason { get; set; }
    public string? Description { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string? TransactionId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool IsPartial { get; set; }
    public Guid? OriginalRefundId { get; set; } // For chained refunds
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
}

public enum RefundReason
{
    CustomerRequest = 0,
    DefectiveProduct = 1,
    WrongItem = 2,
    LateDelivery = 3,
    Other = 4
}

public enum RefundStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5
}

/// <summary>
/// Shipment entity
/// </summary>
public class Shipment : BaseEntity
{
    public Guid OrderId { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
    public string CarrierCode { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string? ShippingService { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal? InsuranceFee { get; set; }
    public string? ExternalShipmentId { get; set; } // GHN/GHTK shipment ID
    public string? ExternalStatus { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? InTransitAt { get; set; }
    public DateTime? OutForDeliveryAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public JsonDocument? TrackingHistory { get; set; } // JSON array of tracking events
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
}
