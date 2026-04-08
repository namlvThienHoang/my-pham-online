using System.Data;
using Dapper;
using Npgsql;
using ECommerce.Core.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Modules.Admin.DTOs;
using ECommerce.Modules.Admin.Services;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Admin.Services.Implementations;

public class AdminOrderService : IAdminOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly string _connectionString;

    public AdminOrderService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<OrderListResultDto> GetOrdersWithCursorPaginationAsync(
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        string? paymentMethod,
        string? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        // Build WHERE clause
        var whereClauses = new List<string> { "o.is_deleted = false" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(status))
        {
            whereClauses.Add("o.status = @Status");
            parameters.Add("Status", status);
        }

        if (fromDate.HasValue)
        {
            whereClauses.Add("o.created_at >= @FromDate");
            parameters.Add("FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            whereClauses.Add("o.created_at <= @ToDate");
            parameters.Add("ToDate", toDate.Value);
        }

        if (!string.IsNullOrEmpty(paymentMethod))
        {
            whereClauses.Add("pm.code = @PaymentMethod");
            parameters.Add("PaymentMethod", paymentMethod);
        }

        var whereClause = string.Join(" AND ", whereClauses);

        // Cursor pagination - decode cursor to get last seen values
        DateTime? cursorCreatedAt = null;
        Guid? cursorId = null;

        if (!string.IsNullOrEmpty(cursor))
        {
            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
                var parts = decoded.Split('|');
                if (parts.Length == 2)
                {
                    cursorCreatedAt = DateTime.Parse(parts[0]);
                    cursorId = Guid.Parse(parts[1]);
                }
            }
            catch
            {
                // Invalid cursor, ignore
            }
        }

        // If we have a cursor, add it to the WHERE clause
        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            whereClauses.Add("(o.created_at, o.id) < (@CursorCreatedAt, @CursorId)");
            parameters.Add("CursorCreatedAt", cursorCreatedAt.Value);
            parameters.Add("CursorId", cursorId.Value);
        }

        whereClause = string.Join(" AND ", whereClauses);

        // Query orders with JOINs
        var sql = $@"
            SELECT 
                o.id, o.order_number, o.status, o.total_amount, o.created_at, o.updated_at,
                u.id as user_id, u.email as customer_email, u.full_name as customer_name, u.phone as customer_phone,
                pm.id as payment_method_id, pm.name as payment_method_name, pm.code as payment_method_code,
                sa.city as shipping_city, sa.district as shipping_district, sa.ward as shipping_ward, sa.street_address as shipping_street
            FROM orders o
            INNER JOIN users u ON o.user_id = u.id
            INNER JOIN payment_methods pm ON o.payment_method_id = pm.id
            LEFT JOIN shipping_addresses sa ON o.shipping_address_id = sa.id
            WHERE {whereClause}
            ORDER BY o.created_at DESC, o.id DESC
            LIMIT @Limit";

        parameters.Add("Limit", limit + 1); // Fetch one extra to check if there's more

        var orders = await connection.QueryAsync<OrderCustomerPaymentDto>(sql, parameters);
        var orderList = orders.ToList();

        // Check if there's a next page
        var hasMore = orderList.Count > limit;
        if (hasMore)
        {
            orderList.RemoveAt(orderList.Count - 1); // Remove the extra item
        }

        // Build next cursor
        string? nextCursor = null;
        if (hasMore && orderList.Any())
        {
            var lastOrder = orderList.Last();
            var cursorData = $"{lastOrder.CreatedAt:yyyy-MM-dd HH:mm:ss.ffffff}|{lastOrder.Id}";
            nextCursor = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(cursorData));
        }

        // Map to DTOs
        var result = new OrderListResultDto
        {
            Orders = orderList.Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                Customer = new CustomerDto
                {
                    Id = o.UserId,
                    Email = o.CustomerEmail,
                    FullName = o.CustomerName,
                    Phone = o.CustomerPhone
                },
                PaymentMethod = new PaymentMethodDto
                {
                    Id = o.PaymentMethodId,
                    Name = o.PaymentMethodName,
                    Code = o.PaymentMethodCode
                },
                ShippingAddress = !string.IsNullOrEmpty(o.ShippingCity) ? new ShippingAddressDto
                {
                    City = o.ShippingCity,
                    District = o.ShippingDistrict,
                    Ward = o.ShippingWard,
                    StreetAddress = o.ShippingStreet
                } : null
            }).ToList(),
            NextCursor = nextCursor,
            HasMore = hasMore,
            TotalCount = await GetTotalCountAsync(status, fromDate, toDate, paymentMethod, cancellationToken)
        };

        return result;
    }

    private async Task<int> GetTotalCountAsync(
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        string? paymentMethod,
        CancellationToken cancellationToken)
    {
        var query = _context.Orders.AsQueryable().Where(o => !o.IsDeleted);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value);

        if (!string.IsNullOrEmpty(paymentMethod))
            query = query.Where(o => o.PaymentMethod.Code == paymentMethod);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<OrderDto> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.PaymentMethod)
            .Include(o => o.OrderItems).ThenJoin(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted, cancellationToken);

        if (order == null)
            throw new KeyNotFoundException($"Order {orderId} not found");

        return MapToOrderDto(order);
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(
        Guid orderId,
        string status,
        string? internalNotes,
        CancellationToken cancellationToken)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var order = await _context.Orders.FindAsync(new object[] { orderId }, cancellationToken);
            if (order == null || order.IsDeleted)
                throw new KeyNotFoundException($"Order {orderId} not found");

            var oldStatus = order.Status;
            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Add internal note if provided
            if (!string.IsNullOrEmpty(internalNotes))
            {
                var note = new OrderInternalNote
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    Note = internalNotes,
                    CreatedBy = "admin",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.OrderInternalNotes.Add(note);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Log audit - this will be handled by the audit interceptor
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return await GetOrderByIdAsync(orderId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private OrderDto MapToOrderDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Customer = new CustomerDto
            {
                Id = order.User.Id,
                Email = order.User.Email,
                FullName = order.User.FullName,
                Phone = order.User.Phone
            },
            PaymentMethod = new PaymentMethodDto
            {
                Id = order.PaymentMethod.Id,
                Name = order.PaymentMethod.Name,
                Code = order.PaymentMethod.Code
            },
            Items = order.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.Product.Name,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice
            }).ToList()
        };
    }
}

// Helper DTO for Dapper query
public class OrderCustomerPaymentDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public Guid UserId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    public Guid PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public string PaymentMethodCode { get; set; } = string.Empty;
    
    public string? ShippingCity { get; set; }
    public string? ShippingDistrict { get; set; }
    public string? ShippingWard { get; set; }
    public string? ShippingStreet { get; set; }
}
