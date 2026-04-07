namespace BeautyEcommerce.Infrastructure.Repositories;

using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(u => u.Email == email && !u.DeletedAt.HasValue)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByIdWithTokensAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.RefreshTokens)
            .Where(u => u.Id == id && !u.DeletedAt.HasValue)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(u => u.Email == email && !u.DeletedAt.HasValue);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(predicate)
            .Where(u => !u.DeletedAt.HasValue)
            .ToListAsync(cancellationToken);
    }
}
