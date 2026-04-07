namespace BeautyEcommerce.Domain.Entities;

using BeautyEcommerce.Domain.Common;
using BeautyEcommerce.Domain.Enums;

/// <summary>
/// User entity with soft delete and row versioning
/// </summary>
public class User : BaseEntity, IAggregateRoot
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? AvatarUrl { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    public virtual ICollection<SkinProfile> SkinProfiles { get; set; } = new List<SkinProfile>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();
    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}

/// <summary>
/// User address entity
/// </summary>
public class UserAddress : BaseEntity
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string Ward { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "VN";
    public string? PostalCode { get; set; }
    public bool IsDefault { get; set; }
    public AddressType Type { get; set; } = AddressType.Home;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}

public enum AddressType
{
    Home = 0,
    Office = 1,
    Other = 2
}

/// <summary>
/// Skin profile for beauty recommendations
/// </summary>
public class SkinProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public int Age { get; set; }
    public SkinType SkinType { get; set; }
    public bool HasAcne { get; set; }
    public bool HasDarkSpots { get; set; }
    public bool HasWrinkles { get; set; }
    public bool IsSensitive { get; set; }
    public string? Concerns { get; set; }
    public string? CurrentProducts { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
