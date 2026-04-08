using System;
using MediatR;

namespace ECommerce.Modules.Reviews.Application.Queries;

public record GetProductReviewsQuery(
    Guid ProductId,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = "created_at", // created_at, rating, helpful_count
    string? Order = "desc",
    int? RatingFilter = null, // Lọc theo số sao
    bool IncludePending = false // Chỉ admin mới xem được pending
) : IRequest<ProductReviewsResult>;

public record ProductReviewsResult(
    List<ReviewDto> Reviews,
    int TotalCount,
    double AverageRating,
    int TotalReviews,
    bool HasMore
);

public record ReviewDto(
    Guid Id,
    Guid UserId,
    string UserName,
    int Rating,
    string? Title,
    string? Content,
    string Status,
    DateTime CreatedAt,
    int HelpfulCount,
    bool IsHelpfulVotedByCurrentUser,
    List<ReviewMediaDto> Media
);

public record ReviewMediaDto(
    Guid Id,
    string MediaUrl,
    string MediaType
);

public record GetUserReviewsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20
) : IRequest<UserReviewsResult>;

public record UserReviewsResult(
    List<ReviewDto> Reviews,
    int TotalCount,
    bool HasMore
);

public record GetReviewDetailQuery(
    Guid ReviewId
) : IRequest<ReviewDetailResult>;

public record ReviewDetailResult(
    ReviewDto? Review
);

public record GetReviewsPendingModerationQuery(
    int Page = 1,
    int PageSize = 20
) : IRequest<ReviewsPendingModerationResult>;

public record ReviewsPendingModerationResult(
    List<ReviewDto> Reviews,
    int TotalCount,
    bool HasMore
);
