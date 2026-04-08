using System;
using MediatR;

namespace ECommerce.Modules.Reviews.Application.Commands;

public record CreateReviewCommand(
    Guid UserId,
    Guid ProductId,
    Guid OrderItemId,
    int Rating,
    string? Title,
    string? Content,
    List<string>? MediaUrls,
    List<string>? MediaTypes
) : IRequest<CreateReviewResult>;

public record CreateReviewResult(
    Guid ReviewId,
    bool Success,
    string? Message
);

public record UpdateReviewCommand(
    Guid ReviewId,
    Guid UserId,
    int? Rating,
    string? Title,
    string? Content,
    List<string>? MediaUrlsToAdd,
    List<Guid>? MediaIdsToDelete
) : IRequest<UpdateReviewResult>;

public record UpdateReviewResult(
    bool Success,
    string? Message
);

public record DeleteReviewCommand(
    Guid ReviewId,
    Guid UserId
) : IRequest<DeleteReviewResult>;

public record DeleteReviewResult(
    bool Success,
    string? Message
);

public record VoteHelpfulCommand(
    Guid ReviewId,
    Guid UserId
) : IRequest<VoteHelpfulResult>;

public record VoteHelpfulResult(
    bool Success,
    string? Message,
    int TotalVotes
);

public record ApproveReviewCommand(
    Guid ReviewId,
    Guid AdminUserId
) : IRequest<ApproveReviewResult>;

public record ApproveReviewResult(
    bool Success,
    string? Message
);

public record RejectReviewCommand(
    Guid ReviewId,
    Guid AdminUserId,
    string Reason
) : IRequest<RejectReviewResult>;

public record RejectReviewResult(
    bool Success,
    string? Message
);
