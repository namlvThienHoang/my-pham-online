namespace BeautyEcommerce.Infrastructure.Repositories;

using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class SkinProfileRepository : Repository<SkinProfile>, ISkinProfileRepository
{
    public SkinProfileRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<SkinProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(sp => sp.ProductRecommendations)
            .FirstOrDefaultAsync(sp => sp.UserId == userId, cancellationToken);
    }

    public async Task<SkinProfile> CreateOrUpdateAsync(SkinProfile profile, CancellationToken cancellationToken = default)
    {
        var existing = await GetByUserIdAsync(profile.UserId, cancellationToken);
        
        if (existing != null)
        {
            existing.UpdateAssessment(
                profile.SkinConcerns,
                profile.CurrentRoutine,
                profile.EnvironmentFactors,
                profile.LifestyleFactors,
                profile.AssessmentScore
            );
            
            await UpdateAsync(existing, cancellationToken);
            return existing;
        }
        
        await AddAsync(profile, cancellationToken);
        return profile;
    }
}

public class QuizQuestionRepository : Repository<QuizQuestion>, IQuizQuestionRepository
{
    public QuizQuestionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<QuizQuestion>> GetActiveQuestionsAsync(string? category = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(q => q.Answers)
            .Where(q => q.IsRequired)
            .OrderBy(q => q.Order)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(q => q.Category == category);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<QuizQuestion> GetWithAnswersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"QuizQuestion with ID {id} not found");
    }
}

public class QuizResultRepository : Repository<QuizResult>, IQuizResultRepository
{
    public QuizResultRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<QuizResult?> GetByUserIdAsync(Guid userId, bool includeLogs = false, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(qr => qr.UserId == userId)
            .OrderByDescending(qr => qr.CreatedAt)
            .AsQueryable();

        if (includeLogs)
        {
            query = query.Include(qr => qr.AnswerLogs);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<QuizResult> GetWithLogsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(qr => qr.AnswerLogs)
            .FirstOrDefaultAsync(qr => qr.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"QuizResult with ID {id} not found");
    }

    public async Task<IReadOnlyList<QuizResult>> GetUserQuizHistoryAsync(Guid userId, int limit = 10, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(qr => qr.UserId == userId)
            .OrderByDescending(qr => qr.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}

public class ProductRecommendationRepository : Repository<ProductRecommendation>, IProductRecommendationRepository
{
    public ProductRecommendationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ProductRecommendation>> GetBySkinProfileIdAsync(Guid skinProfileId, int limit = 10, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(pr => pr.SkinProfileId == skinProfileId && pr.IsActive)
            .OrderBy(pr => pr.Priority)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductRecommendation>> GetRecommendedProductsAsync(Guid userId, string? category = null, int limit = 10, CancellationToken cancellationToken = default)
    {
        var query = from sp in _context.Set<SkinProfile>()
                    join pr in DbSet on sp.Id equals pr.SkinProfileId
                    where sp.UserId == userId && pr.IsActive
                    orderby pr.Priority
                    select pr;

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(pr => pr.Category == category);
        }

        return await query.Take(limit).ToListAsync(cancellationToken);
    }

    public async Task UpdateRecommendationsAsync(Guid skinProfileId, IEnumerable<ProductRecommendation> recommendations, CancellationToken cancellationToken = default)
    {
        var existing = await DbSet
            .Where(pr => pr.SkinProfileId == skinProfileId)
            .ToListAsync(cancellationToken);

        foreach (var rec in existing)
        {
            rec.Deactivate();
        }

        await _context.AddRangeAsync(recommendations, cancellationToken);
    }
}
