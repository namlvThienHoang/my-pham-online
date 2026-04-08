using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ECommerce.Modules.Wishlist.Application.Commands;
using ECommerce.Shared.Interfaces;
using MediatR;

namespace ECommerce.Modules.Wishlist.Application.Handlers;

public class AddToWishlistHandler : IRequestHandler<AddToWishlistCommand, WishlistResult>
{
    private readonly IDbConnection _dbConnection;
    private readonly ICacheService _cacheService;

    public AddToWishlistHandler(IDbConnection dbConnection, ICacheService cacheService)
    {
        _dbConnection = dbConnection;
        _cacheService = cacheService;
    }

    public async Task<WishlistResult> Handle(AddToWishlistCommand request, CancellationToken ct)
    {
        try
        {
            var now = DateTime.UtcNow;
            
            // UPSERT wishlist (ON CONFLICT DO UPDATE)
            await _dbConnection.ExecuteAsync(@"
                INSERT INTO wishlists (id, user_id, product_id, variant_id, added_at, is_deleted)
                VALUES (@Id, @UserId, @ProductId, @VariantId, @AddedAt, FALSE)
                ON CONFLICT (user_id, product_id, COALESCE(variant_id, '00000000-0000-0000-0000-000000000000'::uuid)) 
                DO UPDATE SET 
                    is_deleted = FALSE,
                    added_at = EXCLUDED.added_at",
                new
                {
                    Id = Guid.NewGuid(),
                    request.UserId,
                    request.ProductId,
                    VariantId = request.VariantId ?? Guid.Empty,
                    AddedAt = now
                });
            
            // Invalidate cache
            await _cacheService.RemoveByTagAsync($"wishlist:{request.UserId}", ct);
            
            return new WishlistResult(true, "Đã thêm vào danh sách yêu thích.");
        }
        catch (Exception ex)
        {
            return new WishlistResult(false, $"Lỗi: {ex.Message}");
        }
    }
}

public class RemoveFromWishlistHandler : IRequestHandler<RemoveFromWishlistCommand, WishlistResult>
{
    private readonly IDbConnection _dbConnection;
    private readonly ICacheService _cacheService;

    public RemoveFromWishlistHandler(IDbConnection dbConnection, ICacheService cacheService)
    {
        _dbConnection = dbConnection;
        _cacheService = cacheService;
    }

    public async Task<WishlistResult> Handle(RemoveFromWishlistCommand request, CancellationToken ct)
    {
        try
        {
            // Soft delete
            await _dbConnection.ExecuteAsync(@"
                UPDATE wishlists 
                SET is_deleted = TRUE 
                WHERE user_id = @UserId AND product_id = @ProductId 
                  AND (@VariantId IS NULL OR variant_id = @VariantId)",
                new
                {
                    request.UserId,
                    request.ProductId,
                    VariantId = request.VariantId
                });
            
            // Invalidate cache
            await _cacheService.RemoveByTagAsync($"wishlist:{request.UserId}", ct);
            
            return new WishlistResult(true, "Đã xóa khỏi danh sách yêu thích.");
        }
        catch (Exception ex)
        {
            return new WishlistResult(false, $"Lỗi: {ex.Message}");
        }
    }
}

public class RecordProductViewHandler : IRequestHandler<RecordProductViewCommand, ViewResult>
{
    private readonly IDbConnection _dbConnection;
    private readonly ICacheService _cacheService;

    public RecordProductViewHandler(IDbConnection dbConnection, ICacheService cacheService)
    {
        _dbConnection = dbConnection;
        _cacheService = cacheService;
    }

    public async Task<ViewResult> Handle(RecordProductViewCommand request, CancellationToken ct)
    {
        try
        {
            var now = DateTime.UtcNow;
            
            // UPSERT recently viewed, giữ max 20 sản phẩm/user
            await _dbConnection.ExecuteAsync(@"
                INSERT INTO user_recently_viewed (user_id, product_id, viewed_at)
                VALUES (@UserId, @ProductId, @ViewedAt)
                ON CONFLICT (user_id, product_id) 
                DO UPDATE SET viewed_at = EXCLUDED.viewed_at",
                new
                {
                    request.UserId,
                    request.ProductId,
                    ViewedAt = now
                });
            
            // Xóa các bản ghi cũ nếu vượt quá 20
            await _dbConnection.ExecuteAsync(@"
                DELETE FROM user_recently_viewed
                WHERE user_id = @UserId AND id NOT IN (
                    SELECT id FROM user_recently_viewed 
                    WHERE user_id = @UserId 
                    ORDER BY viewed_at DESC 
                    LIMIT 20
                )",
                new { request.UserId });
            
            // Invalidate cache
            await _cacheService.RemoveByTagAsync($"recently_viewed:{request.UserId}", ct);
            
            return new ViewResult(true);
        }
        catch (Exception ex)
        {
            return new ViewResult(false);
        }
    }
}
