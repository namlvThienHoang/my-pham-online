namespace BeautyEcommerce.Infrastructure.Repositories;

using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get paged products with eager loading to prevent N+1 query
    /// Includes Category, Brand, Images, and Variants in a single query
    /// </summary>
    public async Task<(IReadOnlyList<Product> Items, string NextCursor)> GetPagedAsync(
        int pageSize, 
        string? cursor, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.CreatedAt);

        if (!string.IsNullOrEmpty(cursor))
        {
            var cursorDate = DateTime.Parse(cursor);
            query = query.Where(p => p.CreatedAt < cursorDate);
        }

        var items = await query
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        var resultItems = hasMore ? items.Take(pageSize).ToList() : items;

        var nextCursor = hasMore 
            ? resultItems.Last().CreatedAt.ToString("O") 
            : null;

        return (resultItems, nextCursor ?? string.Empty);
    }

    public Task SyncToElasticsearchAsync(Product product, CancellationToken cancellationToken = default)
    {
        // Elasticsearch sync implementation
        return Task.CompletedTask;
    }

    public Task RemoveFromElasticsearchAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // Elasticsearch remove implementation
        return Task.CompletedTask;
    }
}
