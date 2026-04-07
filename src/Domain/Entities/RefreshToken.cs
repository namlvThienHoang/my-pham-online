namespace BeautyEcommerce.Domain.Entities;

using BeautyEcommerce.Domain.Common;

/// <summary>
/// Refresh token entity with family tracking for token rotation
/// </summary>
public class RefreshToken : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? FamilyId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    // Navigation properties
    public virtual User User { get; private set; } = null!;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt, string? familyId = null, string? ipAddress = null, string? userAgent = null)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            FamilyId = familyId ?? Guid.NewGuid().ToString(),
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    public void Revoke(string? replacedByToken = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = replacedByToken;
    }

    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
    
    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;
    
    public bool IsRevoked => RevokedAt != null;
}
