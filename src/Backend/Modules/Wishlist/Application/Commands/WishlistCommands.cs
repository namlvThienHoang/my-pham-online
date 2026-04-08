using System;
using MediatR;

namespace ECommerce.Modules.Wishlist.Application.Commands;

public record AddToWishlistCommand(
    Guid UserId,
    Guid ProductId,
    Guid? VariantId = null
) : IRequest<WishlistResult>;

public record RemoveFromWishlistCommand(
    Guid UserId,
    Guid ProductId,
    Guid? VariantId = null
) : IRequest<WishlistResult>;

public record WishlistResult(
    bool Success,
    string? Message
);

public record RecordProductViewCommand(
    Guid UserId,
    Guid ProductId
) : IRequest<ViewResult>;

public record ViewResult(
    bool Success
);
