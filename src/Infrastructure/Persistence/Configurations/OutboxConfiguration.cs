using BeautyCommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeautyCommerce.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình EF Core cho bảng OutboxMessages.
/// Áp dụng pattern: Outbox với lease, SKIP LOCKED, retry, dead letter.
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(x => x.Error)
            .HasColumnName("error")
            .HasMaxLength(4000);

        builder.Property(x => x.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        builder.Property(x => x.WorkerId)
            .HasColumnName("worker_id")
            .HasMaxLength(64);

        builder.Property(x => x.LeaseExpiresAt)
            .HasColumnName("lease_expires_at");

        builder.HasIndex(x => x.ProcessedAt)
            .HasFilter("processed_at IS NULL");

        builder.HasIndex(x => x.LeaseExpiresAt)
            .HasFilter("processed_at IS NULL");
    }
}

/// <summary>
/// Cấu hình EF Core cho bảng OrderSagaState.
/// Lưu trữ trạng thái của Saga orchestration cho order.
/// </summary>
public class OrderSagaStateConfiguration : IEntityTypeConfiguration<OrderSagaState>
{
    public void Configure(EntityTypeBuilder<OrderSagaState> builder)
    {
        builder.ToTable("order_saga_state");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(x => x.CurrentState)
            .HasColumnName("current_state")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.SagaData)
            .HasColumnName("saga_data")
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at");

        builder.HasIndex(x => x.OrderId)
            .IsUnique();

        builder.HasIndex(x => x.CurrentState);
    }
}

/// <summary>
/// Cấu hình EF Core cho bảng SagaCompensationLog.
/// Lưu log các compensation transaction để đảm bảo idempotency.
/// </summary>
public class SagaCompensationLogConfiguration : IEntityTypeConfiguration<SagaCompensationLog>
{
    public void Configure(EntityTypeBuilder<SagaCompensationLog> builder)
    {
        builder.ToTable("saga_compensation_log");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.SagaId)
            .HasColumnName("saga_id")
            .IsRequired();

        builder.Property(x => x.StepName)
            .HasColumnName("step_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.CompensatedAt)
            .HasColumnName("compensated_at")
            .IsRequired();

        builder.Property(x => x.Result)
            .HasColumnName("result")
            .HasColumnType("jsonb");

        builder.HasIndex(x => new { x.SagaId, x.StepName })
            .IsUnique();
    }
}
