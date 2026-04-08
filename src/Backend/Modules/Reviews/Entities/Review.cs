using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Modules.Reviews.Entities;

public class Review
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    public Guid ProductId { get; set; }
    
    public Guid OrderItemId { get; set; }
    
    [Range(1, 5)]
    public int Rating { get; set; }
    
    public string? Title { get; set; }
    
    public string? Content { get; set; }
    
    public string Status { get; set; } = "pending"; // pending, approved, rejected
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int RowVersion { get; set; } = 0;
    
    // Navigation properties
    public virtual ICollection<ReviewMedia> Media { get; set; } = new List<ReviewMedia>();
    public virtual ICollection<ReviewHelpfulVote> HelpfulVotes { get; set; } = new List<ReviewHelpfulVote>();
}

public class ReviewMedia
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid ReviewId { get; set; }
    
    public string MediaUrl { get; set; } = string.Empty;
    
    public string MediaType { get; set; } = string.Empty; // image, video
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual Review Review { get; set; } = null!;
}

public class ReviewHelpfulVote
{
    public Guid ReviewId { get; set; }
    
    public Guid UserId { get; set; }
    
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    
    public virtual Review Review { get; set; } = null!;
}
