namespace BeautyEcommerce.Domain.Entities;

using BeautyEcommerce.Domain.Common;

/// <summary>
/// Routine tracker for daily skincare routine management
/// Helps users track their product usage and build habits (increases retention)
/// </summary>
public class SkincareRoutine : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty; // Morning Routine, Night Routine
    public string RoutineType { get; private set; } = string.Empty; // Morning, Evening, Both
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int StreakDays { get; private set; } // Current streak of completed days
    public int TotalCompletedDays { get; private set; }
    public DateTime? LastCompletedDate { get; private set; }
    
    // Navigation properties
    public virtual User? User { get; private set; }
    public virtual ICollection<RoutineStep> Steps { get; private set; } = new List<RoutineStep>();
    public virtual ICollection<RoutineLog> RoutineLogs { get; private set; } = new List<RoutineLog>();
    
    private SkincareRoutine() { }
    
    public static SkincareRoutine Create(
        Guid userId,
        string name,
        string routineType,
        string? description = null)
    {
        return new SkincareRoutine
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            RoutineType = routineType,
            Description = description,
            IsActive = true,
            StreakDays = 0,
            TotalCompletedDays = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void CompleteRoutine(DateTime completionDate)
    {
        if (LastCompletedDate.HasValue && LastCompletedDate.Value.Date == completionDate.Date)
        {
            return; // Already completed today
        }
        
        TotalCompletedDays++;
        
        if (LastCompletedDate.HasValue && 
            completionDate.Date == LastCompletedDate.Value.Date.AddDays(1))
        {
            StreakDays++;
        }
        else if (!LastCompletedDate.HasValue || 
                 completionDate.Date > LastCompletedDate.Value.Date.AddDays(1))
        {
            StreakDays = 1; // Reset streak if missed days
        }
        
        LastCompletedDate = completionDate;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Individual step in a skincare routine
/// </summary>
public class RoutineStep : BaseEntity
{
    public Guid RoutineId { get; private set; }
    public Guid? ProductId { get; private set; } // Optional - can be custom step
    public string StepName { get; private set; } = string.Empty; // Cleanser, Toner, Serum, etc.
    public int Order { get; private set; }
    public string? Instructions { get; private set; }
    public int EstimatedDurationMinutes { get; private set; } // How long to apply/wait
    public string? TimeOfDay { get; private set; } // Morning, Evening, Both
    public bool IsRequired { get; private set; } = true;
    public string? Notes { get; private set; } // Additional tips
    
    // Navigation properties
    public virtual SkincareRoutine? Routine { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ICollection<Reminder> Reminders { get; private set; } = new List<Reminder>();
    
    private RoutineStep() { }
    
    public static RoutineStep Create(
        Guid routineId,
        string stepName,
        int order,
        Guid? productId = null,
        string? instructions = null,
        int estimatedDurationMinutes = 1,
        string? timeOfDay = null,
        bool isRequired = true)
    {
        return new RoutineStep
        {
            Id = Guid.NewGuid(),
            RoutineId = routineId,
            ProductId = productId,
            StepName = stepName,
            Order = order,
            Instructions = instructions,
            EstimatedDurationMinutes = estimatedDurationMinutes,
            TimeOfDay = timeOfDay,
            IsRequired = isRequired,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void UpdateOrder(int newOrder)
    {
        Order = newOrder;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Daily log of routine completion
/// </summary>
public class RoutineLog : BaseEntity
{
    public Guid RoutineId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime LogDate { get; private set; } // Date of the routine
    public bool IsCompleted { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int CompletionPercentage { get; private set; }
    public string? Notes { get; private set; }
    public int MoodRating { get; private set; } // 1-5 how skin feels
    public string? SkinConditionNotes { get; private set; }
    
    // Navigation properties
    public virtual SkincareRoutine? Routine { get; private set; }
    public virtual User? User { get; private set; }
    public virtual ICollection<StepLog> StepLogs { get; private set; } = new List<StepLog>();
    
    private RoutineLog() { }
    
    public static RoutineLog Create(
        Guid routineId,
        Guid userId,
        DateTime logDate)
    {
        return new RoutineLog
        {
            Id = Guid.NewGuid(),
            RoutineId = routineId,
            UserId = userId,
            LogDate = logDate.Date,
            IsCompleted = false,
            StartedAt = DateTime.UtcNow,
            CompletionPercentage = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void Complete(int completionPercentage = 100)
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        CompletionPercentage = completionPercentage;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Log of individual step completion
/// </summary>
public class StepLog : BaseEntity
{
    public Guid RoutineLogId { get; private set; }
    public Guid StepId { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int ActualDurationMinutes { get; private set; }
    public string? Notes { get; private set; }
    public string? ProductAmountUsed { get; private set; } // e.g., "2 pumps", "3 drops"
    
    // Navigation properties
    public virtual RoutineLog? RoutineLog { get; private set; }
    public virtual RoutineStep? Step { get; private set; }
    
    private StepLog() { }
    
    public static StepLog Create(Guid routineLogId, Guid stepId)
    {
        return new StepLog
        {
            Id = Guid.NewGuid(),
            RoutineLogId = routineLogId,
            StepId = stepId,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void Complete(int actualDurationMinutes = 0, string? productAmountUsed = null)
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        ActualDurationMinutes = actualDurationMinutes;
        ProductAmountUsed = productAmountUsed;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Reminder settings for routine steps
/// </summary>
public class Reminder : BaseEntity
{
    public Guid StepId { get; private set; }
    public TimeSpan Time { get; private set; } // Time of day for reminder
    public bool IsActive { get; private set; } = true;
    public string DaysOfWeek { get; private set; } = "1,2,3,4,5,6,7"; // Comma-separated (1=Monday)
    public string? ReminderText { get; private set; }
    public string NotificationChannel { get; private set; } = "Push"; // Push, Email, SMS
    public DateTime? LastSentAt { get; private set; }
    public int SentCount { get; private set; }
    
    // Navigation properties
    public virtual RoutineStep? Step { get; private set; }
    
    private Reminder() { }
    
    public static Reminder Create(
        Guid stepId,
        TimeSpan time,
        string daysOfWeek = "1,2,3,4,5,6,7",
        string? reminderText = null,
        string notificationChannel = "Push")
    {
        return new Reminder
        {
            Id = Guid.NewGuid(),
            StepId = stepId,
            Time = time,
            DaysOfWeek = daysOfWeek,
            ReminderText = reminderText,
            NotificationChannel = notificationChannel,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void RecordSent()
    {
        LastSentAt = DateTime.UtcNow;
        SentCount++;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Toggle()
    {
        IsActive = !IsActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
