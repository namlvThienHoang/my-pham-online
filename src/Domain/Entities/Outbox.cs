using BeautyCommerce.Domain.Entities;

namespace BeautyCommerce.Domain.Entities;

/// <summary>
/// OutboxMessage entity lưu trữ các domain events chờ được publish.
/// Pattern: Transactional Outbox với lease và distributed lock.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }
    public string? WorkerId { get; private set; }
    public DateTime? LeaseExpiresAt { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string eventType, string payload)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            OccurredAt = DateTime.UtcNow
        };
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
    }

    public bool AcquireLease(string workerId, TimeSpan leaseDuration)
    {
        var now = DateTime.UtcNow;
        if (ProcessedAt.HasValue)
            return false;

        if (LeaseExpiresAt.HasValue && LeaseExpiresAt.Value > now)
            return false; // Already leased by another worker

        WorkerId = workerId;
        LeaseExpiresAt = now + leaseDuration;
        return true;
    }

    public void RenewLease(TimeSpan leaseDuration)
    {
        LeaseExpiresAt = DateTime.UtcNow + leaseDuration;
    }
}

/// <summary>
/// Lưu trữ trạng thái hiện tại của Order Saga.
/// </summary>
public class OrderSagaState
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string CurrentState { get; private set; } = string.Empty;
    public string SagaData { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private OrderSagaState() { }

    public static OrderSagaState Create(Guid orderId, string initialState, string sagaData)
    {
        return new OrderSagaState
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CurrentState = initialState,
            SagaData = sagaData,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void TransitionTo(string newState, string? newData = null)
    {
        CurrentState = newState;
        UpdatedAt = DateTime.UtcNow;
        if (newData != null)
            SagaData = newData;
    }

    public void Complete()
    {
        CurrentState = "Completed";
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail()
    {
        CurrentState = "Failed";
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Log các compensation steps đã thực hiện để đảm bảo idempotency.
/// </summary>
public class SagaCompensationLog
{
    public Guid Id { get; private set; }
    public Guid SagaId { get; private set; }
    public string StepName { get; private set; } = string.Empty;
    public DateTime CompensatedAt { get; private set; }
    public string Result { get; private set; } = string.Empty;

    private SagaCompensationLog() { }

    public static SagaCompensationLog Create(Guid sagaId, string stepName, string result)
    {
        return new SagaCompensationLog
        {
            Id = Guid.NewGuid(),
            SagaId = sagaId,
            StepName = stepName,
            CompensatedAt = DateTime.UtcNow,
            Result = result
        };
    }
}
