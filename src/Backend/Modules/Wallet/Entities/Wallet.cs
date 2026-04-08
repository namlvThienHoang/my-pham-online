using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Modules.Wallet.Entities;

public class Wallet
{
    [Key]
    public Guid UserId { get; set; }
    
    public decimal Balance { get; set; } = 0m;
    
    public string Currency { get; set; } = "VND";
    
    public int RowVersion { get; set; } = 0;
}

public class WalletTransaction
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid WalletId { get; set; } // User_id
    
    public string Type { get; set; } = string.Empty; // earn, spend, refund, expire, admin_adjust, gift_card_load
    
    public decimal Amount { get; set; }
    
    public decimal BalanceAfter { get; set; }
    
    public Guid? ReferenceId { get; set; } // OrderID, ReturnID, GiftCardID
    
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class GiftCard
{
    [Key]
    public Guid Id { get; set; }
    
    public string CodeHash { get; set; } = string.Empty;
    
    public string OriginalCode { get; set; } = string.Empty; // Chỉ dùng khi tạo
    
    public decimal InitialBalance { get; set; }
    
    public decimal CurrentBalance { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    public string Status { get; set; } = "active"; // active, used, expired, cancelled
    
    public Guid? PurchasedByUserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
