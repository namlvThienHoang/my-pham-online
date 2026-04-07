namespace BeautyEcommerce.Application.Features.Products.Commands.CreateProduct;

using MediatR;
using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Interfaces;

public record CreateProductCommand : IRequest<Guid>
{
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public Guid CategoryId { get; init; }
    public Guid BrandId { get; init; }
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public decimal Cost { get; init; }
    public int StockQuantity { get; init; }
    public int LowStockThreshold { get; init; } = 10;
    public bool TrackInventory { get; init; } = true;
    public bool AllowBackorder { get; init; }
    public bool IsPublished { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsVirtual { get; init; }
    public decimal Weight { get; init; }
    public decimal? Length { get; init; }
    public decimal? Width { get; init; }
    public decimal? Height { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? MetaKeywords { get; init; }
    public string Slug { get; init; } = string.Empty;
    public List<ProductImageDto>? Images { get; init; }
    public List<ProductVariantDto>? Variants { get; init; }
}

public record ProductImageDto
{
    public string Url { get; init; } = string.Empty;
    public string? AltText { get; init; }
    public int Position { get; init; }
    public bool IsPrimary { get; init; }
}

public record ProductVariantDto
{
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

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Sku = request.Sku,
                Name = request.Name,
                Description = request.Description,
                ShortDescription = request.ShortDescription,
                CategoryId = request.CategoryId,
                BrandId = request.BrandId,
                Price = request.Price,
                CompareAtPrice = request.CompareAtPrice,
                Cost = request.Cost,
                StockQuantity = request.StockQuantity,
                LowStockThreshold = request.LowStockThreshold,
                TrackInventory = request.TrackInventory,
                AllowBackorder = request.AllowBackorder,
                IsPublished = request.IsPublished,
                IsFeatured = request.IsFeatured,
                IsVirtual = request.IsVirtual,
                Weight = request.Weight,
                Length = request.Length,
                Width = request.Width,
                Height = request.Height,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                MetaKeywords = request.MetaKeywords,
                Slug = request.Slug,
                PublishedAt = request.IsPublished ? DateTime.UtcNow : null
            };

            // Add images
            if (request.Images != null && request.Images.Any())
            {
                foreach (var imageDto in request.Images)
                {
                    product.Images.Add(new ProductImage
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        Url = imageDto.Url,
                        AltText = imageDto.AltText,
                        Position = imageDto.Position,
                        IsPrimary = imageDto.IsPrimary
                    });
                }
            }

            // Add variants
            if (request.Variants != null && request.Variants.Any())
            {
                foreach (var variantDto in request.Variants)
                {
                    product.Variants.Add(new ProductVariant
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        Sku = variantDto.Sku,
                        Name = variantDto.Name,
                        Price = variantDto.Price,
                        CompareAtPrice = variantDto.CompareAtPrice,
                        StockQuantity = variantDto.StockQuantity,
                        Option1 = variantDto.Option1,
                        Option1Value = variantDto.Option1Value,
                        Option2 = variantDto.Option2,
                        Option2Value = variantDto.Option2Value
                    });
                }
            }

            await _productRepository.AddAsync(product, cancellationToken);
            
            // Sync to Elasticsearch
            await _productRepository.SyncToElasticsearchAsync(product, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Created product {ProductId} with SKU {Sku}", product.Id, product.Sku);

            return product.Id;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create product with SKU {Sku}", request.Sku);
            throw;
        }
    }
}
