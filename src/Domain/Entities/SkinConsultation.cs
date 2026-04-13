namespace BeautyEcommerce.Domain.Entities;

using BeautyEcommerce.Domain.Common;

/// <summary>
/// Skin profile entity for storing user skin analysis data
/// Used for personalized product recommendations
/// </summary>
public class SkinProfile : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public string SkinType { get; private set; } = string.Empty; // Oily, Dry, Combination, Normal, Sensitive
    public int? SensitivityLevel { get; private set; } // 1-5 scale
    public bool HasAcne { get; private set; }
    public bool HasDarkSpots { get; private set; }
    public bool HasWrinkles { get; private set; }
    public bool HasLargePores { get; private set; }
    public bool IsDehydrated { get; private set; }
    public string? SkinConcerns { get; private set; } // JSON array of concerns
    public string? CurrentRoutine { get; private set; } // JSON of current products used
    public string? EnvironmentFactors { get; private set; } // Climate, pollution exposure
    public string? LifestyleFactors { get; private set; } // Diet, sleep, stress
    public DateTime? LastAssessmentDate { get; private set; }
    public int AssessmentScore { get; private set; } // Overall skin health score 0-100
    
    // Navigation properties
    public virtual User? User { get; private set; }
    public virtual ICollection<QuizResult> QuizResults { get; private set; } = new List<QuizResult>();
    public virtual ICollection<ProductRecommendation> ProductRecommendations { get; private set; } = new List<ProductRecommendation>();
    
    private SkinProfile() { }
    
    public static SkinProfile Create(
        Guid userId,
        string skinType,
        int? sensitivityLevel = null,
        bool hasAcne = false,
        bool hasDarkSpots = false,
        bool hasWrinkles = false,
        bool hasLargePores = false,
        bool isDehydrated = false)
    {
        var profile = new SkinProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SkinType = skinType,
            SensitivityLevel = sensitivityLevel,
            HasAcne = hasAcne,
            HasDarkSpots = hasDarkSpots,
            HasWrinkles = hasWrinkles,
            HasLargePores = hasLargePores,
            IsDehydrated = isDehydrated,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        return profile;
    }
    
    public void UpdateAssessment(
        string? skinConcerns = null,
        string? currentRoutine = null,
        string? environmentFactors = null,
        string? lifestyleFactors = null,
        int assessmentScore = 0)
    {
        if (skinConcerns != null) SkinConcerns = skinConcerns;
        if (currentRoutine != null) CurrentRoutine = currentRoutine;
        if (environmentFactors != null) EnvironmentFactors = environmentFactors;
        if (lifestyleFactors != null) LifestyleFactors = lifestyleFactors;
        if (assessmentScore > 0) AssessmentScore = assessmentScore;
        
        LastAssessmentDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Quiz question entity for skin consultation
/// </summary>
public class QuizQuestion : BaseEntity, IAggregateRoot
{
    public string Category { get; private set; } = string.Empty; // Skin type, Concerns, Lifestyle
    public string QuestionText { get; private set; } = string.Empty;
    public string QuestionType { get; private set; } = string.Empty; // SingleChoice, MultipleChoice, Scale
    public int Order { get; private set; }
    public bool IsRequired { get; private set; } = true;
    public string? ImageUrl { get; private set; }
    public string? HelpText { get; private set; }
    
    // Navigation properties
    public virtual ICollection<QuizAnswer> Answers { get; private set; } = new List<QuizAnswer>();
    
    private QuizQuestion() { }
    
    public static QuizQuestion Create(
        string category,
        string questionText,
        string questionType,
        int order,
        bool isRequired = true,
        string? imageUrl = null,
        string? helpText = null)
    {
        return new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Category = category,
            QuestionText = questionText,
            QuestionType = questionType,
            Order = order,
            IsRequired = isRequired,
            ImageUrl = imageUrl,
            HelpText = helpText,
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
/// Quiz answer option entity
/// </summary>
public class QuizAnswer : BaseEntity, IAggregateRoot
{
    public Guid QuestionId { get; private set; }
    public string AnswerText { get; private set; } = string.Empty;
    public string? AnswerValue { get; private set; } // For scoring/analysis
    public int Order { get; private set; }
    public string? SkinTypeTag { get; private set; } // Links answer to skin type
    public string? ConcernTag { get; private set; } // Links answer to skin concern
    
    // Navigation properties
    public virtual QuizQuestion? Question { get; private set; }
    
    private QuizAnswer() { }
    
    public static QuizAnswer Create(
        Guid questionId,
        string answerText,
        int order,
        string? answerValue = null,
        string? skinTypeTag = null,
        string? concernTag = null)
    {
        return new QuizAnswer
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            AnswerText = answerText,
            AnswerValue = answerValue,
            Order = order,
            SkinTypeTag = skinTypeTag,
            ConcernTag = concernTag,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// User's quiz result and analysis
/// </summary>
public class QuizResult : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid? SkinProfileId { get; private set; }
    public int TotalQuestions { get; private set; }
    public int AnsweredQuestions { get; private set; }
    public int CompletionPercentage { get; private set; }
    public string? ResultsJson { get; private set; } // Detailed analysis results
    public string? RecommendedSkinType { get; private set; }
    public string? TopConcerns { get; private set; } // JSON array
    public int? Score { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CompletedAt { get; private set; }
    
    // Navigation properties
    public virtual User? User { get; private set; }
    public virtual SkinProfile? SkinProfile { get; private set; }
    public virtual ICollection<QuizAnswerLog> AnswerLogs { get; private set; } = new List<QuizAnswerLog>();
    
    private QuizResult() { }
    
    public static QuizResult Create(Guid userId, int totalQuestions)
    {
        return new QuizResult
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TotalQuestions = totalQuestions,
            AnsweredQuestions = 0,
            CompletionPercentage = 0,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void UpdateProgress(int answeredQuestions)
    {
        AnsweredQuestions = answeredQuestions;
        CompletionPercentage = (int)((double)answeredQuestions / TotalQuestions * 100);
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Complete(string resultsJson, string? recommendedSkinType = null, string? topConcerns = null, int? score = null)
    {
        ResultsJson = resultsJson;
        RecommendedSkinType = recommendedSkinType;
        TopConcerns = topConcerns;
        Score = score;
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Log of user's answers during quiz
/// </summary>
public class QuizAnswerLog : BaseEntity
{
    public Guid QuizResultId { get; private set; }
    public Guid QuestionId { get; private set; }
    public Guid? SelectedAnswerId { get; private set; }
    public string? CustomAnswer { get; private set; } // For text input questions
    public int TimeSpentSeconds { get; private set; }
    
    // Navigation properties
    public virtual QuizResult? QuizResult { get; private set; }
    
    private QuizAnswerLog() { }
    
    public static QuizAnswerLog Create(
        Guid quizResultId,
        Guid questionId,
        Guid? selectedAnswerId = null,
        string? customAnswer = null,
        int timeSpentSeconds = 0)
    {
        return new QuizAnswerLog
        {
            Id = Guid.NewGuid(),
            QuizResultId = quizResultId,
            QuestionId = questionId,
            SelectedAnswerId = selectedAnswerId,
            CustomAnswer = customAnswer,
            TimeSpentSeconds = timeSpentSeconds,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Product recommendation based on skin profile
/// </summary>
public class ProductRecommendation : BaseEntity
{
    public Guid SkinProfileId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Priority { get; private set; } // Lower number = higher priority
    public string Reason { get; private set; } = string.Empty; // Why this product is recommended
    public decimal MatchScore { get; private set; } // 0-100 match percentage
    public string? Category { get; private set; } // Cleanser, Toner, Serum, etc.
    public bool IsActive { get; private set; } = true;
    
    // Navigation properties
    public virtual SkinProfile? SkinProfile { get; private set; }
    
    private ProductRecommendation() { }
    
    public static ProductRecommendation Create(
        Guid skinProfileId,
        Guid productId,
        int priority,
        string reason,
        decimal matchScore,
        string? category = null)
    {
        return new ProductRecommendation
        {
            Id = Guid.NewGuid(),
            SkinProfileId = skinProfileId,
            ProductId = productId,
            Priority = priority,
            Reason = reason,
            MatchScore = matchScore,
            Category = category,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
