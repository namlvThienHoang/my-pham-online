using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Modules.Wishlist.Entities;

public class Wishlist
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    public Guid ProductId { get; set; }
    
    public Guid? VariantId { get; set; }
    
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsDeleted { get; set; } = false;
}

public class UserRecentlyViewed
{
    public Guid UserId { get; set; }
    
    public Guid ProductId { get; set; }
    
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
}
