using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using ECommerce.Modules.Admin.Handlers;
using ECommerce.Modules.Admin.DTOs;
using ECommerce.Modules.Admin.Services;

namespace ECommerce.Modules.Admin.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/admin/api/v1")
            .RequireAuthorization("AdminMfaPolicy")
            .WithOpenApi();
        
        // Dashboard
        adminGroup.MapGet("/dashboard/stats", GetDashboardStats)
            .WithName("GetDashboardStats")
            .WithSummary("Get dashboard statistics");
        
        // Orders
        adminGroup.MapGet("/orders", GetOrdersList)
            .WithName("GetOrdersList")
            .WithSummary("Get orders with cursor pagination");
        
        adminGroup.MapGet("/orders/{orderId:guid}", GetOrderById)
            .WithName("GetOrderById")
            .WithSummary("Get order by ID");
        
        adminGroup.MapPatch("/orders/{orderId:guid}/status", UpdateOrderStatus)
            .WithName("UpdateOrderStatus")
            .WithSummary("Update order status and internal notes");
        
        // Customers
        adminGroup.MapGet("/customers/{customerId:guid}", GetCustomerProfile)
            .WithName("GetCustomerProfile")
            .WithSummary("Get customer profile with full details");
        
        adminGroup.MapPost("/customers/{customerId:guid}/notes", AddCustomerNote)
            .WithName("AddCustomerNote")
            .WithSummary("Add note to customer profile");
        
        adminGroup.MapPost("/customers/{customerId:guid}/tags", AddCustomerTags)
            .WithName("AddCustomerTags")
            .WithSummary("Add tags to customer");
        
        // Products
        adminGroup.MapGet("/products", GetProductsList)
            .WithName("GetProductsList")
            .WithSummary("Get products list");
        
        adminGroup.MapPost("/products", CreateProduct)
            .WithName("CreateProduct")
            .WithSummary("Create new product");
        
        adminGroup.MapPut("/products/{productId:guid}", UpdateProduct)
            .WithName("UpdateProduct")
            .WithSummary("Update product");
        
        adminGroup.MapDelete("/products/{productId:guid}", DeleteProduct)
            .WithName("DeleteProduct")
            .WithSummary("Soft delete product");
        
        // Inventory
        adminGroup.MapGet("/inventory/lots", GetInventoryLots)
            .WithName("GetInventoryLots")
            .WithSummary("Get inventory lots");
        
        adminGroup.MapPost("/inventory/lots", CreateInventoryLot)
            .WithName("CreateInventoryLot")
            .WithSummary("Create inventory lot");
        
        adminGroup.MapDelete("/inventory/lots/{lotId:guid}", DeleteInventoryLot)
            .WithName("DeleteInventoryLot")
            .WithSummary("Delete inventory lot");
        
        // Vouchers
        adminGroup.MapGet("/vouchers", GetVouchersList)
            .WithName("GetVouchersList")
            .WithSummary("Get vouchers list");
        
        adminGroup.MapPost("/vouchers", CreateVoucher)
            .WithName("CreateVoucher")
            .WithSummary("Create voucher");
        
        adminGroup.MapPut("/vouchers/{voucherId:guid}", UpdateVoucher)
            .WithName("UpdateVoucher")
            .WithSummary("Update voucher");
        
        adminGroup.MapDelete("/vouchers/{voucherId:guid}", DeleteVoucher)
            .WithName("DeleteVoucher")
            .WithSummary("Delete voucher");
        
        // Flash Sales
        adminGroup.MapGet("/flash-sales", GetFlashSalesList)
            .WithName("GetFlashSalesList")
            .WithSummary("Get flash sales list");
        
        adminGroup.MapPost("/flash-sales", CreateFlashSale)
            .WithName("CreateFlashSale")
            .WithSummary("Create flash sale");
        
        adminGroup.MapPut("/flash-sales/{flashSaleId:guid}", UpdateFlashSale)
            .WithName("UpdateFlashSale")
            .WithSummary("Update flash sale");
        
        adminGroup.MapDelete("/flash-sales/{flashSaleId:guid}", DeleteFlashSale)
            .WithName("DeleteFlashSale")
            .WithSummary("Delete flash sale");
        
        // Audit Logs
        adminGroup.MapGet("/audit-logs", GetAuditLogs)
            .WithName("GetAuditLogs")
            .WithSummary("Get audit logs with cursor pagination");
        
        // Export
        adminGroup.MapPost("/export", StartExport)
            .WithName("StartExport")
            .WithSummary("Start background export job");
        
        adminGroup.MapGet("/export/{jobId:guid}", GetExportStatus)
            .WithName("GetExportStatus")
            .WithSummary("Get export job status");
        
        // Marketing Email
        adminGroup.MapPost("/marketing/email/send", SendMarketingEmail)
            .WithName("SendMarketingEmail")
            .WithSummary("Send batch marketing email");
    }
    
    // Dashboard
    private static async Task<IResult> GetDashboardStats(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetDashboardStatsQuery(), ct);
        return Results.Ok(result);
    }
    
    // Orders
    private static async Task<IResult> GetOrdersList(
        ISender sender,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? paymentMethod,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var query = new GetOrdersListQuery(status, fromDate, toDate, paymentMethod, cursor, limit);
        var result = await sender.Send(query, ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> GetOrderById(ISender sender, Guid orderId, CancellationToken ct)
    {
        var result = await sender.Send(new GetOrdersListQuery(null, null, null, null, null, 1), ct);
        var order = result.Orders.FirstOrDefault(o => o.Id == orderId);
        return order != null ? Results.Ok(order) : Results.NotFound();
    }
    
    private static async Task<IResult> UpdateOrderStatus(
        ISender sender,
        Guid orderId,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken ct)
    {
        var command = new UpdateOrderStatusCommand(orderId, request.Status, request.InternalNotes);
        var result = await sender.Send(command, ct);
        return Results.Ok(result);
    }
    
    // Customers
    private static async Task<IResult> GetCustomerProfile(ISender sender, Guid customerId, CancellationToken ct)
    {
        var result = await sender.Send(new GetCustomerProfileQuery(customerId), ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> AddCustomerNote(
        ISender sender,
        Guid customerId,
        [FromBody] AddCustomerNoteRequest request,
        CancellationToken ct)
    {
        var command = new AddCustomerNoteCommand(customerId, request.Content, request.IsPrivate);
        var result = await sender.Send(command, ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> AddCustomerTags(
        ISender sender,
        Guid customerId,
        [FromBody] AddCustomerTagsRequest request,
        CancellationToken ct)
    {
        // Implementation would go here
        return Results.Ok();
    }
    
    // Products
    private static async Task<IResult> GetProductsList(
        IAdminProductService productService,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await productService.GetProductsAsync(isActive, search, ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> CreateProduct(
        ISender sender,
        [FromBody] CreateProductDto dto,
        CancellationToken ct)
    {
        // Implementation would use a service or command
        return Results.Ok();
    }
    
    private static async Task<IResult> UpdateProduct(
        ISender sender,
        Guid productId,
        [FromBody] UpdateProductDto dto,
        CancellationToken ct)
    {
        return Results.Ok();
    }
    
    private static async Task<IResult> DeleteProduct(
        ISender sender,
        Guid productId,
        CancellationToken ct)
    {
        return Results.NoContent();
    }
    
    // Inventory
    private static async Task<IResult> GetInventoryLots(
        IAdminInventoryService inventoryService,
        [FromQuery] Guid? productId,
        [FromQuery] bool? expiringSoon,
        CancellationToken ct)
    {
        var result = await inventoryService.GetInventoryLotsAsync(productId, expiringSoon, ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> CreateInventoryLot(
        [FromBody] CreateInventoryLotDto dto,
        IAdminInventoryService inventoryService,
        CancellationToken ct)
    {
        var result = await inventoryService.CreateInventoryLotAsync(dto, ct);
        return Results.Created($"/admin/api/v1/inventory/lots/{result.Id}", result);
    }
    
    private static async Task<IResult> DeleteInventoryLot(
        Guid lotId,
        IAdminInventoryService inventoryService,
        CancellationToken ct)
    {
        await inventoryService.DeleteInventoryLotAsync(lotId, ct);
        return Results.NoContent();
    }
    
    // Vouchers
    private static async Task<IResult> GetVouchersList(
        IAdminVoucherService voucherService,
        [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var result = await voucherService.GetVouchersAsync(isActive, ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> CreateVoucher(
        [FromBody] CreateVoucherDto dto,
        IAdminVoucherService voucherService,
        CancellationToken ct)
    {
        var result = await voucherService.CreateVoucherAsync(dto, ct);
        return Results.Created($"/admin/api/v1/vouchers/{result.Id}", result);
    }
    
    private static async Task<IResult> UpdateVoucher(
        Guid voucherId,
        [FromBody] UpdateVoucherDto dto,
        IAdminVoucherService voucherService,
        CancellationToken ct)
    {
        var result = await voucherService.UpdateVoucherAsync(voucherId, dto, ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> DeleteVoucher(
        Guid voucherId,
        IAdminVoucherService voucherService,
        CancellationToken ct)
    {
        await voucherService.DeleteVoucherAsync(voucherId, ct);
        return Results.NoContent();
    }
    
    // Flash Sales
    private static async Task<IResult> GetFlashSalesList(
        IAdminFlashSaleService flashSaleService,
        [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var result = await flashSaleService.GetFlashSalesAsync(isActive, ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> CreateFlashSale(
        [FromBody] CreateFlashSaleDto dto,
        IAdminFlashSaleService flashSaleService,
        CancellationToken ct)
    {
        var result = await flashSaleService.CreateFlashSaleAsync(dto, ct);
        return Results.Created($"/admin/api/v1/flash-sales/{result.Id}", result);
    }
    
    private static async Task<IResult> UpdateFlashSale(
        Guid flashSaleId,
        [FromBody] UpdateFlashSaleDto dto,
        IAdminFlashSaleService flashSaleService,
        CancellationToken ct)
    {
        var result = await flashSaleService.UpdateFlashSaleAsync(flashSaleId, dto, ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> DeleteFlashSale(
        Guid flashSaleId,
        IAdminFlashSaleService flashSaleService,
        CancellationToken ct)
    {
        await flashSaleService.DeleteFlashSaleAsync(flashSaleId, ct);
        return Results.NoContent();
    }
    
    // Audit Logs
    private static async Task<IResult> GetAuditLogs(
        ISender sender,
        [FromQuery] Guid? userId,
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var query = new GetAuditLogsQuery(userId, entityType, action, fromDate, toDate, cursor, limit);
        var result = await sender.Send(query, ct);
        return Results.Ok(result);
    }
    
    // Export
    private static async Task<IResult> StartExport(
        ISender sender,
        [FromBody] ExportRequest request,
        CancellationToken ct)
    {
        var command = new ExportDataCommand(request.ExportType, request.Filters ?? new());
        var result = await sender.Send(command, ct);
        return Results.Accepted($"/admin/api/v1/export/{result.JobId}", result);
    }
    
    private static async Task<IResult> GetExportStatus(
        IExportService exportService,
        Guid jobId,
        CancellationToken ct)
    {
        var result = await exportService.GetExportJobStatusAsync(jobId, ct);
        return Results.Ok(result);
    }
    
    // Marketing Email
    private static async Task<IResult> SendMarketingEmail(
        ISender sender,
        [FromBody] SendMarketingEmailRequest request,
        CancellationToken ct)
    {
        var command = new SendMarketingEmailCommand(request.CustomerIds, request.Subject, request.Body, request.TemplateId);
        var result = await sender.Send(command, ct);
        return Results.Accepted(result);
    }
}

// Request DTOs
public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
}

public class AddCustomerNoteRequest
{
    public string Content { get; set; } = string.Empty;
    public bool IsPrivate { get; set; } = true;
}

public class AddCustomerTagsRequest
{
    public List<Guid> TagIds { get; set; } = new();
}

public class ExportRequest
{
    public string ExportType { get; set; } = string.Empty; // Orders, Inventory, Customers
    public Dictionary<string, string?>? Filters { get; set; }
}

public class SendMarketingEmailRequest
{
    public List<Guid> CustomerIds { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid? TemplateId { get; set; }
}
