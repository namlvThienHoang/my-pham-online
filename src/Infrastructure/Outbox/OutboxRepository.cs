namespace BeautyEcommerce.Infrastructure.Outbox;

using Microsoft.EntityFrameworkCore;
using BeautyEcommerce.Infrastructure.Persistence;
using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Interfaces;

/// <summary>
/// Outbox repository implementation with distributed lock support using SKIP LOCKED
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OutboxRepository> _logger;

    public OutboxRepository(AppDbContext dbContext, ILogger<OutboxRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task AddMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _dbContext.OutboxMessages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(
        int limit, 
        Guid workerId, 
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        // Use raw SQL with SKIP LOCKED for efficient distributed processing
        var sql = @"
            SELECT * FROM outbox_messages
            WHERE processed_at IS NULL 
              AND is_dead_letter = false
              AND (lease_expires_at IS NULL OR lease_expires_at < @now)
            ORDER BY created_at ASC
            LIMIT @limit
            FOR UPDATE SKIP LOCKED
        ";

        var messages = await _dbContext.OutboxMessages
            .FromSqlRaw(sql, 
                new Npgsql.NpgsqlParameter("@limit", limit),
                new Npgsql.NpgsqlParameter("@now", now))
            .ToListAsync(cancellationToken);

        // Acquire lease for each message
        foreach (var message in messages)
        {
            message.WorkerId = workerId;
            message.LeaseExpiresAt = now.AddSeconds(30); // 30 second lease
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Acquired lease on {Count} outbox messages for worker {WorkerId}", 
            messages.Count, workerId);

        return messages;
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            message.WorkerId = null;
            message.LeaseExpiresAt = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Marked outbox message {MessageId} as processed", messageId);
        }
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.ErrorAt = DateTime.UtcNow;
            message.Error = error;
            message.RetryCount++;
            message.WorkerId = null;
            message.LeaseExpiresAt = null;

            // Move to dead letter after max retries (e.g., 5)
            if (message.RetryCount >= 5)
            {
                message.IsDeadLetter = true;
                _logger.LogWarning("Moved outbox message {MessageId} to dead letter after {RetryCount} retries", 
                    messageId, message.RetryCount);
            }
            else
            {
                _logger.LogWarning("Outbox message {MessageId} failed (attempt {RetryCount}): {Error}", 
                    messageId, message.RetryCount, error);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RenewLeaseAsync(Guid messageId, Guid workerId, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null && message.WorkerId == workerId)
        {
            message.LeaseExpiresAt = DateTime.UtcNow.Add(leaseDuration);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MoveToDeadLetterAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.IsDeadLetter = true;
            message.WorkerId = null;
            message.LeaseExpiresAt = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogWarning("Manually moved outbox message {MessageId} to dead letter", messageId);
        }
    }
}

/// <summary>
/// Outbox worker service that processes messages using Hangfire
/// </summary>
public interface IOutboxProcessor
{
    Task ProcessMessagesAsync(CancellationToken cancellationToken = default);
}

public class OutboxProcessor : IOutboxProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly Guid _workerId;

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _workerId = Guid.NewGuid();
        _logger.LogInformation("OutboxProcessor initialized with WorkerId: {WorkerId}", _workerId);
    }

    public async Task ProcessMessagesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var eventHandlers = scope.ServiceProvider.GetServices<IOutboxMessageHandler>();
        var handlerDictionary = eventHandlers.ToDictionary(h => h.MessageType);

        try
        {
            var messages = await repository.GetUnprocessedMessagesAsync(100, _workerId, cancellationToken);

            foreach (var message in messages)
            {
                try
                {
                    if (handlerDictionary.TryGetValue(message.Type, out var handler))
                    {
                        await handler.HandleAsync(message, cancellationToken);
                        await repository.MarkAsProcessedAsync(message.Id, cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning("No handler found for outbox message type: {Type}", message.Type);
                        await repository.MarkAsFailedAsync(message.Id, $"No handler for type: {message.Type}", cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);
                    await repository.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in outbox processor");
        }
    }
}

/// <summary>
/// Handler interface for outbox messages
/// </summary>
public interface IOutboxMessageHandler
{
    string MessageType { get; }
    Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken);
}
