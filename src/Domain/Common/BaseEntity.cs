namespace BeautyCommerce.Domain.Common;

/// <summary>
/// Base entity with soft delete, row versioning, and audit fields
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = UuidV7Generator.GenerateUuidV7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public ulong RowVersion { get; set; }
}

/// <summary>
/// Generates UUID v7 (timestamp-based)
/// </summary>
public static class UuidV7Generator
{
    private static readonly Random _random = new();
    
    public static Guid GenerateUuidV7()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var bytes = new byte[16];
        
        // First 6 bytes: timestamp (48 bits)
        bytes[0] = (byte)(timestamp >> 40);
        bytes[1] = (byte)(timestamp >> 32);
        bytes[2] = (byte)(timestamp >> 24);
        bytes[3] = (byte)(timestamp >> 16);
        bytes[4] = (byte)(timestamp >> 8);
        bytes[5] = (byte)timestamp;
        
        // Version byte (byte 6): set to 0x07 for UUID v7
        bytes[6] = 0x07;
        
        // Remaining 10 bytes: random
        _random.NextBytes(bytes.AsSpan(7, 9));
        
        return new Guid(bytes);
    }
}

/// <summary>
/// Base interface for aggregate roots
/// </summary>
public interface IAggregateRoot { }

/// <summary>
/// Base interface for domain events
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

/// <summary>
/// Base class for domain events
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
