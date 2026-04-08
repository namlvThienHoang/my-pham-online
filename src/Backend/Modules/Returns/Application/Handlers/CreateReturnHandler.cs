using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ECommerce.Modules.Returns.Entities;
using ECommerce.Shared.Events;
using ECommerce.Shared.Interfaces;
using MediatR;

namespace ECommerce.Modules.Returns.Application.Handlers;

public record CreateReturnRequestCommand(
    Guid UserId,
    Guid OrderId,
    List<ReturnItemDto> Items,
    string? RefundMethod = "original",
    List<string>? MediaUrls = null
) : IRequest<ReturnRequestResult>;

public record ReturnItemDto(
    Guid OrderItemId,
    int Quantity,
    string Reason,
    decimal RefundAmount
);

public record ReturnRequestResult(
    bool Success,
    string? Message,
    Guid? ReturnRequestId
);

public class CreateReturnRequestHandler : IRequestHandler<CreateReturnRequestCommand, ReturnRequestResult>
{
    private readonly IDbConnection _dbConnection;
    private readonly IOutboxService _outboxService;
    private readonly IIdempotencyService _idempotencyService;

    public CreateReturnRequestHandler(
        IDbConnection dbConnection,
        IOutboxService outboxService,
        IIdempotencyService idempotencyService)
    {
        _dbConnection = dbConnection;
        _outboxService = outboxService;
        _idempotencyService = idempotencyService;
    }

    public async Task<ReturnRequestResult> Handle(CreateReturnRequestCommand request, CancellationToken ct)
    {
        var idempotencyKey = $"create_return:{request.OrderId}:{request.UserId}";
        
        if (!await _idempotencyService.TryAcquireLockAsync(idempotencyKey, TimeSpan.FromSeconds(30), ct))
        {
            return new ReturnRequestResult(false, "Yêu cầu đang được xử lý.", null);
        }

        await using var transaction = _dbConnection.BeginTransaction();
        
        try
        {
            var returnId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var totalRefund = request.Items.Sum(i => i.RefundAmount * i.Quantity);
            
            // Insert return request
            await _dbConnection.ExecuteAsync(@"
                INSERT INTO return_requests (id, user_id, order_id, status, refund_method, total_refund_amount, created_at, updated_at)
                VALUES (@Id, @UserId, @OrderId, 'requested', @RefundMethod, @TotalRefund, @CreatedAt, @UpdatedAt)",
                new
                {
                    Id = returnId,
                    request.UserId,
                    request.OrderId,
                    RefundMethod = request.RefundMethod,
                    TotalRefund = totalRefund,
                    CreatedAt = now,
                    UpdatedAt = now
                }, transaction);
            
            // Insert return items
            foreach (var item in request.Items)
            {
                await _dbConnection.ExecuteAsync(@"
                    INSERT INTO return_items (id, return_request_id, order_item_id, quantity, reason, refund_amount)
                    VALUES (@Id, @ReturnId, @OrderItemId, @Quantity, @Reason, @RefundAmount)",
                    new
                    {
                        Id = Guid.NewGuid(),
                        ReturnId = returnId,
                        item.OrderItemId,
                        item.Quantity,
                        item.Reason,
                        RefundAmount = item.RefundAmount * item.Quantity
                    }, transaction);
            }
            
            // Insert media nếu có
            if (request.MediaUrls != null)
            {
                foreach (var url in request.MediaUrls)
                {
                    await _dbConnection.ExecuteAsync(@"
                        INSERT INTO return_media (id, return_request_id, media_url, created_at)
                        VALUES (@Id, @ReturnId, @MediaUrl, @CreatedAt)",
                        new
                        {
                            Id = Guid.NewGuid(),
                            ReturnId = returnId,
                            MediaUrl = url,
                            CreatedAt = now
                        }, transaction);
                }
            }
            
            transaction.Commit();
            
            // Publish event
            await _outboxService.SaveEventAsync(
                "Return",
                returnId,
                nameof(ReturnRequested),
                new ReturnRequested(returnId, request.UserId, request.OrderId, totalRefund, now),
                ct);
            
            await _idempotencyService.ReleaseLockAsync(idempotencyKey, ct);
            
            return new ReturnRequestResult(true, "Yêu cầu trả hàng đã được gửi.", returnId);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            await _idempotencyService.ReleaseLockAsync(idempotencyKey, ct);
            return new ReturnRequestResult(false, $"Lỗi: {ex.Message}", null);
        }
    }
}
