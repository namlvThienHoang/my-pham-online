namespace BeautyEcommerce.Infrastructure.Repositories;

using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(rt => rt.Token == token && !rt.DeletedAt.HasValue)
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(rt => rt.UserId == userId && !rt.DeletedAt.HasValue)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RefreshToken>> GetByFamilyIdAsync(string familyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(rt => rt.FamilyId == familyId && !rt.DeletedAt.HasValue)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await DbSet
            .Where(rt => rt.UserId == userId 
                      && !rt.DeletedAt.HasValue
                      && rt.RevokedAt == null
                      && rt.ExpiresAt > now)
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await GetActiveTokensAsync(userId, cancellationToken);
        foreach (var token in tokens)
        {
            token.Revoke();
        }
    }

    public async Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var expiredTokens = await DbSet
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow && !rt.DeletedAt.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var token in expiredTokens)
        {
            DbSet.Remove(token);
        }

        return expiredTokens.Count;
    }
}
