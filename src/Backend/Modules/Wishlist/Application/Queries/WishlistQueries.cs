using System;
using MediatR;

namespace ECommerce.Modules.Wishlist.Application.Queries;

public record GetWishlistQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20
) : IRequest<WishlistResult>;

public record WishlistResult(
    List<WishlistItemDto> Items,
    int TotalCount,
    bool HasMore
);

public record WishlistItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductImage,
    decimal? Price,
    Guid? VariantId,
    string? VariantName,
    DateTime AddedAt
);

public record GetRecentlyViewedQuery(
    Guid UserId,
    string? Cursor = null,
    int Limit = 20
) : IRequest<RecentlyViewedResult>;

public record RecentlyViewedResult(
    List<RecentlyViewedItemDto> Items,
    string? NextCursor,
    bool HasMore
);

public record RecentlyViewedItemDto(
    Guid ProductId,
    string ProductName,
    string? ProductImage,
    decimal? Price,
    DateTime ViewedAt
);
