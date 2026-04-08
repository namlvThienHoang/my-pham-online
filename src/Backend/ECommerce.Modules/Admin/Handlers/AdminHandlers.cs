using MediatR;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Modules.Admin.DTOs;
using ECommerce.Modules.Admin.Services;

namespace ECommerce.Modules.Admin.Handlers;

// Dashboard Stats
public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public class GetDashboardStatsHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IAdminDashboardService _dashboardService;
    
    public GetDashboardStatsHandler(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }
    
    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        return await _dashboardService.GetDashboardStatsAsync(cancellationToken);
    }
}

// Orders List with Cursor Pagination
public record GetOrdersListQuery(
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    string? PaymentMethod,
    string? Cursor,
    int Limit = 20
) : IRequest<OrderListResultDto>;

public class GetOrdersListHandler : IRequestHandler<GetOrdersListQuery, OrderListResultDto>
{
    private readonly IAdminOrderService _orderService;
    
    public GetOrdersListHandler(IAdminOrderService orderService)
    {
        _orderService = orderService;
    }
    
    public async Task<OrderListResultDto> Handle(GetOrdersListQuery request, CancellationToken cancellationToken)
    {
        return await _orderService.GetOrdersWithCursorPaginationAsync(
            request.Status,
            request.FromDate,
            request.ToDate,
            request.PaymentMethod,
            request.Cursor,
            request.Limit,
            cancellationToken);
    }
}

// Update Order Status
public record UpdateOrderStatusCommand(Guid OrderId, string Status, string? InternalNotes) : IRequest<OrderDto>;

public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, OrderDto>
{
    private readonly IAdminOrderService _orderService;
    
    public UpdateOrderStatusHandler(IAdminOrderService orderService)
    {
        _orderService = orderService;
    }
    
    public async Task<OrderDto> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        return await _orderService.UpdateOrderStatusAsync(request.OrderId, request.Status, request.InternalNotes, cancellationToken);
    }
}

// Get Customer Profile
public record GetCustomerProfileQuery(Guid CustomerId) : IRequest<CustomerProfileDto>;

public class GetCustomerProfileHandler : IRequestHandler<GetCustomerProfileQuery, CustomerProfileDto>
{
    private readonly IAdminCustomerService _customerService;
    
    public GetCustomerProfileHandler(IAdminCustomerService customerService)
    {
        _customerService = customerService;
    }
    
    public async Task<CustomerProfileDto> Handle(GetCustomerProfileQuery request, CancellationToken cancellationToken)
    {
        return await _customerService.GetCustomerProfileAsync(request.CustomerId, cancellationToken);
    }
}

// Add Customer Note
public record AddCustomerNoteCommand(Guid CustomerId, string Content, bool IsPrivate) : IRequest<CustomerNoteDto>;

public class AddCustomerNoteHandler : IRequestHandler<AddCustomerNoteCommand, CustomerNoteDto>
{
    private readonly IAdminCustomerService _customerService;
    
    public AddCustomerNoteHandler(IAdminCustomerService customerService)
    {
        _customerService = customerService;
    }
    
    public async Task<CustomerNoteDto> Handle(AddCustomerNoteCommand request, CancellationToken cancellationToken)
    {
        return await _customerService.AddCustomerNoteAsync(request.CustomerId, request.Content, request.IsPrivate, cancellationToken);
    }
}

// Get Audit Logs with Cursor Pagination
public record GetAuditLogsQuery(
    Guid? UserId,
    string? EntityType,
    string? Action,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Cursor,
    int Limit = 20
) : IRequest<AuditLogListResultDto>;

public class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, AuditLogListResultDto>
{
    private readonly IAuditLogService _auditLogService;
    
    public GetAuditLogsHandler(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }
    
    public async Task<AuditLogListResultDto> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        return await _auditLogService.GetAuditLogsAsync(
            request.UserId,
            request.EntityType,
            request.Action,
            request.FromDate,
            request.ToDate,
            request.Cursor,
            request.Limit,
            cancellationToken);
    }
}

// Export Data (Background Job Trigger)
public record ExportDataCommand(string ExportType, Dictionary<string, string?> Filters) : IRequest<ExportJobResultDto>;

public class ExportDataHandler : IRequestHandler<ExportDataCommand, ExportJobResultDto>
{
    private readonly IExportService _exportService;
    
    public ExportDataHandler(IExportService exportService)
    {
        _exportService = exportService;
    }
    
    public async Task<ExportJobResultDto> Handle(ExportDataCommand request, CancellationToken cancellationToken)
    {
        return await _exportService.StartExportJobAsync(request.ExportType, request.Filters, cancellationToken);
    }
}

// Send Marketing Email (Batch)
public record SendMarketingEmailCommand(List<Guid> CustomerIds, string Subject, string Body, Guid? TemplateId) : IRequest<SendEmailResultDto>;

public class SendMarketingEmailHandler : IRequestHandler<SendMarketingEmailCommand, SendEmailResultDto>
{
    private readonly IMarketingEmailService _emailService;
    
    public SendMarketingEmailHandler(IMarketingEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public async Task<SendEmailResultDto> Handle(SendMarketingEmailCommand request, CancellationToken cancellationToken)
    {
        return await _emailService.SendBatchEmailsAsync(request.CustomerIds, request.Subject, request.Body, request.TemplateId, cancellationToken);
    }
}
