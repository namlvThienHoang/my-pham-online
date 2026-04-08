using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.Shared.Interfaces;

public interface IOutboxService
{
    Task SaveEventAsync(string aggregateType, Guid aggregateId, string eventType, object payload, CancellationToken ct = default);
    Task ProcessPendingEventsAsync(CancellationToken ct = default);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByTagAsync(string tag, CancellationToken ct = default);
}

public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);
    Task SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default);
    Task QueueNotificationAsync(Guid? userId, string channel, string recipient, string type, string subject, string content, CancellationToken ct = default);
}

public interface IIdempotencyService
{
    Task<bool> TryAcquireLockAsync(string key, TimeSpan expiration, CancellationToken ct = default);
    Task ReleaseLockAsync(string key, CancellationToken ct = default);
    Task<T?> GetCachedResultAsync<T>(string key, CancellationToken ct = default);
    Task CacheResultAsync<T>(string key, T result, TimeSpan expiration, CancellationToken ct = default);
}
