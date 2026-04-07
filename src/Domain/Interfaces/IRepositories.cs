namespace BeautyCommerce.Domain.Interfaces;

using BeautyCommerce.Domain.Common;

/// <summary>
/// Generic repository interface
/// </summary>
public interface IRepository<T> where T : BaseEntity, IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Unit of Work pattern for transaction management
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Outbox repository interface
/// </summary>
public interface IOutboxRepository
{
    Task AddMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(int limit, Guid workerId, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
    Task RenewLeaseAsync(Guid messageId, Guid workerId, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task MoveToDeadLetterAsync(Guid messageId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Saga repository interface
/// </summary>
public interface ISagaRepository
{
    Task<OrderSagaState?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderSagaState> CreateAsync(OrderSagaState state, CancellationToken cancellationToken = default);
    Task UpdateAsync(OrderSagaState state, CancellationToken cancellationToken = default);
    Task AddCompensationLogAsync(SagaCompensationLog log, CancellationToken cancellationToken = default);
    Task<bool> HasCompensatedAsync(Guid sagaStateId, string stepName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Inventory repository interface with FEFO support
/// </summary>
public interface IInventoryRepository
{
    Task<InventoryLot?> GetAvailableLotAsync(Guid productId, Guid? variantId, int quantity, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryLot>> GetAvailableLotsAsync(Guid productId, Guid? variantId, int quantity, CancellationToken cancellationToken = default);
    Task ReserveInventoryAsync(Guid lotId, int quantity, Guid orderId, CancellationToken cancellationToken = default);
    Task ReleaseReservationAsync(Guid lotId, int quantity, Guid orderId, CancellationToken cancellationToken = default);
    Task CommitInventoryAsync(Guid lotId, int quantity, Guid orderId, CancellationToken cancellationToken = default);
    Task<int> GetAvailableStockAsync(Guid productId, Guid? variantId, CancellationToken cancellationToken = default);
    Task AddStockAsync(InventoryLot lot, CancellationToken cancellationToken = default);
}

/// <summary>
/// Product repository interface with Elasticsearch sync
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Items, string NextCursor)> GetPagedAsync(int pageSize, string? cursor, CancellationToken cancellationToken = default);
    Task SyncToElasticsearchAsync(Product product, CancellationToken cancellationToken = default);
    Task RemoveFromElasticsearchAsync(Guid productId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Order repository interface
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Order> Items, string NextCursor)> GetUserOrdersAsync(Guid userId, int pageSize, string? cursor, CancellationToken cancellationToken = default);
    Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}

/// <summary>
/// User repository interface
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache service interface with tag-based invalidation
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
    Task RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event bus interface for domain events
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : DomainEvent;
}
