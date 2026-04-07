using Xunit;
using FluentAssertions;
using BeautyCommerce.Domain.Entities;

namespace BeautyCommerce.Unit.Tests;

/// <summary>
/// Unit tests for OutboxMessage entity
/// </summary>
public class OutboxMessageTests
{
    [Fact]
    public void Create_ShouldInitializeWithCorrectValues()
    {
        // Arrange
        var eventType = "OrderCreated";
        var payload = "{\"orderId\":\"123\"}";

        // Act
        var message = OutboxMessage.Create(eventType, payload);

        // Assert
        message.Id.Should().NotBeEmpty();
        message.EventType.Should().Be(eventType);
        message.Payload.Should().Be(payload);
        message.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        message.ProcessedAt.Should().BeNull();
        message.RetryCount.Should().Be(0);
        message.WorkerId.Should().BeNull();
        message.LeaseExpiresAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsProcessed_ShouldSetProcessedAt()
    {
        // Arrange
        var message = OutboxMessage.Create("TestEvent", "{}");
        var beforeProcess = DateTime.UtcNow;

        // Act
        message.MarkAsProcessed();
        var afterProcess = DateTime.UtcNow;

        // Assert
        message.ProcessedAt.Should().HaveValue();
        message.ProcessedAt.Value.Should().BeOnOrAfter(beforeProcess);
        message.ProcessedAt.Value.Should().BeOnOrBefore(afterProcess);
    }

    [Fact]
    public void MarkAsFailed_ShouldIncrementRetryCountAndSetError()
    {
        // Arrange
        var message = OutboxMessage.Create("TestEvent", "{}");
        var errorMessage = "Test error message";

        // Act
        message.MarkAsFailed(errorMessage);

        // Assert
        message.RetryCount.Should().Be(1);
        message.Error.Should().Be(errorMessage);
    }

    [Fact]
    public void MarkAsFailed_MultipleTimes_ShouldIncrementRetryCountEachTime()
    {
        // Arrange
        var message = OutboxMessage.Create("TestEvent", "{}");

        // Act
        message.MarkAsFailed("Error 1");
        message.MarkAsFailed("Error 2");
        message.MarkAsFailed("Error 3");

        // Assert
        message.RetryCount.Should().Be(3);
        message.Error.Should().Be("Error 3");
    }

    [Fact]
    public void AcquireLease_WhenAvailable_ShouldSucceed()
    {
        // Arrange
        var message = OutboxMessage.Create("TestEvent", "{}");
        var workerId = "worker-123";
        var leaseDuration = TimeSpan.FromSeconds(30);

        // Act
        var result = message.AcquireLease(workerId, leaseDuration);

        // Assert
        result.Should().BeTrue();
        message.WorkerId.Should().Be(workerId);
        message.LeaseExpiresAt.Should().HaveValue();
    }

    [Fact]
    public void AcquireLease_WhenAlreadyProcessed_ShouldFail()
    {
        // Arrange
        var message = OutboxMessage.Create("TestEvent", "{}");
        message.MarkAsProcessed();
        var workerId = "worker-123";

        // Act
        var result = message.AcquireLease(workerId, TimeSpan.FromSeconds(30));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AcquireLease_WhenAlreadyLeased_ShouldFail()
    {
        // Arrange
        var message = OutboxMessage.Create("TestEvent", "{}");
        message.AcquireLease("worker-1", TimeSpan.FromSeconds(30));
        
        // Act - try to acquire with different worker
        var result = message.AcquireLease("worker-2", TimeSpan.FromSeconds(30));

        // Assert
        result.Should().BeFalse();
        message.WorkerId.Should().Be("worker-1");
    }

    [Fact]
    public void AcquireLease_WhenLeaseExpired_ShouldSucceed()
    {
        // Arrange
        var message = OutboxMessage.Create("TestEvent", "{}");
        message.AcquireLease("worker-1", TimeSpan.FromMilliseconds(-1)); // Expired lease
        
        // Act - try to acquire with different worker
        var result = message.AcquireLease("worker-2", TimeSpan.FromSeconds(30));

        // Assert
        result.Should().BeTrue();
        message.WorkerId.Should().Be("worker-2");
    }

    [Fact]
    public void RenewLease_ShouldExtendLeaseExpiration()
    {
        // Arrange
        var message = OutboxMessage.Create("TestEvent", "{}");
        message.AcquireLease("worker-1", TimeSpan.FromSeconds(10));
        var firstExpiry = message.LeaseExpiresAt!.Value;

        // Act
        message.RenewLease(TimeSpan.FromSeconds(60));

        // Assert
        message.LeaseExpiresAt.Should().BeAfter(firstExpiry);
    }
}
