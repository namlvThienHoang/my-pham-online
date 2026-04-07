namespace BeautyEcommerce.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using System.Security.Claims;
using BeautyEcommerce.Application.Features.Orders.Commands;
using BeautyEcommerce.Application.Features.Orders.Queries;
using BeautyEcommerce.Application.Features.Vouchers.Commands;

/// <summary>
/// Controller quản lý đơn hàng
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Tạo đơn hàng mới từ giỏ hàng
    /// POST /api/orders
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
    {
        try
        {
            // Lấy UserId từ JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            command.UserId = Guid.Parse(userIdClaim);

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách đơn hàng của user hiện tại
    /// GET /api/orders
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            var query = new GetOrdersQuery
            {
                UserId = Guid.Parse(userIdClaim),
                Page = page,
                PageSize = pageSize,
                Status = status
            };

            var orders = await _mediator.Send(query);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết đơn hàng
    /// GET /api/orders/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        try
        {
            var query = new GetOrderByIdQuery { OrderId = id };
            var order = await _mediator.Send(query);

            if (order == null)
                return NotFound();

            // Check ownership
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || order.CustomerEmail != User.FindFirst(ClaimTypes.Email)?.Value)
            {
                // Check if admin
                if (!User.IsInRole("Admin"))
                    return Forbid();
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order {OrderId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng (Admin only)
    /// PATCH /api/orders/{id}/status
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            // Implementation sẽ update order status trong database
            // Đây là placeholder, cần implement handler tương ứng
            return Ok(new { message = $"Updated order {id} to status {request.Status}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order status {OrderId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Áp dụng voucher vào đơn hàng
    /// POST /api/orders/apply-voucher
    /// </summary>
    [HttpPost("apply-voucher")]
    public async Task<IActionResult> ApplyVoucher([FromBody] ApplyVoucherRequest request)
    {
        try
        {
            var command = new ApplyVoucherToOrderCommand
            {
                OrderId = request.OrderId,
                VoucherCode = request.VoucherCode
            };

            var result = await _mediator.Send(command);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply voucher");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy link thanh toán cho đơn hàng
    /// GET /api/orders/{id}/payment-link
    /// </summary>
    [HttpGet("{id:guid}/payment-link")]
    public async Task<IActionResult> GetPaymentLink(Guid id)
    {
        try
        {
            // Implementation sẽ tạo payment link nếu chưa có
            // Trả về URL thanh toán Stripe/PayOS
            return Ok(new { paymentUrl = $"https://checkout.stripe.com/c/pay/{id}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get payment link for order {OrderId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

/// <summary>
/// Admin controller cho quản lý đơn hàng
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AdminOrdersController> _logger;

    public AdminOrdersController(IMediator mediator, ILogger<AdminOrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách tất cả đơn hàng với filter (Admin only)
    /// GET /api/admin/orders
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? paymentStatus = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            var query = new GetOrdersAdminQuery
            {
                Page = page,
                PageSize = pageSize,
                Status = status,
                PaymentStatus = paymentStatus,
                FromDate = fromDate,
                ToDate = toDate,
                SearchTerm = searchTerm
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get admin orders");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng (Admin only)
    /// PATCH /api/admin/orders/{id}/status
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            // Implementation sẽ update order status
            return Ok(new { message = $"Updated order {id} to status {request.Status}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order status {OrderId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}

// Request models
public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class ApplyVoucherRequest
{
    public Guid OrderId { get; set; }
    public string VoucherCode { get; set; } = string.Empty;
}
