namespace BeautyEcommerce.Application.Features.Products.Queries.GetProducts;

using MediatR;
using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Interfaces;

public record GetProductsQuery : IRequest<PagedResult<ProductDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchKeyword { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? BrandId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? IsFeatured { get; init; }
    public bool? IsPublished { get; init; }
    public string? SortBy { get; init; } = "CreatedAt";
    public bool SortDescending { get; init; } = true;
    public string? Cursor { get; init; }
}

public record ProductDto
{
    public Guid Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public Guid BrandId { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public int StockQuantity { get; init; }
    public bool TrackInventory { get; init; }
    public bool AllowBackorder { get; init; }
    public bool IsPublished { get; init; }
    public bool IsFeatured { get; init; }
    public string Slug { get; init; } = string.Empty;
    public int ViewCount { get; init; }
    public int SalesCount { get; init; }
    public decimal RatingAverage { get; init; }
    public int RatingCount { get; init; }
    public List<ProductImageDto> Images { get; init; } = new();
    public List<ProductVariantDto> Variants { get; init; } = new();
}

public record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = new List<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public string? NextCursor { get; init; }
    public string? PreviousCursor { get; init; }
}

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    public GetProductsQueryHandler(
        IProductRepository productRepository,
        ILogger<GetProductsQueryHandler> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get products from repository (with cursor-based pagination)
            var (products, nextCursor) = await _productRepository.GetPagedAsync(
                request.PageSize, 
                request.Cursor, 
                cancellationToken);

            // Convert to DTOs
            var productDtos = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                ShortDescription = p.ShortDescription,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                BrandId = p.BrandId,
                BrandName = p.Brand?.Name ?? string.Empty,
                Price = p.Price,
                CompareAtPrice = p.CompareAtPrice,
                StockQuantity = p.StockQuantity,
                TrackInventory = p.TrackInventory,
                AllowBackorder = p.AllowBackorder,
                IsPublished = p.IsPublished,
                IsFeatured = p.IsFeatured,
                Slug = p.Slug,
                ViewCount = p.ViewCount,
                SalesCount = p.SalesCount,
                RatingAverage = p.RatingAverage,
                RatingCount = p.RatingCount,
                Images = p.Images.Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    Url = i.Url,
                    AltText = i.AltText,
                    Position = i.Position,
                    IsPrimary = i.IsPrimary
                }).ToList(),
                Variants = p.Variants.Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    Sku = v.Sku,
                    Name = v.Name,
                    Price = v.Price,
                    CompareAtPrice = v.CompareAtPrice,
                    StockQuantity = v.StockQuantity,
                    Option1 = v.Option1,
                    Option1Value = v.Option1Value,
                    Option2 = v.Option2,
                    Option2Value = v.Option2Value
                }).ToList()
            }).ToList();

            return new PagedResult<ProductDto>
            {
                Items = productDtos,
                TotalCount = products.Count,
                Page = request.Page,
                PageSize = request.PageSize,
                NextCursor = nextCursor,
                PreviousCursor = request.Cursor
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get products");
            throw;
        }
    }
}

public record ProductImageDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? AltText { get; init; }
    public int Position { get; init; }
    public bool IsPrimary { get; init; }
}

public record ProductVariantDto
{
    public Guid Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public int StockQuantity { get; init; }
    public string? Option1 { get; init; }
    public string? Option1Value { get; init; }
    public string? Option2 { get; init; }
    public string? Option2Value { get; init; }
}
