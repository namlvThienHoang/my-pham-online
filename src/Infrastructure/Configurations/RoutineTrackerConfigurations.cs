namespace BeautyEcommerce.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BeautyEcommerce.Domain.Entities;

/// <summary>
/// Configuration for SkincareRoutine entity
/// </summary>
public class SkincareRoutineConfiguration : IEntityTypeConfiguration<SkincareRoutine>
{
    public void Configure(EntityTypeBuilder<SkincareRoutine> builder)
    {
        builder.ToTable("skincare_routines");

        builder.HasKey(sr => sr.Id);

        builder.Property(sr => sr.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(sr => sr.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(sr => sr.RoutineType)
            .HasColumnName("routine_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(sr => sr.Description)
            .HasColumnName("description");

        builder.Property(sr => sr.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(sr => sr.StreakDays)
            .HasColumnName("streak_days")
            .HasDefaultValue(0);

        builder.Property(sr => sr.TotalCompletedDays)
            .HasColumnName("total_completed_days")
            .HasDefaultValue(0);

        builder.Property(sr => sr.LastCompletedDate)
            .HasColumnName("last_completed_date");

        builder.HasIndex(sr => sr.UserId)
            .HasDatabaseName("ix_skincare_routines_user_id");

        builder.HasOne(sr => sr.User)
            .WithMany()
            .HasForeignKey(sr => sr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sr => sr.Steps)
            .WithOne(s => s.Routine)
            .HasForeignKey(s => s.RoutineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sr => sr.RoutineLogs)
            .WithOne(rl => rl.Routine)
            .HasForeignKey(rl => rl.RoutineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for RoutineStep entity
/// </summary>
public class RoutineStepConfiguration : IEntityTypeConfiguration<RoutineStep>
{
    public void Configure(EntityTypeBuilder<RoutineStep> builder)
    {
        builder.ToTable("routine_steps");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.RoutineId)
            .HasColumnName("routine_id")
            .IsRequired();

        builder.Property(s => s.ProductId)
            .HasColumnName("product_id");

        builder.Property(s => s.StepName)
            .HasColumnName("step_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(s => s.Instructions)
            .HasColumnName("instructions");

        builder.Property(s => s.EstimatedDurationMinutes)
            .HasColumnName("estimated_duration_minutes")
            .HasDefaultValue(1);

        builder.Property(s => s.TimeOfDay)
            .HasColumnName("time_of_day")
            .HasMaxLength(50);

        builder.Property(s => s.IsRequired)
            .HasColumnName("is_required")
            .HasDefaultValue(true);

        builder.Property(s => s.Notes)
            .HasColumnName("notes");

        builder.HasIndex(s => s.RoutineId)
            .HasDatabaseName("ix_routine_steps_routine_id");

        builder.HasIndex(s => new { s.RoutineId, s.Order })
            .HasDatabaseName("ix_routine_steps_order");

        builder.HasOne(s => s.Routine)
            .WithMany(r => r.Steps)
            .HasForeignKey(s => s.RoutineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(s => s.Reminders)
            .WithOne(r => r.Step)
            .HasForeignKey(r => r.StepId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for RoutineLog entity
/// </summary>
public class RoutineLogConfiguration : IEntityTypeConfiguration<RoutineLog>
{
    public void Configure(EntityTypeBuilder<RoutineLog> builder)
    {
        builder.ToTable("routine_logs");

        builder.HasKey(rl => rl.Id);

        builder.Property(rl => rl.RoutineId)
            .HasColumnName("routine_id")
            .IsRequired();

        builder.Property(rl => rl.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(rl => rl.LogDate)
            .HasColumnName("log_date")
            .IsRequired();

        builder.Property(rl => rl.IsCompleted)
            .HasColumnName("is_completed")
            .HasDefaultValue(false);

        builder.Property(rl => rl.StartedAt)
            .HasColumnName("started_at");

        builder.Property(rl => rl.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(rl => rl.CompletionPercentage)
            .HasColumnName("completion_percentage")
            .HasDefaultValue(0);

        builder.Property(rl => rl.Notes)
            .HasColumnName("notes");

        builder.Property(rl => rl.MoodRating)
            .HasColumnName("mood_rating");

        builder.Property(rl => rl.SkinConditionNotes)
            .HasColumnName("skin_condition_notes");

        builder.HasIndex(rl => rl.RoutineId)
            .HasDatabaseName("ix_routine_logs_routine_id");

        builder.HasIndex(rl => new { rl.RoutineId, rl.LogDate })
            .HasDatabaseName("ix_routine_logs_date")
            .IsUnique();

        builder.HasOne(rl => rl.Routine)
            .WithMany(r => r.RoutineLogs)
            .HasForeignKey(rl => rl.RoutineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rl => rl.User)
            .WithMany()
            .HasForeignKey(rl => rl.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(rl => rl.StepLogs)
            .WithOne(sl => sl.RoutineLog)
            .HasForeignKey(sl => sl.RoutineLogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for StepLog entity
/// </summary>
public class StepLogConfiguration : IEntityTypeConfiguration<StepLog>
{
    public void Configure(EntityTypeBuilder<StepLog> builder)
    {
        builder.ToTable("step_logs");

        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.RoutineLogId)
            .HasColumnName("routine_log_id")
            .IsRequired();

        builder.Property(sl => sl.StepId)
            .HasColumnName("step_id")
            .IsRequired();

        builder.Property(sl => sl.IsCompleted)
            .HasColumnName("is_completed")
            .HasDefaultValue(false);

        builder.Property(sl => sl.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(sl => sl.ActualDurationMinutes)
            .HasColumnName("actual_duration_minutes")
            .HasDefaultValue(0);

        builder.Property(sl => sl.Notes)
            .HasColumnName("notes");

        builder.Property(sl => sl.ProductAmountUsed)
            .HasColumnName("product_amount_used");

        builder.HasIndex(sl => sl.RoutineLogId)
            .HasDatabaseName("ix_step_logs_routine_log_id");

        builder.HasIndex(sl => new { sl.RoutineLogId, sl.StepId })
            .HasDatabaseName("ix_step_logs_unique")
            .IsUnique();

        builder.HasOne(sl => sl.RoutineLog)
            .WithMany(rl => rl.StepLogs)
            .HasForeignKey(sl => sl.RoutineLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sl => sl.Step)
            .WithMany()
            .HasForeignKey(sl => sl.StepId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for Reminder entity
/// </summary>
public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("reminders");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.StepId)
            .HasColumnName("step_id")
            .IsRequired();

        builder.Property(r => r.Time)
            .HasColumnName("time")
            .IsRequired();

        builder.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(r => r.DaysOfWeek)
            .HasColumnName("days_of_week")
            .HasMaxLength(50)
            .HasDefaultValue("1,2,3,4,5,6,7");

        builder.Property(r => r.ReminderText)
            .HasColumnName("reminder_text");

        builder.Property(r => r.NotificationChannel)
            .HasColumnName("notification_channel")
            .HasMaxLength(50)
            .HasDefaultValue("Push");

        builder.Property(r => r.LastSentAt)
            .HasColumnName("last_sent_at");

        builder.Property(r => r.SentCount)
            .HasColumnName("sent_count")
            .HasDefaultValue(0);

        builder.HasIndex(r => r.StepId)
            .HasDatabaseName("ix_reminders_step_id");

        builder.HasIndex(r => new { r.IsActive, r.NotificationChannel, r.Time })
            .HasDatabaseName("ix_reminders_due");

        builder.HasOne(r => r.Step)
            .WithMany(s => s.Reminders)
            .HasForeignKey(r => r.StepId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
