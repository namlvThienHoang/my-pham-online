namespace BeautyCommerce.Domain.Entities;

using BeautyCommerce.Domain.Common;
using BeautyCommerce.Domain.Enums;

/// <summary>
/// Product entity with multi-language support
/// </summary>
public class Product : BaseEntity, IAggregateRoot
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // Default language (EN)
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public Guid CategoryId { get; set; }
    public Guid BrandId { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal Cost { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 10;
    public bool TrackInventory { get; set; } = true;
    public bool AllowBackorder { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsVirtual { get; set; }
    public decimal Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int SalesCount { get; set; }
    public decimal RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public DateTime? PublishedAt { get; set; }
    
    // Navigation properties
    public virtual Category Category { get; set; } = null!;
    public virtual Brand Brand { get; set; } = null!;
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public virtual ICollection<ProductTranslation> Translations { get; set; } = new List<ProductTranslation>();
    public virtual ICollection<InventoryLot> InventoryLots { get; set; } = new List<InventoryLot>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<ProductQa> Questions { get; set; } = new List<ProductQa>();
}

/// <summary>
/// Product translation for multi-language support
/// </summary>
public class ProductTranslation : BaseEntity
{
    public Guid ProductId { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    
    // Navigation properties
    public virtual Product Product { get; set; } = null!;
}

/// <summary>
/// Product image entity
/// </summary>
public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int Position { get; set; }
    public bool IsPrimary { get; set; }
    
    // Navigation properties
    public virtual Product Product { get; set; } = null!;
}

/// <summary>
/// Product variant (e.g., different sizes, colors)
/// </summary>
public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? Option1 { get; set; } // e.g., Size
    public string? Option1Value { get; set; } // e.g., 50ml
    public string? Option2 { get; set; } // e.g., Color
    public string? Option2Value { get; set; } // e.g., Pink
    
    // Navigation properties
    public virtual Product Product { get; set; } = null!;
}

/// <summary>
/// Category entity
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public int Position { get; set; }
    public bool IsPublished { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    
    // Navigation properties
    public virtual Category? ParentCategory { get; set; }
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}

/// <summary>
/// Brand entity
/// </summary>
public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool IsPublished { get; set; }
    public int Position { get; set; }
    
    // Navigation properties
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
