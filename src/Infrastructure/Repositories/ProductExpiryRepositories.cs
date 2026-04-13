namespace BeautyEcommerce.Infrastructure.Repositories;

using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class UserProductRepository : Repository<UserProduct>, IUserProductRepository
{
    public UserProductRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<UserProduct>> GetUserProductsAsync(Guid userId, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(up => up.Product)
            .Where(up => up.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(up => up.Status == status);
        }

        return await query.OrderByDescending(up => up.PurchasedDate).ToListAsync(cancellationToken);
    }

    public async Task<UserProduct?> GetByUserAndProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(up => up.UserId == userId && up.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<UserProduct>> GetExpiringProductsAsync(int daysUntilExpiry, CancellationToken cancellationToken = default)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(daysUntilExpiry);

        return await DbSet
            .Include(up => up.User)
            .Include(up => up.Product)
            .Where(up => up.ExpiryDate.HasValue && 
                        up.ExpiryDate.Value <= thresholdDate && 
                        up.ExpiryDate.Value >= DateTime.UtcNow &&
                        up.Status == "InUse" &&
                        up.IsExpiryAlertEnabled)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserProduct>> GetLowStockProductsAsync(int thresholdDays, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(up => up.User)
            .Include(up => up.Product)
            .Where(up => up.Status == "InUse" &&
                        up.EstimatedDailyUsage > 0 &&
                        (up.CurrentQuantity / up.EstimatedDailyUsage) <= thresholdDays)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserProduct> AddFromOrderAsync(UserProduct userProduct, CancellationToken cancellationToken = default)
    {
        await AddAsync(userProduct, cancellationToken);
        return userProduct;
    }
}

public class ExpiryAlertRepository : Repository<ExpiryAlert>, IExpiryAlertRepository
{
    public ExpiryAlertRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ExpiryAlert>> GetUserAlertsAsync(Guid userId, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(ea => ea.UserProduct)
            .Where(ea => ea.UserId == userId)
            .OrderByDescending(ea => eа.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(ea => ea.Status == status);
        }

        return await query.Take(100).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExpiryAlert>> GetPendingAlertsAsync(string alertType, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(ea => ea.Status == "Pending" && ea.AlertType == alertType)
            .OrderBy(ea => ea.TriggerDate)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAlertsAsSentAsync(IEnumerable<Guid> alertIds, CancellationToken cancellationToken = default)
    {
        var alerts = await DbSet
            .Where(ea => alertIds.Contains(ea.Id))
            .ToListAsync(cancellationToken);

        foreach (var alert in alerts)
        {
            alert.MarkAsSent();
        }
    }

    public async Task<int> GetUnreadAlertCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(ea => ea.UserId == userId && ea.Status == "Sent", cancellationToken);
    }
}

public class ReplenishmentSubscriptionRepository : Repository<ReplenishmentSubscription>, IReplenishmentSubscriptionRepository
{
    public ReplenishmentSubscriptionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ReplenishmentSubscription>> GetUserSubscriptionsAsync(Guid userId, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(rs => rs.Product)
            .Include(rs => rs.UserProduct)
            .Where(rs => rs.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(rs => rs.Status == status);
        }

        return await query.OrderByDescending(rs => rs.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<ReplenishmentSubscription?> GetByUserAndProductAsync(Guid userId, Guid productId, string status = "Active", CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(rs => rs.UserId == userId && 
                                      rs.ProductId == productId && 
                                      rs.Status == status, cancellationToken);
    }

    public async Task<IReadOnlyList<ReplenishmentSubscription>> GetDueForDeliveryAsync(DateTime currentDate, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(rs => rs.User)
            .Include(rs => rs.Product)
            .Where(rs => rs.Status == "Active" && 
                        rs.NextDeliveryDate <= currentDate)
            .OrderBy(rs => rs.NextDeliveryDate)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReplenishmentSubscription>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(rs => rs.Status == "Active")
            .ToListAsync(cancellationToken);
    }
}

public class ReplenishmentOrderRepository : Repository<ReplenishmentOrder>, IReplenishmentOrderRepository
{
    public ReplenishmentOrderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ReplenishmentOrder>> GetBySubscriptionIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(ro => ro.SubscriptionId == subscriptionId)
            .OrderByDescending(ro => ro.ScheduledDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReplenishmentOrder>> GetByStatusAsync(string status, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(ro => ro.Product)
            .Include(ro => ro.User)
            .Where(ro => ro.Status == status)
            .OrderBy(ro => ro.ScheduledDate)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReplenishmentOrder?> GetByScheduledDateAsync(DateTime scheduledDate, Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(ro => ro.SubscriptionId == subscriptionId && 
                                      ro.ScheduledDate.Date == scheduledDate.Date, cancellationToken);
    }
}

public class UsageLogRepository : Repository<UsageLog>, IUsageLogRepository
{
    public UsageLogRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<UsageLog>> GetUserProductUsageAsync(Guid userProductId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(ul => ul.UserProductId == userProductId && 
                        ul.UsageDate >= startDate && 
                        ul.UsageDate <= endDate)
            .OrderBy(ul => ul.UsageDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<UsageLog?> GetByDateAsync(Guid userProductId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(ul => ul.UserProductId == userProductId && 
                                      ul.UsageDate == date.Date, cancellationToken);
    }

    public async Task<int> GetTotalUsageQuantityAsync(Guid userProductId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(ul => ul.UserProductId == userProductId)
            .SumAsync(ul => ul.QuantityUsed, cancellationToken);
    }
}
