namespace BeautyCommerce.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BeautyEcommerce.Domain.Interfaces;
using BeautyEcommerce.Domain.Entities;
using System.Security.Claims;

/// <summary>
/// API Controller for Skin Consultation Quiz
/// Helps users find personalized product recommendations through interactive skin analysis
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SkinConsultationController : ControllerBase
{
    private readonly ILogger<SkinConsultationController> _logger;
    private readonly ISkinProfileRepository _skinProfileRepository;
    private readonly IQuizQuestionRepository _quizQuestionRepository;
    private readonly IQuizResultRepository _quizResultRepository;
    private readonly IProductRecommendationRepository _productRecommendationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SkinConsultationController(
        ILogger<SkinConsultationController> logger,
        ISkinProfileRepository skinProfileRepository,
        IQuizQuestionRepository quizQuestionRepository,
        IQuizResultRepository quizResultRepository,
        IProductRecommendationRepository productRecommendationRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _skinProfileRepository = skinProfileRepository;
        _quizQuestionRepository = quizQuestionRepository;
        _quizResultRepository = quizResultRepository;
        _productRecommendationRepository = productRecommendationRepository;
        _unitOfWork = unitOfWork;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found");
        }
        return userId;
    }

    /// <summary>
    /// Get quiz questions optionally filtered by category
    /// GET /api/v1/skin-consultation/questions
    /// </summary>
    [HttpGet("questions")]
    public async Task<ActionResult<List<QuizQuestionDto>>> GetQuestions([FromQuery] string? category = null)
    {
        try
        {
            var questions = await _quizQuestionRepository.GetActiveQuestionsAsync(category);
            
            var result = questions.Select(q => new QuizQuestionDto
            {
                Id = q.Id,
                Category = q.Category,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Order = q.Order,
                IsRequired = q.IsRequired,
                ImageUrl = q.ImageUrl,
                HelpText = q.HelpText,
                Answers = q.Answers.Select(a => new QuizAnswerDto
                {
                    Id = a.Id,
                    AnswerText = a.AnswerText,
                    AnswerValue = a.AnswerValue,
                    Order = a.Order,
                    SkinTypeTag = a.SkinTypeTag,
                    ConcernTag = a.ConcernTag
                }).OrderBy(a => a.Order).ToList()
            }).OrderBy(q => q.Order).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quiz questions");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Start a new quiz session
    /// POST /api/v1/skin-consultation/start
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<QuizResultDto>> StartQuiz()
    {
        try
        {
            var userId = GetCurrentUserId();
            var totalQuestions = (await _quizQuestionRepository.GetActiveQuestionsAsync()).Count;
            
            var quizResult = QuizResult.Create(userId, totalQuestions);
            await _quizResultRepository.AddAsync(quizResult);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new QuizResultDto
            {
                Id = quizResult.Id,
                UserId = quizResult.UserId,
                TotalQuestions = quizResult.TotalQuestions,
                AnsweredQuestions = quizResult.AnsweredQuestions,
                CompletionPercentage = quizResult.CompletionPercentage,
                IsCompleted = quizResult.IsCompleted,
                CreatedAt = quizResult.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting quiz");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Submit an answer to a quiz question
    /// POST /api/v1/skin-consultation/answer
    /// </summary>
    [HttpPost("answer")]
    public async Task<ActionResult> SubmitAnswer([FromBody] SubmitAnswerRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var quizResult = await _quizResultRepository.GetByIdAsync(request.QuizResultId);
            if (quizResult == null || quizResult.UserId != userId)
            {
                return NotFound("Quiz session not found");
            }

            // Log the answer
            var answerLog = QuizAnswerLog.Create(
                request.QuizResultId,
                request.QuestionId,
                request.SelectedAnswerId,
                request.CustomAnswer,
                request.TimeSpentSeconds
            );
            
            // Update progress
            quizResult.UpdateProgress(quizResult.AnsweredQuestions + 1);
            
            await _unitOfWork.SaveChangesAsync();

            return Ok(new 
            { 
                Success = true,
                CompletionPercentage = quizResult.CompletionPercentage 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Complete quiz and get results with product recommendations
    /// POST /api/v1/skin-consultation/complete
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult<QuizCompletionResult>> CompleteQuiz([FromBody] CompleteQuizRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var quizResult = await _quizResultRepository.GetByIdAsync(request.QuizResultId);
            if (quizResult == null || quizResult.UserId != userId)
            {
                return NotFound("Quiz session not found");
            }

            // Complete the quiz
            quizResult.Complete(
                request.ResultsJson,
                request.RecommendedSkinType,
                request.TopConcerns,
                request.Score
            );

            // Create or update skin profile
            var skinProfile = SkinProfile.Create(
                userId,
                request.RecommendedSkinType ?? "Normal"
            );
            
            skinProfile.UpdateAssessment(
                request.TopConcerns,
                request.CurrentRoutine,
                request.EnvironmentFactors,
                request.LifestyleFactors,
                request.Score ?? 0
            );

            await _skinProfileRepository.CreateOrUpdateAsync(skinProfile);
            
            // Generate product recommendations (simplified - in real app would use algorithm)
            var recommendations = new List<ProductRecommendation>();
            if (request.RecommendedProducts != null)
            {
                foreach (var prod in request.RecommendedProducts.Take(10))
                {
                    recommendations.Add(ProductRecommendation.Create(
                        skinProfile.Id,
                        prod.ProductId,
                        prod.Priority,
                        prod.Reason,
                        prod.MatchScore,
                        prod.Category
                    ));
                }
                
                await _productRecommendationRepository.UpdateRecommendationsAsync(skinProfile.Id, recommendations);
            }

            await _unitOfWork.SaveChangesAsync();

            // Get recommendations
            var recommendedProducts = await _productRecommendationRepository.GetBySkinProfileIdAsync(skinProfile.Id, 10);

            return Ok(new QuizCompletionResult
            {
                QuizResultId = quizResult.Id,
                SkinProfileId = skinProfile.Id,
                RecommendedSkinType = skinProfile.SkinType,
                AssessmentScore = skinProfile.AssessmentScore,
                Recommendations = recommendedProducts.Select(r => new ProductRecommendationDto
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    Priority = r.Priority,
                    Reason = r.Reason,
                    MatchScore = r.MatchScore,
                    Category = r.Category
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing quiz");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get user's current skin profile and recommendations
    /// GET /api/v1/skin-consultation/profile
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<SkinProfileDto>> GetSkinProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            var skinProfile = await _skinProfileRepository.GetByUserIdAsync(userId);
            
            if (skinProfile == null)
            {
                return NotFound("No skin profile found. Please complete the quiz first.");
            }

            var recommendations = await _productRecommendationRepository.GetBySkinProfileIdAsync(skinProfile.Id, 10);

            return Ok(new SkinProfileDto
            {
                Id = skinProfile.Id,
                SkinType = skinProfile.SkinType,
                SensitivityLevel = skinProfile.SensitivityLevel,
                HasAcne = skinProfile.HasAcne,
                HasDarkSpots = skinProfile.HasDarkSpots,
                HasWrinkles = skinProfile.HasWrinkles,
                HasLargePores = skinProfile.HasLargePores,
                IsDehydrated = skinProfile.IsDehydrated,
                AssessmentScore = skinProfile.AssessmentScore,
                LastAssessmentDate = skinProfile.LastAssessmentDate,
                Recommendations = recommendations.Select(r => new ProductRecommendationDto
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    Priority = r.Priority,
                    Reason = r.Reason,
                    MatchScore = r.MatchScore,
                    Category = r.Category
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting skin profile");
            return StatusCode(500, "Internal server error");
        }
    }
}

// DTOs
public record QuizQuestionDto
{
    public Guid Id { get; init; }
    public string Category { get; init; } = string.Empty;
    public string QuestionText { get; init; } = string.Empty;
    public string QuestionType { get; init; } = string.Empty;
    public int Order { get; init; }
    public bool IsRequired { get; init; }
    public string? ImageUrl { get; init; }
    public string? HelpText { get; init; }
    public List<QuizAnswerDto> Answers { get; init; } = new();
}

public record QuizAnswerDto
{
    public Guid Id { get; init; }
    public string AnswerText { get; init; } = string.Empty;
    public string? AnswerValue { get; init; }
    public int Order { get; init; }
    public string? SkinTypeTag { get; init; }
    public string? ConcernTag { get; init; }
}

public record QuizResultDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public int TotalQuestions { get; init; }
    public int AnsweredQuestions { get; init; }
    public int CompletionPercentage { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SubmitAnswerRequest
{
    public Guid QuizResultId { get; init; }
    public Guid QuestionId { get; init; }
    public Guid? SelectedAnswerId { get; init; }
    public string? CustomAnswer { get; init; }
    public int TimeSpentSeconds { get; init; }
}

public record CompleteQuizRequest
{
    public Guid QuizResultId { get; init; }
    public string ResultsJson { get; init; } = string.Empty;
    public string? RecommendedSkinType { get; init; }
    public string? TopConcerns { get; init; }
    public string? CurrentRoutine { get; init; }
    public string? EnvironmentFactors { get; init; }
    public string? LifestyleFactors { get; init; }
    public int? Score { get; init; }
    public List<RecommendedProductRequest>? RecommendedProducts { get; init; }
}

public record RecommendedProductRequest
{
    public Guid ProductId { get; init; }
    public int Priority { get; init; }
    public string Reason { get; init; } = string.Empty;
    public decimal MatchScore { get; init; }
    public string? Category { get; init; }
}

public record QuizCompletionResult
{
    public Guid QuizResultId { get; init; }
    public Guid SkinProfileId { get; init; }
    public string RecommendedSkinType { get; init; } = string.Empty;
    public int AssessmentScore { get; init; }
    public List<ProductRecommendationDto> Recommendations { get; init; } = new();
}

public record SkinProfileDto
{
    public Guid Id { get; init; }
    public string SkinType { get; init; } = string.Empty;
    public int? SensitivityLevel { get; init; }
    public bool HasAcne { get; init; }
    public bool HasDarkSpots { get; init; }
    public bool HasWrinkles { get; init; }
    public bool HasLargePores { get; init; }
    public bool IsDehydrated { get; init; }
    public int AssessmentScore { get; init; }
    public DateTime? LastAssessmentDate { get; init; }
    public List<ProductRecommendationDto> Recommendations { get; init; } = new();
}

public record ProductRecommendationDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public int Priority { get; init; }
    public string Reason { get; init; } = string.Empty;
    public decimal MatchScore { get; init; }
    public string? Category { get; init; }
}
