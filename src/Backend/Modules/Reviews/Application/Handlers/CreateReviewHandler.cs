using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ECommerce.Modules.Reviews.Application.Commands;
using ECommerce.Shared.Events;
using ECommerce.Shared.Interfaces;
using MediatR;

namespace ECommerce.Modules.Reviews.Application.Handlers;

public class CreateReviewHandler : IRequestHandler<CreateReviewCommand, CreateReviewResult>
{
    private readonly IDbConnection _dbConnection;
    private readonly IOutboxService _outboxService;
    private readonly ICacheService _cacheService;

    public CreateReviewHandler(
        IDbConnection dbConnection,
        IOutboxService outboxService,
        ICacheService cacheService)
    {
        _dbConnection = dbConnection;
        _outboxService = outboxService;
        _cacheService = cacheService;
    }

    public async Task<CreateReviewResult> Handle(CreateReviewCommand request, CancellationToken ct)
    {
        await using var transaction = _dbConnection.BeginTransaction();
        
        try
        {
            // Kiểm tra xem order item đã ở trạng thái DELIVERED chưa
            var orderItemStatus = await _dbConnection.ExecuteScalarAsync<string>(@"
                SELECT o.status 
                FROM order_items oi
                JOIN orders o ON oi.order_id = o.id
                WHERE oi.id = @OrderItemId", 
                new { request.OrderItemId }, transaction);
            
            if (orderItemStatus != "delivered")
            {
                return new CreateReviewResult(Guid.Empty, false, "Chỉ có thể đánh giá sau khi đơn hàng đã giao thành công.");
            }
            
            // Kiểm tra xem user đã đánh giá sản phẩm này chưa
            var existingReview = await _dbConnection.ExecuteScalarAsync<Guid?>(@"
                SELECT id FROM reviews 
                WHERE user_id = @UserId AND product_id = @ProductId AND order_item_id = @OrderItemId AND is_deleted = FALSE",
                new { request.UserId, request.ProductId, request.OrderItemId }, transaction);
            
            if (existingReview.HasValue)
            {
                return new CreateReviewResult(Guid.Empty, false, "Bạn đã đánh giá sản phẩm này rồi.");
            }
            
            var reviewId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            
            // Insert review
            await _dbConnection.ExecuteAsync(@"
                INSERT INTO reviews (id, user_id, product_id, order_item_id, rating, title, content, status, created_at, updated_at)
                VALUES (@Id, @UserId, @ProductId, @OrderItemId, @Rating, @Title, @Content, 'pending', @CreatedAt, @UpdatedAt)",
                new
                {
                    Id = reviewId,
                    request.UserId,
                    request.ProductId,
                    request.OrderItemId,
                    request.Rating,
                    request.Title,
                    request.Content,
                    CreatedAt = now,
                    UpdatedAt = now
                }, transaction);
            
            // Insert media nếu có
            if (request.MediaUrls != null && request.MediaTypes != null)
            {
                for (int i = 0; i < request.MediaUrls.Count; i++)
                {
                    await _dbConnection.ExecuteAsync(@"
                        INSERT INTO review_media (id, review_id, media_url, media_type, created_at)
                        VALUES (@Id, @ReviewId, @MediaUrl, @MediaType, @CreatedAt)",
                        new
                        {
                            Id = Guid.NewGuid(),
                            ReviewId = reviewId,
                            MediaUrl = request.MediaUrls[i],
                            MediaType = request.MediaTypes[i],
                            CreatedAt = now
                        }, transaction);
                }
            }
            
            transaction.Commit();
            
            // Publish event qua Outbox
            await _outboxService.SaveEventAsync(
                "Review",
                reviewId,
                nameof(ReviewCreated),
                new ReviewCreated(
                    reviewId,
                    request.UserId,
                    request.ProductId,
                    request.Rating,
                    request.Title ?? string.Empty,
                    request.Content ?? string.Empty,
                    now
                ), ct);
            
            // Invalidate cache của product reviews
            await _cacheService.RemoveByTagAsync($"product_reviews:{request.ProductId}", ct);
            
            return new CreateReviewResult(reviewId, true, "Đánh giá đã được gửi và đang chờ duyệt.");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return new CreateReviewResult(Guid.Empty, false, $"Lỗi: {ex.Message}");
        }
    }
}
