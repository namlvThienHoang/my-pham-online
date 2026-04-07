namespace BeautyEcommerce.Application.Features.Orders.Queries;

using MediatR;
using Microsoft.EntityFrameworkCore;
using BeautyEcommerce.Infrastructure.Persistence;
using BeautyEcommerce.Domain.Enums;

/// <summary>
/// Query lấy danh sách đơn hàng của user
/// </summary>
public class GetOrdersQuery : IRequest<List<OrderDto>>
{
    public Guid UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; } // Filter by status
}

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public string? TrackingNumber { get; set; }
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? VariantName { get; set; }
}

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    private readonly AppDbContext _dbContext;

    public GetOrdersQueryHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == request.UserId);

        // Filter by status if provided
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<OrderStatus>(request.Status, true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        // Sort by created date descending
        query = query.OrderByDescending(o => o.CreatedAt);

        // Pagination
        query = query.Skip((request.Page - 1) * request.PageSize)
                     .Take(request.PageSize);

        var orders = await query.ToListAsync(cancellationToken);

        return orders.Select(o => new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status.ToString(),
            TotalAmount = o.Total,
            PaymentStatus = o.PaymentStatus.ToString(),
            CreatedAt = o.CreatedAt,
            TrackingNumber = o.TrackingNumber,
            Items = o.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductSku = i.ProductSku,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                VariantName = i.VariantName
            }).ToList()
        }).ToList();
    }
}

/// <summary>
/// Query lấy chi tiết đơn hàng
/// </summary>
public class GetOrderByIdQuery : IRequest<OrderDetailDto?>
{
    public Guid OrderId { get; set; }
}

public class OrderDetailDto : OrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public string? VoucherCode { get; set; }
    public string? CustomerNote { get; set; }
    public string? InternalNote { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? CancellationReason { get; set; }
}

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailDto?>
{
    private readonly AppDbContext _dbContext;

    public GetOrderByIdQueryHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderDetailDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            return null;

        return new OrderDetailDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            TotalAmount = order.Total,
            PaymentStatus = order.PaymentStatus.ToString(),
            CreatedAt = order.CreatedAt,
            TrackingNumber = order.TrackingNumber,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            ShippingAddress = $"{order.ShippingAddressLine1}, {order.ShippingWard}, {order.ShippingDistrict}, {order.ShippingCity}",
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            ShippingFee = order.ShippingAmount,
            VoucherCode = order.VoucherCode,
            CustomerNote = order.CustomerNote,
            InternalNote = order.InternalNote,
            PaidAt = order.PaidAt,
            DeliveredAt = order.DeliveredAt,
            CancellationReason = order.CancellationReason,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductSku = i.ProductSku,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                VariantName = i.VariantName
            }).ToList()
        };
    }
}

/// <summary>
/// Admin query: Lấy danh sách đơn hàng với filter
/// </summary>
public class GetOrdersAdminQuery : IRequest<PagedOrderResult>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; } // Order number, customer name, email
}

public class PagedOrderResult
{
    public List<OrderDto> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class GetOrdersAdminQueryHandler : IRequestHandler<GetOrdersAdminQuery, PagedOrderResult>
{
    private readonly AppDbContext _dbContext;

    public GetOrdersAdminQueryHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedOrderResult> Handle(GetOrdersAdminQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Orders
            .Include(o => o.Items)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<OrderStatus>(request.Status, true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrEmpty(request.PaymentStatus) && Enum.TryParse<PaymentStatus>(request.PaymentStatus, true, out var paymentStatus))
        {
            query = query.Where(o => o.PaymentStatus == paymentStatus);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= request.ToDate.Value);
        }

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(o => 
                o.OrderNumber.Contains(request.SearchTerm) ||
                o.CustomerName.Contains(request.SearchTerm) ||
                o.CustomerEmail.Contains(request.SearchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedOrderResult
        {
            Orders = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status.ToString(),
                TotalAmount = o.Total,
                PaymentStatus = o.PaymentStatus.ToString(),
                CreatedAt = o.CreatedAt,
                TrackingNumber = o.TrackingNumber,
                Items = o.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ProductSku = i.ProductSku,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice,
                    VariantName = i.VariantName
                }).ToList()
            }).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
