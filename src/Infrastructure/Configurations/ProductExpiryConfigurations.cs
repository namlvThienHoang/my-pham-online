namespace BeautyEcommerce.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BeautyEcommerce.Domain.Entities;

/// <summary>
/// Configuration for UserProduct entity
/// </summary>
public class UserProductConfiguration : IEntityTypeConfiguration<UserProduct>
{
    public void Configure(EntityTypeBuilder<UserProduct> builder)
    {
        builder.ToTable("user_products");

        builder.HasKey(up => up.Id);

        builder.Property(up => up.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(up => up.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(up => up.OrderItemId)
            .HasColumnName("order_item_id");

        builder.Property(up => up.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasDefaultValue("InUse");

        builder.Property(up => up.PurchasedDate)
            .HasColumnName("purchased_date")
            .IsRequired();

        builder.Property(up => up.OpenedDate)
            .HasColumnName("opened_date")
            .IsRequired();

        builder.Property(up => up.ShelfLifeMonths)
            .HasColumnName("shelf_life_months")
            .IsRequired();

        builder.Property(up => up.PaO_Months)
            .HasColumnName("pao_months")
            .IsRequired();

        builder.Property(up => up.ExpiryDate)
            .HasColumnName("expiry_date");

        builder.Property(up => up.FinishedDate)
            .HasColumnName("finished_date");

        builder.Property(up => up.InitialQuantity)
            .HasColumnName("initial_quantity")
            .IsRequired();

        builder.Property(up => up.CurrentQuantity)
            .HasColumnName("current_quantity")
            .IsRequired();

        builder.Property(up => up.QuantityUnit)
            .HasColumnName("quantity_unit")
            .HasMaxLength(20);

        builder.Property(up => up.EstimatedDailyUsage)
            .HasColumnName("estimated_daily_usage")
            .HasDefaultValue(0);

        builder.Property(up => up.LastUsedDate)
            .HasColumnName("last_used_date");

        builder.Property(up => up.UsageCount)
            .HasColumnName("usage_count")
            .HasDefaultValue(0);

        builder.Property(up => up.Notes)
            .HasColumnName("notes");

        builder.Property(up => up.IsExpiryAlertEnabled)
            .HasColumnName("is_expiry_alert_enabled")
            .HasDefaultValue(true);

        builder.Property(up => up.IsReplenishEnabled)
            .HasColumnName("is_replenish_enabled")
            .HasDefaultValue(false);

        builder.Property(up => up.ReplenishThresholdDays)
            .HasColumnName("replenish_threshold_days")
            .HasDefaultValue(7);

        builder.HasIndex(up => up.UserId)
            .HasDatabaseName("ix_user_products_user_id");

        builder.HasIndex(up => new { up.UserId, up.Status })
            .HasDatabaseName("ix_user_products_status");

        builder.HasIndex(up => up.ExpiryDate)
            .HasDatabaseName("ix_user_products_expiry");

        builder.HasOne(up => up.User)
            .WithMany()
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.Product)
            .WithMany()
            .HasForeignKey(up => up.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.OrderItem)
            .WithMany()
            .HasForeignKey(up => up.OrderItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(up => up.UsageLogs)
            .WithOne(ul => ul.UserProduct)
            .HasForeignKey(ul => ul.UserProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(up => up.ExpiryAlerts)
            .WithOne(ea => ea.UserProduct)
            .HasForeignKey(ea => ea.UserProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for UsageLog entity
/// </summary>
public class UsageLogConfiguration : IEntityTypeConfiguration<UsageLog>
{
    public void Configure(EntityTypeBuilder<UsageLog> builder)
    {
        builder.ToTable("usage_logs");

        builder.HasKey(ul => ul.Id);

        builder.Property(ul => ul.UserProductId)
            .HasColumnName("user_product_id")
            .IsRequired();

        builder.Property(ul => ul.UsageDate)
            .HasColumnName("usage_date")
            .IsRequired();

        builder.Property(ul => ul.QuantityUsed)
            .HasColumnName("quantity_used")
            .IsRequired();

        builder.Property(ul => ul.ApplicationArea)
            .HasColumnName("application_area")
            .HasMaxLength(100);

        builder.Property(ul => ul.TimeOfDay)
            .HasColumnName("time_of_day")
            .HasMaxLength(50);

        builder.Property(ul => ul.Notes)
            .HasColumnName("notes");

        builder.Property(ul => ul.SkinReactionRating)
            .HasColumnName("skin_reaction_rating");

        builder.HasIndex(ul => ul.UserProductId)
            .HasDatabaseName("ix_usage_logs_user_product_id");

        builder.HasIndex(ul => new { ul.UserProductId, ul.UsageDate })
            .HasDatabaseName("ix_usage_logs_date");

        builder.HasOne(ul => ul.UserProduct)
            .WithMany(up => up.UsageLogs)
            .HasForeignKey(ul => ul.UserProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for ExpiryAlert entity
/// </summary>
public class ExpiryAlertConfiguration : IEntityTypeConfiguration<ExpiryAlert>
{
    public void Configure(EntityTypeBuilder<ExpiryAlert> builder)
    {
        builder.ToTable("expiry_alerts");

        builder.HasKey(ea => ea.Id);

        builder.Property(ea => ea.UserProductId)
            .HasColumnName("user_product_id")
            .IsRequired();

        builder.Property(ea => ea.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ea => ea.AlertType)
            .HasColumnName("alert_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(ea => ea.Message)
            .HasColumnName("message")
            .IsRequired();

        builder.Property(ea => ea.TriggerDate)
            .HasColumnName("trigger_date")
            .IsRequired();

        builder.Property(ea => ea.SentDate)
            .HasColumnName("sent_date");

        builder.Property(ea => ea.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasDefaultValue("Pending");

        builder.Property(ea => ea.NotificationChannel)
            .HasColumnName("notification_channel")
            .HasMaxLength(50)
            .HasDefaultValue("Push");

        builder.Property(ea => ea.ActionUrl)
            .HasColumnName("action_url");

        builder.Property(ea => ea.DismissedDate)
            .HasColumnName("dismissed_date");

        builder.Property(ea => ea.ActionedDate)
            .HasColumnName("actioned_date");

        builder.HasIndex(ea => ea.UserId)
            .HasDatabaseName("ix_expiry_alerts_user_id");

        builder.HasIndex(ea => new { ea.Status, ea.TriggerDate })
            .HasDatabaseName("ix_expiry_alerts_pending");

        builder.HasOne(ea => ea.UserProduct)
            .WithMany(up => up.ExpiryAlerts)
            .HasForeignKey(ea => ea.UserProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ea => ea.User)
            .WithMany()
            .HasForeignKey(ea => ea.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for ReplenishmentSubscription entity
/// </summary>
public class ReplenishmentSubscriptionConfiguration : IEntityTypeConfiguration<ReplenishmentSubscription>
{
    public void Configure(EntityTypeBuilder<ReplenishmentSubscription> builder)
    {
        builder.ToTable("replenishment_subscriptions");

        builder.HasKey(rs => rs.Id);

        builder.Property(rs => rs.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(rs => rs.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(rs => rs.UserProductId)
            .HasColumnName("user_product_id");

        builder.Property(rs => rs.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasDefaultValue("Active");

        builder.Property(rs => rs.FrequencyDays)
            .HasColumnName("frequency_days")
            .IsRequired();

        builder.Property(rs => rs.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(rs => rs.Price)
            .HasColumnName("price")
            .IsRequired();

        builder.Property(rs => rs.DiscountPercentage)
            .HasColumnName("discount_percentage")
            .HasDefaultValue(10);

        builder.Property(rs => rs.NextDeliveryDate)
            .HasColumnName("next_delivery_date")
            .IsRequired();

        builder.Property(rs => rs.LastDeliveryDate)
            .HasColumnName("last_delivery_date");

        builder.Property(rs => rs.TotalDeliveries)
            .HasColumnName("total_deliveries")
            .HasDefaultValue(0);

        builder.Property(rs => rs.RemainingDeliveries)
            .HasColumnName("remaining_deliveries")
            .HasDefaultValue(0);

        builder.Property(rs => rs.ShippingAddressJson)
            .HasColumnName("shipping_address_json")
            .HasColumnType("jsonb");

        builder.Property(rs => rs.PaymentMethodId)
            .HasColumnName("payment_method_id");

        builder.Property(rs => rs.IsNotificationEnabled)
            .HasColumnName("is_notification_enabled")
            .HasDefaultValue(true);

        builder.Property(rs => rs.NotificationDaysBefore)
            .HasColumnName("notification_days_before")
            .HasDefaultValue(3);

        builder.Property(rs => rs.Notes)
            .HasColumnName("notes");

        builder.Property(rs => rs.CancelledDate)
            .HasColumnName("cancelled_date");

        builder.Property(rs => rs.CancellationReason)
            .HasColumnName("cancellation_reason");

        builder.HasIndex(rs => rs.UserId)
            .HasDatabaseName("ix_replenishment_subscriptions_user_id");

        builder.HasIndex(rs => rs.Status)
            .HasDatabaseName("ix_replenishment_subscriptions_status");

        builder.HasIndex(rs => rs.NextDeliveryDate)
            .HasDatabaseName("ix_replenishment_subscriptions_next_delivery");

        builder.HasOne(rs => rs.User)
            .WithMany()
            .HasForeignKey(rs => rs.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rs => rs.Product)
            .WithMany()
            .HasForeignKey(rs => rs.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rs => rs.UserProduct)
            .WithMany()
            .HasForeignKey(rs => rs.UserProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(rs => rs.ReplenishmentOrders)
            .WithOne(ro => ro.Subscription)
            .HasForeignKey(ro => ro.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for ReplenishmentOrder entity
/// </summary>
public class ReplenishmentOrderConfiguration : IEntityTypeConfiguration<ReplenishmentOrder>
{
    public void Configure(EntityTypeBuilder<ReplenishmentOrder> builder)
    {
        builder.ToTable("replenishment_orders");

        builder.HasKey(ro => ro.Id);

        builder.Property(ro => ro.SubscriptionId)
            .HasColumnName("subscription_id")
            .IsRequired();

        builder.Property(ro => ro.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ro => ro.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(ro => ro.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(ro => ro.UnitPrice)
            .HasColumnName("unit_price")
            .IsRequired();

        builder.Property(ro => ro.TotalPrice)
            .HasColumnName("total_price")
            .IsRequired();

        builder.Property(ro => ro.DiscountApplied)
            .HasColumnName("discount_applied")
            .HasDefaultValue(0);

        builder.Property(ro => ro.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasDefaultValue("Pending");

        builder.Property(ro => ro.ScheduledDate)
            .HasColumnName("scheduled_date")
            .IsRequired();

        builder.Property(ro => ro.ProcessedDate)
            .HasColumnName("processed_date");

        builder.Property(ro => ro.ShippedDate)
            .HasColumnName("shipped_date");

        builder.Property(ro => ro.DeliveredDate)
            .HasColumnName("delivered_date");

        builder.Property(ro => ro.FailureReason)
            .HasColumnName("failure_reason");

        builder.Property(ro => ro.TrackingNumber)
            .HasColumnName("tracking_number");

        builder.Property(ro => ro.Carrier)
            .HasColumnName("carrier");

        builder.Property(ro => ro.ShippingAddressJson)
            .HasColumnName("shipping_address_json")
            .HasColumnType("jsonb");

        builder.Property(ro => ro.RelatedOrderId)
            .HasColumnName("related_order_id");

        builder.HasIndex(ro => ro.SubscriptionId)
            .HasDatabaseName("ix_replenishment_orders_subscription_id");

        builder.HasIndex(ro => ro.Status)
            .HasDatabaseName("ix_replenishment_orders_status");

        builder.HasIndex(ro => ro.ScheduledDate)
            .HasDatabaseName("ix_replenishment_orders_scheduled");

        builder.HasOne(ro => ro.Subscription)
            .WithMany(rs => rs.ReplenishmentOrders)
            .HasForeignKey(ro => ro.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ro => ro.User)
            .WithMany()
            .HasForeignKey(ro => ro.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ro => ro.Product)
            .WithMany()
            .HasForeignKey(ro => ro.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
