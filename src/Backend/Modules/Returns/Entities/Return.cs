using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Modules.Returns.Entities;

public class ReturnRequest
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    public Guid OrderId { get; set; }
    
    public string Status { get; set; } = "requested"; // requested, approved, rejected, shipped, received, completed, cancelled
    
    public string? RefundMethod { get; set; } // original, store_credit, gift_card
    
    public decimal? TotalRefundAmount { get; set; }
    
    public string? AdminNote { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ReturnItem
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid ReturnRequestId { get; set; }
    
    public Guid OrderItemId { get; set; }
    
    public int Quantity { get; set; }
    
    public string? Reason { get; set; }
    
    public decimal RefundAmount { get; set; }
}

public class ReturnMedia
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid ReturnRequestId { get; set; }
    
    public string MediaUrl { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
