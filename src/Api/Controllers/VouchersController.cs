namespace BeautyEcommerce.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using BeautyEcommerce.Application.Features.Vouchers.Commands;

/// <summary>
/// Controller quản lý voucher (Admin only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class VouchersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<VouchersController> _logger;

    public VouchersController(IMediator mediator, ILogger<VouchersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Tạo voucher mới
    /// POST /api/vouchers
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateVoucher([FromBody] CreateVoucherCommand command)
    {
        try
        {
            var voucherId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetVoucherById), new { id = voucherId }, new { id = voucherId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create voucher");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách voucher
    /// GET /api/vouchers
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetVouchers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            // Implementation query để lấy danh sách voucher
            return Ok(new { message = "List of vouchers" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vouchers");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết voucher
    /// GET /api/vouchers/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetVoucherById(Guid id)
    {
        try
        {
            // Implementation query để lấy voucher by id
            return Ok(new { id = id, code = "VOUCHER123" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get voucher {VoucherId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật voucher
    /// PUT /api/vouchers/{id}
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateVoucher(Guid id, [FromBody] UpdateVoucherRequest request)
    {
        try
        {
            // Implementation update voucher
            return Ok(new { message = $"Updated voucher {id}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update voucher {VoucherId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Xóa voucher (soft delete)
    /// DELETE /api/vouchers/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteVoucher(Guid id)
    {
        try
        {
            // Implementation soft delete voucher
            return Ok(new { message = $"Deleted voucher {id}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete voucher {VoucherId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class UpdateVoucherRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Value { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? TotalUsageLimit { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsPublic { get; set; }
}
