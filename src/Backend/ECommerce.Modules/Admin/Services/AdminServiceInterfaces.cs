using ECommerce.Modules.Admin.DTOs;

namespace ECommerce.Modules.Admin.Services;

public interface IAdminDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken);
}

public interface IAdminOrderService
{
    Task<OrderListResultDto> GetOrdersWithCursorPaginationAsync(
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        string? paymentMethod,
        string? cursor,
        int limit,
        CancellationToken cancellationToken);
    
    Task<OrderDto> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, string status, string? internalNotes, CancellationToken cancellationToken);
}

public interface IAdminCustomerService
{
    Task<CustomerProfileDto> GetCustomerProfileAsync(Guid customerId, CancellationToken cancellationToken);
    Task<CustomerNoteDto> AddCustomerNoteAsync(Guid customerId, string content, bool isPrivate, CancellationToken cancellationToken);
    Task<List<CustomerSegmentDto>> GetCustomerSegmentsAsync(Guid customerId, CancellationToken cancellationToken);
    Task<CustomerProfileDto> AddCustomerTagsAsync(Guid customerId, List<Guid> tagIds, CancellationToken cancellationToken);
}

public interface IAuditLogService
{
    Task<AuditLogListResultDto> GetAuditLogsAsync(
        Guid? userId,
        string? entityType,
        string? action,
        DateTime? fromDate,
        DateTime? toDate,
        string? cursor,
        int limit,
        CancellationToken cancellationToken);
}

public interface IExportService
{
    Task<ExportJobResultDto> StartExportJobAsync(string exportType, Dictionary<string, string?> filters, CancellationToken cancellationToken);
    Task<ExportJobResultDto> GetExportJobStatusAsync(Guid jobId, CancellationToken cancellationToken);
}

public interface IMarketingEmailService
{
    Task<SendEmailResultDto> SendBatchEmailsAsync(List<Guid> customerIds, string subject, string body, Guid? templateId, CancellationToken cancellationToken);
    Task<bool> SendSegmentEmailsAsync(Guid segmentId, string subject, string body, Guid? templateId, CancellationToken cancellationToken);
}

public interface IAdminProductService
{
    Task<List<ProductDto>> GetProductsAsync(bool? isActive = null, string? search = null, CancellationToken cancellationToken = default);
    Task<ProductDto> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken);
    Task<ProductDto> UpdateProductAsync(Guid productId, UpdateProductDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteProductAsync(Guid productId, CancellationToken cancellationToken);
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 10;
    public bool IsActive { get; set; } = true;
}

public class UpdateProductDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int? StockQuantity { get; set; }
    public int? LowStockThreshold { get; set; }
    public bool? IsActive { get; set; }
}

public interface IAdminInventoryService
{
    Task<List<InventoryLotDto>> GetInventoryLotsAsync(Guid? productId = null, bool? expiringSoon = null, CancellationToken cancellationToken = default);
    Task<InventoryLotDto> CreateInventoryLotAsync(CreateInventoryLotDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteInventoryLotAsync(Guid lotId, CancellationToken cancellationToken);
}

public class CreateInventoryLotDto
{
    public Guid ProductId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ManufactureDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal UnitCost { get; set; }
}

public interface IAdminVoucherService
{
    Task<List<VoucherDto>> GetVouchersAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<VoucherDto> GetVoucherByCodeAsync(string code, CancellationToken cancellationToken);
    Task<VoucherDto> CreateVoucherAsync(CreateVoucherDto dto, CancellationToken cancellationToken);
    Task<VoucherDto> UpdateVoucherAsync(Guid voucherId, UpdateVoucherDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteVoucherAsync(Guid voucherId, CancellationToken cancellationToken);
}

public class CreateVoucherDto
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "Percentage";
    public decimal Value { get; set; }
    public decimal? MinOrderValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int UsageLimit { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateVoucherDto
{
    public string? Description { get; set; }
    public decimal? Value { get; set; }
    public decimal? MinOrderValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int? UsageLimit { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool? IsActive { get; set; }
}

public interface IAdminFlashSaleService
{
    Task<List<FlashSaleDto>> GetFlashSalesAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<FlashSaleDto> GetFlashSaleByIdAsync(Guid flashSaleId, CancellationToken cancellationToken);
    Task<FlashSaleDto> CreateFlashSaleAsync(CreateFlashSaleDto dto, CancellationToken cancellationToken);
    Task<FlashSaleDto> UpdateFlashSaleAsync(Guid flashSaleId, UpdateFlashSaleDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteFlashSaleAsync(Guid flashSaleId, CancellationToken cancellationToken);
}

public class CreateFlashSaleDto
{
    public Guid ProductId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public int MaxQuantity { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateFlashSaleDto
{
    public string? Title { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public int? MaxQuantity { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public bool? IsActive { get; set; }
}
