namespace BeautyEcommerce.Infrastructure.Repositories;

using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class SkincareRoutineRepository : Repository<SkincareRoutine>, ISkincareRoutineRepository
{
    public SkincareRoutineRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<SkincareRoutine>> GetUserRoutinesAsync(Guid userId, bool includeSteps = false, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(sr => sr.UserId == userId && sr.IsActive)
            .OrderBy(sr => sr.Name)
            .AsQueryable();

        if (includeSteps)
        {
            query = query.Include(sr => sr.Steps);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<SkincareRoutine> GetWithStepsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(sr => sr.Steps)
                .ThenInclude(s => s.Product)
            .Include(sr => sr.Steps)
                .ThenInclude(s => s.Reminders)
            .FirstOrDefaultAsync(sr => sr.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"SkincareRoutine with ID {id} not found");
    }

    public async Task<SkincareRoutine?> GetActiveRoutineByTypeAsync(Guid userId, string routineType, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(sr => sr.UserId == userId && sr.RoutineType == routineType && sr.IsActive, cancellationToken);
    }
}

public class RoutineLogRepository : Repository<RoutineLog>, IRoutineLogRepository
{
    public RoutineLogRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<RoutineLog?> GetByDateAsync(Guid routineId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(rl => rl.StepLogs)
            .FirstOrDefaultAsync(rl => rl.RoutineId == routineId && rl.LogDate == date.Date, cancellationToken);
    }

    public async Task<IReadOnlyList<RoutineLog>> GetRoutineHistoryAsync(Guid routineId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(rl => rl.RoutineId == routineId && rl.LogDate >= startDate && rl.LogDate <= endDate)
            .OrderByDescending(rl => rl.LogDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoutineLog>> GetUserRecentLogsAsync(Guid userId, int limit = 30, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(rl => rl.UserId == userId)
            .OrderByDescending(rl => rl.LogDate)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCompletionStreakAsync(Guid routineId, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        var logs = await DbSet
            .Where(rl => rl.RoutineId == routineId && rl.IsCompleted && rl.LogDate <= fromDate)
            .OrderByDescending(rl => rl.LogDate)
            .ToListAsync(cancellationToken);

        if (logs.Count == 0) return 0;

        int streak = 1;
        var currentDate = logs[0].LogDate;

        for (int i = 1; i < logs.Count; i++)
        {
            var expectedDate = currentDate.AddDays(-1);
            if (logs[i].LogDate == expectedDate)
            {
                streak++;
                currentDate = expectedDate;
            }
            else
            {
                break;
            }
        }

        return streak;
    }
}

public class ReminderRepository : Repository<Reminder>, IReminderRepository
{
    public ReminderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Reminder>> GetStepRemindersAsync(Guid stepId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.StepId == stepId && r.IsActive)
            .OrderBy(r => r.Time)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reminder>> GetDueRemindersAsync(DateTime currentTime, string notificationChannel, int batchSize = 100, CancellationToken cancellationToken = default)
    {
        var dayOfWeek = ((int)currentTime.DayOfWeek).ToString(); // 0=Sunday, 1=Monday, etc.
        // Adjust to match our format (1=Monday)
        var dayNumber = currentTime.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)currentTime.DayOfWeek;

        return await DbSet
            .Include(r => r.Step)
                .ThenInclude(s => s.Routine)
            .Where(r => r.IsActive && 
                       r.NotificationChannel == notificationChannel &&
                       r.DaysOfWeek.Contains(dayNumber.ToString()) &&
                       r.Time.Hours == currentTime.Hour &&
                       r.Time.Minutes == currentTime.Minute)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reminder>> GetUserActiveRemindersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.Step)
                .ThenInclude(s => s.Routine)
            .Where(r => r.IsActive && r.Step!.Routine!.UserId == userId)
            .OrderBy(r => r.Time)
            .ToListAsync(cancellationToken);
    }
}
