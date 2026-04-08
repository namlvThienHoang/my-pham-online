using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ECommerce.Modules.Reviews.Application.Queries;
using MediatR;

namespace ECommerce.Modules.Reviews.Application.Handlers;

public class GetProductReviewsHandler : IRequestHandler<GetProductReviewsQuery, ProductReviewsResult>
{
    private readonly IDbConnection _dbConnection;

    public GetProductReviewsHandler(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<ProductReviewsResult> Handle(GetProductReviewsQuery request, CancellationToken ct)
    {
        // Build dynamic SQL based on filters
        var sql = @"
            SELECT 
                r.id, r.user_id, u.full_name as user_name, r.rating, r.title, r.content, 
                r.status, r.created_at,
                COUNT(DISTINCT rv.user_id) as helpful_count
            FROM reviews r
            JOIN users u ON r.user_id = u.id
            LEFT JOIN review_helpful_votes rv ON r.id = rv.review_id
            WHERE r.product_id = @ProductId 
              AND r.is_deleted = FALSE
              AND (r.status = 'approved' OR @IncludePending = TRUE)
              AND (@RatingFilter IS NULL OR r.rating = @RatingFilter)
            GROUP BY r.id, r.user_id, u.full_name, r.rating, r.title, r.content, r.status, r.created_at
        ";
        
        var countSql = @"
            SELECT COUNT(DISTINCT r.id)
            FROM reviews r
            WHERE r.product_id = @ProductId 
              AND r.is_deleted = FALSE
              AND (r.status = 'approved' OR @IncludePending = TRUE)
              AND (@RatingFilter IS NULL OR r.rating = @RatingFilter)
        ";

        // Sorting
        sql += $" ORDER BY {request.SortBy} {request.Order}";
        
        // Pagination
        var offset = (request.Page - 1) * request.PageSize;
        sql += " LIMIT @PageSize OFFSET @Offset";

        // Execute queries
        var parameters = new
        {
            request.ProductId,
            request.IncludePending,
            RatingFilter = request.RatingFilter,
            PageSize = request.PageSize,
            Offset = offset
        };

        var reviews = await _dbConnection.QueryAsync<(ReviewDto, ReviewMediaDto?)>(@"
            WITH review_data AS (" + sql.Replace("COUNT(DISTINCT rv.user_id)", "COUNT(DISTINCT rv.user_id)::int") + @")
            SELECT 
                rd.id, rd.user_id, rd.user_name, rd.rating, rd.title, rd.content, 
                rd.status, rd.created_at, rd.helpful_count,
                rm.id as media_id, rm.media_url, rm.media_type
            FROM review_data rd
            LEFT JOIN review_media rm ON rd.id = rm.review_id
            ORDER BY rd.created_at DESC", 
            parameters);

        var totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql, parameters);
        
        // Get average rating
        var avgRating = await _dbConnection.ExecuteScalarAsync<double>(@"
            SELECT COALESCE(AVG(rating), 0)
            FROM reviews
            WHERE product_id = @ProductId AND status = 'approved' AND is_deleted = FALSE",
            new { request.ProductId });

        // Group by review
        var reviewDict = new Dictionary<Guid, ReviewDto>();
        foreach (var row in reviews)
        {
            var review = row.Item1;
            if (!reviewDict.ContainsKey(review.Id))
            {
                reviewDict[review.Id] = new ReviewDto(
                    review.Id,
                    review.UserId,
                    review.UserName,
                    review.Rating,
                    review.Title,
                    review.Content,
                    review.Status,
                    review.CreatedAt,
                    review.HelpfulCount,
                    false, // IsHelpfulVotedByCurrentUser - cần user context để set
                    new List<ReviewMediaDto>()
                );
            }
            
            if (row.Item2.HasValue && !string.IsNullOrEmpty(row.Item2.Value.MediaUrl))
            {
                reviewDict[review.Id].Media.Add(new ReviewMediaDto(
                    row.Item2.Value.MediaId ?? Guid.Empty,
                    row.Item2.Value.MediaUrl,
                    row.Item2.Value.MediaType
                ));
            }
        }

        return new ProductReviewsResult(
            reviewDict.Values.ToList(),
            totalCount,
            avgRating,
            totalCount,
            offset + request.PageSize < totalCount
        );
    }
}
