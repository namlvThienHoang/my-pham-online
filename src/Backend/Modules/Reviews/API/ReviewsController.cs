using Microsoft.AspNetCore.Mvc;
using MediatR;
using ECommerce.Modules.Reviews.Application.Commands;
using ECommerce.Modules.Reviews.Application.Queries;

namespace ECommerce.Modules.Reviews.API;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Tạo đánh giá mới (chỉ khi order đã DELIVERED)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
    {
        var command = new CreateReviewCommand(
            request.UserId,
            request.ProductId,
            request.OrderItemId,
            request.Rating,
            request.Title,
            request.Content,
            request.MediaUrls,
            request.MediaTypes
        );
        
        var result = await _mediator.Send(command);
        
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        
        return CreatedAtAction(nameof(GetReviewById), new { id = result.ReviewId }, new { reviewId = result.ReviewId });
    }

    /// <summary>
    /// Lấy danh sách review của sản phẩm
    /// </summary>
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetProductReviews(
        Guid productId,
        int page = 1,
        int pageSize = 20,
        string? sortBy = "created_at",
        string? order = "desc",
        int? ratingFilter = null)
    {
        var query = new GetProductReviewsQuery(productId, page, pageSize, sortBy, order, ratingFilter);
        var result = await _mediator.Send(query);
        
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết review
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetReviewById(Guid id)
    {
        var query = new GetReviewDetailQuery(id);
        var result = await _mediator.Send(query);
        
        if (result.Review == null)
            return NotFound();
        
        return Ok(result.Review);
    }

    /// <summary>
    /// Vote helpful cho review
    /// </summary>
    [HttpPost("{id:guid}/helpful")]
    public async Task<IActionResult> VoteHelpful(Guid id, [FromBody] VoteHelpfulRequest request)
    {
        var command = new VoteHelpfulCommand(id, request.UserId);
        var result = await _mediator.Send(command);
        
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        
        return Ok(new { totalVotes = result.TotalVotes });
    }

    /// <summary>
    /// Admin duyệt review
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveReview(Guid id, [FromBody] ApproveReviewRequest request)
    {
        var command = new ApproveReviewCommand(id, request.AdminUserId);
        var result = await _mediator.Send(command);
        
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Admin từ chối review
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectReview(Guid id, [FromBody] RejectReviewRequest request)
    {
        var command = new RejectReviewCommand(id, request.AdminUserId, request.Reason);
        var result = await _mediator.Send(command);
        
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Xóa review (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id, [FromQuery] Guid userId)
    {
        var command = new DeleteReviewCommand(id, userId);
        var result = await _mediator.Send(command);
        
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        
        return NoContent();
    }
}

// DTOs for API
public record CreateReviewRequest(
    Guid UserId,
    Guid ProductId,
    Guid OrderItemId,
    int Rating,
    string? Title,
    string? Content,
    List<string>? MediaUrls,
    List<string>? MediaTypes
);

public record VoteHelpfulRequest(Guid UserId);
public record ApproveReviewRequest(Guid AdminUserId);
public record RejectReviewRequest(Guid AdminUserId, string Reason);
