namespace BeautyEcommerce.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BeautyEcommerce.Domain.Entities;

/// <summary>
/// Configuration for SkinProfile entity
/// </summary>
public class SkinProfileConfiguration : IEntityTypeConfiguration<SkinProfile>
{
    public void Configure(EntityTypeBuilder<SkinProfile> builder)
    {
        builder.ToTable("skin_profiles");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(sp => sp.SkinType)
            .HasColumnName("skin_type")
            .HasMaxLength(50);

        builder.Property(sp => sp.SensitivityLevel)
            .HasColumnName("sensitivity_level");

        builder.Property(sp => sp.HasAcne)
            .HasColumnName("has_acne")
            .HasDefaultValue(false);

        builder.Property(sp => sp.HasDarkSpots)
            .HasColumnName("has_dark_spots")
            .HasDefaultValue(false);

        builder.Property(sp => sp.HasWrinkles)
            .HasColumnName("has_wrinkles")
            .HasDefaultValue(false);

        builder.Property(sp => sp.HasLargePores)
            .HasColumnName("has_large_pores")
            .HasDefaultValue(false);

        builder.Property(sp => sp.IsDehydrated)
            .HasColumnName("is_dehydrated")
            .HasDefaultValue(false);

        builder.Property(sp => sp.SkinConcerns)
            .HasColumnName("skin_concerns")
            .HasColumnType("jsonb");

        builder.Property(sp => sp.CurrentRoutine)
            .HasColumnName("current_routine")
            .HasColumnType("jsonb");

        builder.Property(sp => sp.EnvironmentFactors)
            .HasColumnName("environment_factors")
            .HasColumnType("jsonb");

        builder.Property(sp => sp.LifestyleFactors)
            .HasColumnName("lifestyle_factors")
            .HasColumnType("jsonb");

        builder.Property(sp => sp.LastAssessmentDate)
            .HasColumnName("last_assessment_date");

        builder.Property(sp => sp.AssessmentScore)
            .HasColumnName("assessment_score")
            .HasDefaultValue(0);

        builder.HasIndex(sp => sp.UserId)
            .HasDatabaseName("ix_skin_profiles_user_id")
            .IsUnique();

        builder.HasOne(sp => sp.User)
            .WithMany()
            .HasForeignKey(sp => sp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for QuizQuestion entity
/// </summary>
public class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
    public void Configure(EntityTypeBuilder<QuizQuestion> builder)
    {
        builder.ToTable("quiz_questions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Category)
            .HasColumnName("category")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(q => q.QuestionText)
            .HasColumnName("question_text")
            .IsRequired();

        builder.Property(q => q.QuestionType)
            .HasColumnName("question_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(q => q.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(q => q.IsRequired)
            .HasColumnName("is_required")
            .HasDefaultValue(true);

        builder.Property(q => q.ImageUrl)
            .HasColumnName("image_url");

        builder.Property(q => q.HelpText)
            .HasColumnName("help_text");

        builder.HasIndex(q => q.Category)
            .HasDatabaseName("ix_quiz_questions_category");

        builder.HasMany(q => q.Answers)
            .WithOne(a => a.Question)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for QuizAnswer entity
/// </summary>
public class QuizAnswerConfiguration : IEntityTypeConfiguration<QuizAnswer>
{
    public void Configure(EntityTypeBuilder<QuizAnswer> builder)
    {
        builder.ToTable("quiz_answers");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.QuestionId)
            .HasColumnName("question_id")
            .IsRequired();

        builder.Property(a => a.AnswerText)
            .HasColumnName("answer_text")
            .IsRequired();

        builder.Property(a => a.AnswerValue)
            .HasColumnName("answer_value");

        builder.Property(a => a.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(a => a.SkinTypeTag)
            .HasColumnName("skin_type_tag")
            .HasMaxLength(50);

        builder.Property(a => a.ConcernTag)
            .HasColumnName("concern_tag")
            .HasMaxLength(100);

        builder.HasIndex(a => a.QuestionId)
            .HasDatabaseName("ix_quiz_answers_question_id");
    }
}

/// <summary>
/// Configuration for QuizResult entity
/// </summary>
public class QuizResultConfiguration : IEntityTypeConfiguration<QuizResult>
{
    public void Configure(EntityTypeBuilder<QuizResult> builder)
    {
        builder.ToTable("quiz_results");

        builder.HasKey(qr => qr.Id);

        builder.Property(qr => qr.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(qr => qr.SkinProfileId)
            .HasColumnName("skin_profile_id");

        builder.Property(qr => qr.TotalQuestions)
            .HasColumnName("total_questions")
            .IsRequired();

        builder.Property(qr => qr.AnsweredQuestions)
            .HasColumnName("answered_questions")
            .HasDefaultValue(0);

        builder.Property(qr => qr.CompletionPercentage)
            .HasColumnName("completion_percentage")
            .HasDefaultValue(0);

        builder.Property(qr => qr.ResultsJson)
            .HasColumnName("results_json")
            .HasColumnType("jsonb");

        builder.Property(qr => qr.RecommendedSkinType)
            .HasColumnName("recommended_skin_type")
            .HasMaxLength(50);

        builder.Property(qr => qr.TopConcerns)
            .HasColumnName("top_concerns")
            .HasColumnType("jsonb");

        builder.Property(qr => qr.Score)
            .HasColumnName("score");

        builder.Property(qr => qr.IsCompleted)
            .HasColumnName("is_completed")
            .HasDefaultValue(false);

        builder.Property(qr => qr.CompletedAt)
            .HasColumnName("completed_at");

        builder.HasIndex(qr => qr.UserId)
            .HasDatabaseName("ix_quiz_results_user_id");

        builder.HasOne(qr => qr.User)
            .WithMany()
            .HasForeignKey(qr => qr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(qr => qr.SkinProfile)
            .WithMany(sp => sp.QuizResults)
            .HasForeignKey(qr => qr.SkinProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(qr => qr.AnswerLogs)
            .WithOne(al => al.QuizResult)
            .HasForeignKey(al => al.QuizResultId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for QuizAnswerLog entity
/// </summary>
public class QuizAnswerLogConfiguration : IEntityTypeConfiguration<QuizAnswerLog>
{
    public void Configure(EntityTypeBuilder<QuizAnswerLog> builder)
    {
        builder.ToTable("quiz_answer_logs");

        builder.HasKey(al => al.Id);

        builder.Property(al => al.QuizResultId)
            .HasColumnName("quiz_result_id")
            .IsRequired();

        builder.Property(al => al.QuestionId)
            .HasColumnName("question_id")
            .IsRequired();

        builder.Property(al => al.SelectedAnswerId)
            .HasColumnName("selected_answer_id");

        builder.Property(al => al.CustomAnswer)
            .HasColumnName("custom_answer");

        builder.Property(al => al.TimeSpentSeconds)
            .HasColumnName("time_spent_seconds")
            .HasDefaultValue(0);

        builder.HasIndex(al => al.QuizResultId)
            .HasDatabaseName("ix_quiz_answer_logs_quiz_result_id");

        builder.HasIndex(al => new { al.QuizResultId, al.QuestionId })
            .HasDatabaseName("ix_quiz_answer_logs_unique")
            .IsUnique();
    }
}

/// <summary>
/// Configuration for ProductRecommendation entity
/// </summary>
public class ProductRecommendationConfiguration : IEntityTypeConfiguration<ProductRecommendation>
{
    public void Configure(EntityTypeBuilder<ProductRecommendation> builder)
    {
        builder.ToTable("product_recommendations");

        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.SkinProfileId)
            .HasColumnName("skin_profile_id")
            .IsRequired();

        builder.Property(pr => pr.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(pr => pr.Priority)
            .HasColumnName("priority")
            .HasDefaultValue(999);

        builder.Property(pr => pr.Reason)
            .HasColumnName("reason");

        builder.Property(pr => pr.MatchScore)
            .HasColumnName("match_score")
            .HasDefaultValue(0);

        builder.Property(pr => pr.Category)
            .HasColumnName("category")
            .HasMaxLength(100);

        builder.Property(pr => pr.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(pr => pr.SkinProfileId)
            .HasDatabaseName("ix_product_recommendations_skin_profile_id");

        builder.HasIndex(pr => new { pr.SkinProfileId, pr.IsActive, pr.Priority })
            .HasDatabaseName("ix_product_recommendations_active");

        builder.HasOne(pr => pr.SkinProfile)
            .WithMany(sp => sp.ProductRecommendations)
            .HasForeignKey(pr => pr.SkinProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
