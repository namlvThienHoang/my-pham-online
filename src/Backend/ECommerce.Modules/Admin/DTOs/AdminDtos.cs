namespace ECommerce.Modules.Admin.DTOs;

// Dashboard Stats
public class DashboardStatsDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal RevenueGrowthPercentage { get; set; }
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<LowStockProductDto> LowStockProducts { get; set; } = new();
    public List<ExpiringInventoryDto> ExpiringInventory { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class TopProductDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SoldQuantity { get; set; }
    public decimal Revenue { get; set; }
}

public class LowStockProductDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int LowStockThreshold { get; set; }
}

public class ExpiringInventoryDto
{
    public Guid LotId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int RemainingQuantity { get; set; }
    public int DaysUntilExpiry => (ExpiryDate - DateTime.UtcNow).Days;
}

// Orders
public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentStatus { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? ShippingAddress { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class OrderListResultDto
{
    public List<OrderDto> Orders { get; set; } = new();
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
    public int TotalCount { get; set; }
}

// Customers & CRM
public class CustomerProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public decimal TotalSpent { get; set; }
    public int OrderCount { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public List<CustomerSegmentDto> Segments { get; set; } = new();
    public List<CustomerTagDto> Tags { get; set; } = new();
    public List<CustomerNoteDto> Notes { get; set; } = new();
    public List<OrderSummaryDto> RecentOrders { get; set; } = new();
    public List<InteractionHistoryDto> InteractionHistory { get; set; } = new();
}

public class CustomerSegmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CustomerTagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public class CustomerNoteDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByAdminId { get; set; }
    public string? CreatedByAdminName { get; set; }
}

public class OrderSummaryDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InteractionHistoryDto
{
    public string Type { get; set; } = string.Empty; // Ticket, Support, Email, etc.
    public string Subject { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Status { get; set; }
}

// Audit Logs
public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; } // Masked
    public string? NewValues { get; set; } // Masked
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogListResultDto
{
    public List<AuditLogDto> Logs { get; set; } = new();
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
    public int TotalCount { get; set; }
}

// Export Jobs
public class ExportJobResultDto
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty; // Queued, Processing, Completed, Failed
    public string? DownloadUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

// Marketing Email
public class SendEmailResultDto
{
    public Guid BatchId { get; set; }
    public int TotalRecipients { get; set; }
    public int QueuedCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

// Products, Inventory, Vouchers, Flash Sales (simplified)
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
}

public class InventoryLotDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public DateTime ExpiryDate { get; set; }
}

public class VoucherDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; }
}

public class FlashSaleDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public int MaxQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsActive { get; set; }
}
