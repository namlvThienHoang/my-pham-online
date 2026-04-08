using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using ECommerce.Shared.Events;
using ECommerce.Shared.Helpers;
using ECommerce.Shared.Interfaces;
using MediatR;

namespace ECommerce.Modules.Wallet.Application.Handlers;

public record CreateGiftCardCommand(decimal Amount, Guid? PurchasedByUserId, DateTime? ExpiresAt) 
    : IRequest<GiftCardResult>;

public record RedeemGiftCardCommand(string Code, Guid UserId) : IRequest<GiftCardResult>;

public record GiftCardResult(bool Success, string? Message, decimal? Balance);

public class CreateGiftCardHandler : IRequestHandler<CreateGiftCardCommand, GiftCardResult>
{
    private readonly IDbConnection _dbConnection;
    private readonly IOutboxService _outboxService;

    public CreateGiftCardHandler(IDbConnection dbConnection, IOutboxService outboxService)
    {
        _dbConnection = dbConnection;
        _outboxService = outboxService;
    }

    public async Task<GiftCardResult> Handle(CreateGiftCardCommand request, CancellationToken ct)
    {
        await using var transaction = _dbConnection.BeginTransaction();
        
        try
        {
            var giftCardId = Guid.NewGuid();
            var code = GiftCardHelper.GenerateGiftCardCode();
            var codeHash = GiftCardHelper.HashGiftCardCode(code);
            var now = DateTime.UtcNow;
            
            // Insert gift card
            await _dbConnection.ExecuteAsync(@"
                INSERT INTO gift_cards (id, code_hash, original_code, initial_balance, current_balance, expires_at, status, purchased_by_user_id, created_at)
                VALUES (@Id, @CodeHash, @OriginalCode, @InitialBalance, @CurrentBalance, @ExpiresAt, 'active', @PurchasedByUserId, @CreatedAt)",
                new
                {
                    Id = giftCardId,
                    CodeHash = codeHash,
                    OriginalCode = code,
                    InitialBalance = request.Amount,
                    CurrentBalance = request.Amount,
                    ExpiresAt = request.ExpiresAt,
                    PurchasedByUserId = request.PurchasedByUserId,
                    CreatedAt = now
                }, transaction);
            
            transaction.Commit();
            
            // Publish event (chỉ internal, không log code thật)
            await _outboxService.SaveEventAsync(
                "GiftCard",
                giftCardId,
                nameof(GiftCardPurchased),
                new GiftCardPurchased(giftCardId, code, request.Amount, request.PurchasedByUserId ?? Guid.Empty, now),
                ct);
            
            return new GiftCardResult(true, "Gift card đã được tạo.", request.Amount);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return new GiftCardResult(false, $"Lỗi: {ex.Message}", null);
        }
    }
}

public class RedeemGiftCardHandler : IRequestHandler<RedeemGiftCardCommand, GiftCardResult>
{
    private readonly IDbConnection _dbConnection;
    private readonly IIdempotencyService _idempotencyService;

    public RedeemGiftCardHandler(IDbConnection dbConnection, IIdempotencyService idempotencyService)
    {
        _dbConnection = dbConnection;
        _idempotencyService = idempotencyService;
    }

    public async Task<GiftCardResult> Handle(RedeemGiftCardCommand request, CancellationToken ct)
    {
        // Idempotency key
        var idempotencyKey = $"redeem_gc:{request.Code}:{request.UserId}";
        
        if (!await _idempotencyService.TryAcquireLockAsync(idempotencyKey, TimeSpan.FromSeconds(30), ct))
        {
            return new GiftCardResult(false, "Yêu cầu đang được xử lý.", null);
        }

        await using var transaction = _dbConnection.BeginTransaction();
        
        try
        {
            var codeHash = GiftCardHelper.HashGiftCardCode(request.Code);
            var now = DateTime.UtcNow;
            
            // Lookup gift card by hash
            var giftCard = await _dbConnection.QueryFirstOrDefaultAsync(@"
                SELECT * FROM gift_cards 
                WHERE code_hash = @CodeHash AND status = 'active' 
                  AND (expires_at IS NULL OR expires_at > @Now)",
                new { CodeHash = codeHash, Now = now }, transaction);
            
            if (giftCard == null)
            {
                await _idempotencyService.ReleaseLockAsync(idempotencyKey, ct);
                return new GiftCardResult(false, "Gift card không hợp lệ hoặc đã hết hạn.", null);
            }
            
            // Check if user has wallet, create if not
            var wallet = await _dbConnection.QueryFirstOrDefaultAsync(
                "SELECT * FROM wallets WHERE user_id = @UserId",
                new { request.UserId }, transaction);
            
            if (wallet == null)
            {
                await _dbConnection.ExecuteAsync(
                    "INSERT INTO wallets (user_id, balance, currency) VALUES (@UserId, 0, 'VND')",
                    new { request.UserId }, transaction);
            }
            
            // Update gift card status
            await _dbConnection.ExecuteAsync(@"
                UPDATE gift_cards SET status = 'used', current_balance = 0 
                WHERE id = @Id",
                new { giftCard.Id }, transaction);
            
            // Add to wallet
            var newBalance = (wallet?.Balance ?? 0m) + giftCard.CurrentBalance;
            await _dbConnection.ExecuteAsync(@"
                UPDATE wallets SET balance = @NewBalance, row_version = row_version + 1 
                WHERE user_id = @UserId",
                new { NewBalance = newBalance, request.UserId }, transaction);
            
            // Record transaction
            await _dbConnection.ExecuteAsync(@"
                INSERT INTO wallet_transactions (id, wallet_id, type, amount, balance_after, reference_id, description, created_at)
                VALUES (@Id, @WalletId, 'gift_card_load', @Amount, @BalanceAfter, @ReferenceId, @Description, @CreatedAt)",
                new
                {
                    Id = Guid.NewGuid(),
                    WalletId = request.UserId,
                    Amount = giftCard.CurrentBalance,
                    BalanceAfter = newBalance,
                    ReferenceId = giftCard.Id,
                    Description = $"Nạp từ gift card",
                    CreatedAt = now
                }, transaction);
            
            transaction.Commit();
            await _idempotencyService.ReleaseLockAsync(idempotencyKey, ct);
            
            return new GiftCardResult(true, $"Đã nạp {giftCard.CurrentBalance:C} vào ví.", newBalance);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            await _idempotencyService.ReleaseLockAsync(idempotencyKey, ct);
            return new GiftCardResult(false, $"Lỗi: {ex.Message}", null);
        }
    }
}
