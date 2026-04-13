namespace BeautyEcommerce.Domain.Interfaces;

using BeautyEcommerce.Domain.Entities;

/// <summary>
/// Repository interface for SkinProfile
/// </summary>
public interface ISkinProfileRepository : IRepository<SkinProfile>
{
    Task<SkinProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SkinProfile> CreateOrUpdateAsync(SkinProfile profile, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for QuizQuestion
/// </summary>
public interface IQuizQuestionRepository : IRepository<QuizQuestion>
{
    Task<IReadOnlyList<QuizQuestion>> GetActiveQuestionsAsync(string? category = null, CancellationToken cancellationToken = default);
    Task<QuizQuestion> GetWithAnswersAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for QuizResult
/// </summary>
public interface IQuizResultRepository : IRepository<QuizResult>
{
    Task<QuizResult?> GetByUserIdAsync(Guid userId, bool includeLogs = false, CancellationToken cancellationToken = default);
    Task<QuizResult> GetWithLogsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuizResult>> GetUserQuizHistoryAsync(Guid userId, int limit = 10, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for ProductRecommendation
/// </summary>
public interface IProductRecommendationRepository : IRepository<ProductRecommendation>
{
    Task<IReadOnlyList<ProductRecommendation>> GetBySkinProfileIdAsync(Guid skinProfileId, int limit = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductRecommendation>> GetRecommendedProductsAsync(Guid userId, string? category = null, int limit = 10, CancellationToken cancellationToken = default);
    Task UpdateRecommendationsAsync(Guid skinProfileId, IEnumerable<ProductRecommendation> recommendations, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for SkincareRoutine
/// </summary>
public interface ISkincareRoutineRepository : IRepository<SkincareRoutine>
{
    Task<IReadOnlyList<SkincareRoutine>> GetUserRoutinesAsync(Guid userId, bool includeSteps = false, CancellationToken cancellationToken = default);
    Task<SkincareRoutine> GetWithStepsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SkincareRoutine?> GetActiveRoutineByTypeAsync(Guid userId, string routineType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for RoutineLog
/// </summary>
public interface IRoutineLogRepository : IRepository<RoutineLog>
{
    Task<RoutineLog?> GetByDateAsync(Guid routineId, DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoutineLog>> GetRoutineHistoryAsync(Guid routineId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoutineLog>> GetUserRecentLogsAsync(Guid userId, int limit = 30, CancellationToken cancellationToken = default);
    Task<int> GetCompletionStreakAsync(Guid routineId, DateTime fromDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Reminder
/// </summary>
public interface IReminderRepository : IRepository<Reminder>
{
    Task<IReadOnlyList<Reminder>> GetStepRemindersAsync(Guid stepId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reminder>> GetDueRemindersAsync(DateTime currentTime, string notificationChannel, int batchSize = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reminder>> GetUserActiveRemindersAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for UserProduct
/// </summary>
public interface IUserProductRepository : IRepository<UserProduct>
{
    Task<IReadOnlyList<UserProduct>> GetUserProductsAsync(Guid userId, string? status = null, CancellationToken cancellationToken = default);
    Task<UserProduct?> GetByUserAndProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserProduct>> GetExpiringProductsAsync(int daysUntilExpiry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserProduct>> GetLowStockProductsAsync(int thresholdDays, CancellationToken cancellationToken = default);
    Task<UserProduct> AddFromOrderAsync(UserProduct userProduct, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for ExpiryAlert
/// </summary>
public interface IExpiryAlertRepository : IRepository<ExpiryAlert>
{
    Task<IReadOnlyList<ExpiryAlert>> GetUserAlertsAsync(Guid userId, string? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpiryAlert>> GetPendingAlertsAsync(string alertType, int limit = 100, CancellationToken cancellationToken = default);
    Task MarkAlertsAsSentAsync(IEnumerable<Guid> alertIds, CancellationToken cancellationToken = default);
    Task<int> GetUnreadAlertCountAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for ReplenishmentSubscription
/// </summary>
public interface IReplenishmentSubscriptionRepository : IRepository<ReplenishmentSubscription>
{
    Task<IReadOnlyList<ReplenishmentSubscription>> GetUserSubscriptionsAsync(Guid userId, string? status = null, CancellationToken cancellationToken = default);
    Task<ReplenishmentSubscription?> GetByUserAndProductAsync(Guid userId, Guid productId, string status = "Active", CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReplenishmentSubscription>> GetDueForDeliveryAsync(DateTime currentDate, int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReplenishmentSubscription>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for ReplenishmentOrder
/// </summary>
public interface IReplenishmentOrderRepository : IRepository<ReplenishmentOrder>
{
    Task<IReadOnlyList<ReplenishmentOrder>> GetBySubscriptionIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReplenishmentOrder>> GetByStatusAsync(string status, int limit = 100, CancellationToken cancellationToken = default);
    Task<ReplenishmentOrder?> GetByScheduledDateAsync(DateTime scheduledDate, Guid subscriptionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for UsageLog
/// </summary>
public interface IUsageLogRepository : IRepository<UsageLog>
{
    Task<IReadOnlyList<UsageLog>> GetUserProductUsageAsync(Guid userProductId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<UsageLog?> GetByDateAsync(Guid userProductId, DateTime date, CancellationToken cancellationToken = default);
    Task<int> GetTotalUsageQuantityAsync(Guid userProductId, CancellationToken cancellationToken = default);
}
