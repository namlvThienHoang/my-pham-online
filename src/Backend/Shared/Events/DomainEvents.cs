using System;

namespace ECommerce.Shared.Events;

// ==================== REVIEW EVENTS ====================
public record ReviewCreated(
    Guid ReviewId,
    Guid UserId,
    Guid ProductId,
    int Rating,
    string Title,
    string Content,
    DateTime OccurredOn
);

public record ReviewApproved(
    Guid ReviewId,
    Guid ProductId,
    int Rating,
    DateTime OccurredOn
);

public record ReviewRejected(
    Guid ReviewId,
    string Reason,
    DateTime OccurredOn
);

// ==================== RETURN EVENTS ====================
public record ReturnRequested(
    Guid ReturnId,
    Guid UserId,
    Guid OrderId,
    decimal TotalRefundAmount,
    DateTime OccurredOn
);

public record ReturnApproved(
    Guid ReturnId,
    Guid UserId,
    decimal RefundAmount,
    DateTime OccurredOn
);

public record ReturnReceived(
    Guid ReturnId,
    bool IsApproved,
    DateTime OccurredOn
);

public record RefundProcessed(
    Guid ReturnId,
    string RefundMethod,
    decimal Amount,
    DateTime OccurredOn
);

// ==================== GIFT CARD EVENTS ====================
public record GiftCardPurchased(
    Guid GiftCardId,
    string Code, // Chỉ gửi qua internal event, không lưu log
    decimal Amount,
    Guid PurchasedByUserId,
    DateTime OccurredOn
);

public record GiftCardRedeemed(
    Guid GiftCardId,
    Guid UserId,
    decimal Amount,
    DateTime OccurredOn
);

// ==================== NOTIFICATION EVENTS ====================
public record NotificationQueued(
    Guid NotificationId,
    Guid? UserId,
    string Channel,
    string Recipient,
    string Type,
    DateTime OccurredOn
);
