using BeautyCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BeautyCommerce.Infrastructure.Outbox;

/// <summary>
/// Worker xử lý Outbox messages với pattern: SKIP LOCKED, lease, retry, dead letter.
/// Chạy như một Hangfire recurring job.
/// </summary>
public interface IOutboxProcessor
{
    Task ProcessAsync();
}

public class OutboxProcessor : IOutboxProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly string _workerId;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);
    private const int BatchSize = 50;
    private const int MaxRetries = 5;

    public OutboxProcessor(AppDbContext dbContext, ILogger<OutboxProcessor> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _workerId = Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..8];
    }

    public async Task ProcessAsync()
    {
        _logger.LogInformation("Starting outbox processing with worker {WorkerId}", _workerId);

        try
        {
            var messagesToProcess = await GetMessagesWithLeaseAsync();

            foreach (var message in messagesToProcess)
            {
                await ProcessMessageAsync(message);
            }

            _logger.LogInformation("Processed {Count} messages", messagesToProcess.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing outbox messages");
            throw;
        }
    }

    private async Task<List<OutboxMessage>> GetMessagesWithLeaseAsync()
    {
        var now = DateTime.UtcNow;

        // Sử dụng raw SQL với SKIP LOCKED để lấy messages chưa được xử lý và chưa bị lock
        var sql = @"
            SELECT * FROM outbox_messages
            WHERE processed_at IS NULL 
              AND (lease_expires_at IS NULL OR lease_expires_at < @now)
              AND retry_count < @maxRetries
            ORDER BY occurred_at ASC
            LIMIT @batchSize
            FOR UPDATE SKIP LOCKED";

        var messages = await _dbContext.OutboxMessages
            .FromSqlRaw(sql, 
                new Npgsql.NpgsqlParameter("now", now),
                new Npgsql.NpgsqlParameter("maxRetries", MaxRetries),
                new Npgsql.NpgsqlParameter("batchSize", BatchSize))
            .ToListAsync();

        // Acquire lease cho từng message
        var leasedMessages = new List<OutboxMessage>();
        foreach (var message in messages)
        {
            if (message.AcquireLease(_workerId, LeaseDuration))
            {
                leasedMessages.Add(message);
            }
        }

        if (leasedMessages.Any())
        {
            await _dbContext.SaveChangesAsync();
        }

        return leasedMessages;
    }

    private async Task ProcessMessageAsync(OutboxMessage message)
    {
        try
        {
            _logger.LogDebug("Processing message {MessageId} of type {EventType}", message.Id, message.EventType);

            // Deserialize payload
            var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Payload);
            
            // TODO: Dispatch to event handlers based on EventType
            // Đây là nơi gọi các handler thực tế để publish events
            await DispatchEventAsync(message.EventType, payload!);

            // Mark as processed
            message.MarkAsProcessed();
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully processed message {MessageId}", message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {MessageId}", message.Id);
            
            message.MarkAsFailed(ex.Message);
            
            if (message.RetryCount >= MaxRetries)
            {
                _logger.LogError("Message {MessageId} exceeded max retries, moving to dead letter", message.Id);
                // TODO: Move to dead letter queue
            }
            
            await _dbContext.SaveChangesAsync();
        }
    }

    private Task DispatchEventAsync(string eventType, Dictionary<string, object> payload)
    {
        // TODO: Implement actual event dispatching logic
        // Có thể gọi EventBus, Message Broker, hoặc invoke handlers trực tiếp
        _logger.LogInformation("Dispatching event {EventType} with payload {Payload}", eventType, JsonSerializer.Serialize(payload));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Background task để renew lease cho các messages đang xử lý.
    /// </summary>
    public async Task RenewLeasesAsync()
    {
        var activeMessages = await _dbContext.OutboxMessages
            .Where(m => m.WorkerId == _workerId && m.ProcessedAt == null)
            .ToListAsync();

        foreach (var message in activeMessages)
        {
            message.RenewLease(LeaseDuration);
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Renewed leases for {Count} messages", activeMessages.Count);
    }
}
