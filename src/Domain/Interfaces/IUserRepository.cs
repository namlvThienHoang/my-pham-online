namespace BeautyEcommerce.Domain.Interfaces;

using BeautyEcommerce.Domain.Entities;
using System.Linq.Expressions;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithTokensAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default);
}
