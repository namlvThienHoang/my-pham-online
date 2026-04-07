namespace BeautyEcommerce.Domain.Entities;

using BeautyEcommerce.Domain.Common;
using BeautyEcommerce.Domain.Enums;

/// <summary>
/// User entity with soft delete and row versioning
/// </summary>
public class User : BaseEntity, IAggregateRoot
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? FullName { get; private set; }
    public string? PhoneNumber { get; private set; }
    public UserRole Role { get; private set; } = UserRole.Customer;
    public bool IsEmailVerified { get; private set; }
    public bool IsPhoneVerified { get; private set; }
    public bool MfaEnabled { get; private set; }
    public string? MfaSecret { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? AvatarUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public virtual ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();
    public virtual ICollection<UserAddress> Addresses { get; private set; } = new List<UserAddress>();
    public virtual ICollection<SkinProfile> SkinProfiles { get; private set; } = new List<SkinProfile>();
    public virtual ICollection<Order> Orders { get; private set; } = new List<Order>();
    public virtual ICollection<Cart> Carts { get; private set; } = new List<Cart>();
    public virtual ICollection<Wishlist> Wishlists { get; private set; } = new List<Wishlist>();
    public virtual ICollection<Review> Reviews { get; private set; } = new List<Review>();
    public virtual ICollection<WalletTransaction> WalletTransactions { get; private set; } = new List<WalletTransaction>();
    public virtual ICollection<AuditLog> AuditLogs { get; private set; } = new List<AuditLog>();

    private User() { }

    public static User Create(string email, string passwordHash, string? fullName = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLower().Trim(),
            PasswordHash = passwordHash,
            FullName = fullName,
            Role = UserRole.Customer,
            IsEmailVerified = false,
            IsPhoneVerified = false,
            MfaEnabled = false,
            IsActive = true,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Email));
        return user;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(15);
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unlock()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsLocked() => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

    public void EnableMfa(string secret)
    {
        MfaEnabled = true;
        MfaSecret = secret;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableMfa()
    {
        MfaEnabled = false;
        MfaSecret = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PasswordChangedEvent(Id));
    }

    public void UpdateProfile(string? fullName, string? phoneNumber, string? avatarUrl)
    {
        if (fullName != null) FullName = fullName;
        if (phoneNumber != null) PhoneNumber = phoneNumber;
        if (avatarUrl != null) AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRole(UserRole role)
    {
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class UserCreatedEvent : IDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }

    public UserCreatedEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

public class PasswordChangedEvent : IDomainEvent
{
    public Guid UserId { get; }

    public PasswordChangedEvent(Guid userId)
    {
        UserId = userId;
    }
}
