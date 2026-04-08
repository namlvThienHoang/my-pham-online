using Microsoft.AspNetCore.Mvc;
using MediatR;
using ECommerce.Modules.Wishlist.Application.Commands;
using ECommerce.Modules.Wishlist.Application.Queries;

namespace ECommerce.Modules.Wishlist.API;

[ApiController]
[Route("api/[controller]")]
public class WishlistController : ControllerBase
{
    private readonly IMediator _mediator;

    public WishlistController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách yêu thích của user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWishlist(
        [FromQuery] Guid userId,
        int page = 1,
        int pageSize = 20)
    {
        var query = new GetWishlistQuery(userId, page, pageSize);
        var result = await _mediator.Send(query);
        
        return Ok(result);
    }

    /// <summary>
    /// Thêm sản phẩm vào wishlist
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistRequest request)
    {
        var command = new AddToWishlistCommand(request.UserId, request.ProductId, request.VariantId);
        var result = await _mediator.Send(command);
        
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Xóa sản phẩm khỏi wishlist
    /// </summary>
    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> RemoveFromWishlist(
        Guid productId,
        [FromQuery] Guid userId,
        [FromQuery] Guid? variantId = null)
    {
        var command = new RemoveFromWishlistCommand(userId, productId, variantId);
        var result = await _mediator.Send(command);
        
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Ghi nhận xem sản phẩm (cho recently viewed)
    /// </summary>
    [HttpPost("view")]
    public async Task<IActionResult> RecordView([FromBody] RecordViewRequest request)
    {
        var command = new RecordProductViewCommand(request.UserId, request.ProductId);
        var result = await _mediator.Send(command);
        
        return result.Success ? Ok() : BadRequest();
    }

    /// <summary>
    /// Lấy danh sách sản phẩm đã xem gần đây (cursor pagination)
    /// </summary>
    [HttpGet("recently-viewed")]
    public async Task<IActionResult> GetRecentlyViewed(
        [FromQuery] Guid userId,
        string? cursor = null,
        int limit = 20)
    {
        var query = new GetRecentlyViewedQuery(userId, cursor, limit);
        var result = await _mediator.Send(query);
        
        return Ok(result);
    }
}

// DTOs
public record AddToWishlistRequest(
    Guid UserId,
    Guid ProductId,
    Guid? VariantId = null
);

public record RecordViewRequest(
    Guid UserId,
    Guid ProductId
);
