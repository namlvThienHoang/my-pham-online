namespace BeautyCommerce.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BeautyEcommerce.Domain.Interfaces;
using BeautyEcommerce.Domain.Entities;
using System.Security.Claims;

/// <summary>
/// API Controller for Product Expiry Tracking and Auto-Replenishment
/// Helps users track product expiry and set up subscriptions for repeat purchases
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ReplenishmentController : ControllerBase
{
    private readonly ILogger<ReplenishmentController> _logger;
    private readonly IUserProductRepository _userProductRepository;
    private readonly IExpiryAlertRepository _expiryAlertRepository;
    private readonly IReplenishmentSubscriptionRepository _subscriptionRepository;
    private readonly IReplenishmentOrderRepository _replenishmentOrderRepository;
    private readonly IUsageLogRepository _usageLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReplenishmentController(
        ILogger<ReplenishmentController> logger,
        IUserProductRepository userProductRepository,
        IExpiryAlertRepository expiryAlertRepository,
        IReplenishmentSubscriptionRepository subscriptionRepository,
        IReplenishmentOrderRepository replenishmentOrderRepository,
        IUsageLogRepository usageLogRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _userProductRepository = userProductRepository;
        _expiryAlertRepository = expiryAlertRepository;
        _subscriptionRepository = subscriptionRepository;
        _replenishmentOrderRepository = replenishmentOrderRepository;
        _usageLogRepository = usageLogRepository;
        _unitOfWork = unitOfWork;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found");
        }
        return userId;
    }

    /// <summary>
    /// Get user's product inventory
    /// GET /api/v1/my-products
    /// </summary>
    [HttpGet("my-products")]
    public async Task<ActionResult<List<UserProductDto>>> GetUserProducts([FromQuery] string? status = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var products = await _userProductRepository.GetUserProductsAsync(userId, status);

            var result = products.Select(p => new UserProductDto
            {
                Id = p.Id,
                ProductId = p.ProductId,
                ProductName = p.Product?.Name ?? "Unknown",
                Status = p.Status,
                PurchasedDate = p.PurchasedDate,
                OpenedDate = p.OpenedDate,
                ExpiryDate = p.ExpiryDate,
                InitialQuantity = p.InitialQuantity,
                CurrentQuantity = p.CurrentQuantity,
                QuantityUnit = p.QuantityUnit,
                UsageCount = p.UsageCount,
                LastUsedDate = p.LastUsedDate,
                IsExpiryAlertEnabled = p.IsExpiryAlertEnabled,
                IsReplenishEnabled = p.IsReplenishEnabled,
                DaysUntilExpiry = p.ExpiryDate.HasValue 
                    ? (int)(p.ExpiryDate.Value - DateTime.UtcNow).TotalDays 
                    : null
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user products");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Add a product manually to inventory
    /// POST /api/v1/my-products
    /// </summary>
    [HttpPost("my-products")]
    public async Task<ActionResult<UserProductDto>> AddUserProduct([FromBody] AddUserProductRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var userProduct = UserProduct.Create(
                userId,
                request.ProductId,
                request.PurchasedDate,
                request.ShelfLifeMonths,
                request.PaOMonths,
                request.InitialQuantity,
                request.QuantityUnit
            );

            if (request.OpenedDate.HasValue)
            {
                userProduct.MarkAsOpened(request.OpenedDate.Value);
            }

            await _userProductRepository.AddAsync(userProduct);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new UserProductDto
            {
                Id = userProduct.Id,
                ProductId = userProduct.ProductId,
                Status = userProduct.Status,
                PurchasedDate = userProduct.PurchasedDate,
                OpenedDate = userProduct.OpenedDate,
                ExpiryDate = userProduct.ExpiryDate,
                InitialQuantity = userProduct.InitialQuantity,
                CurrentQuantity = userProduct.CurrentQuantity,
                QuantityUnit = userProduct.QuantityUnit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user product");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Log product usage
    /// PUT /api/v1/my-products/{id}/usage
    /// </summary>
    [HttpPut("my-products/{id:guid}/usage")]
    public async Task<ActionResult> LogProductUsage(Guid id, [FromBody] LogUsageRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userProduct = await _userProductRepository.GetByIdAsync(id);
            
            if (userProduct == null || userProduct.UserId != userId)
            {
                return NotFound("Product not found");
            }

            // Record usage
            userProduct.RecordUsage(request.QuantityUsed, request.Notes);

            // Create usage log
            var usageLog = UsageLog.Create(
                id,
                request.UsageDate.Date,
                request.QuantityUsed,
                request.ApplicationArea,
                request.TimeOfDay
            );
            usageLog.Notes = request.Notes;
            usageLog.SkinReactionRating = request.SkinReactionRating;

            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Success = true, RemainingQuantity = userProduct.CurrentQuantity });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging product usage");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get upcoming expiry alerts
    /// GET /api/v1/expiry-alerts
    /// </summary>
    [HttpGet("expiry-alerts")]
    public async Task<ActionResult<List<ExpiryAlertDto>>> GetExpiryAlerts([FromQuery] string? status = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var alerts = await _expiryAlertRepository.GetUserAlertsAsync(userId, status);

            var result = alerts.Select(a => new ExpiryAlertDto
            {
                Id = a.Id,
                AlertType = a.AlertType,
                Message = a.Message,
                TriggerDate = a.TriggerDate,
                SentDate = a.SentDate,
                Status = a.Status,
                NotificationChannel = a.NotificationChannel,
                ActionUrl = a.ActionUrl,
                DismissedDate = a.DismissedDate,
                ActionedDate = a.ActionedDate
            }).OrderByDescending(a => a.CreatedAt).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expiry alerts");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create auto-replenishment subscription
    /// POST /api/v1/replenishment-subscriptions
    /// </summary>
    [HttpPost("replenishment-subscriptions")]
    public async Task<ActionResult<SubscriptionDto>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var subscription = ReplenishmentSubscription.Create(
                userId,
                request.ProductId,
                request.FrequencyDays,
                request.Quantity,
                request.Price,
                request.DiscountPercentage,
                request.UserProductId,
                request.RemainingDeliveries
            );

            if (request.ShippingAddressJson != null)
            {
                subscription.ShippingAddressJson = request.ShippingAddressJson;
            }

            await _subscriptionRepository.AddAsync(subscription);
            await _unitOfWork.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSubscriptions), new { }, new SubscriptionDto
            {
                Id = subscription.Id,
                ProductId = subscription.ProductId,
                Status = subscription.Status,
                FrequencyDays = subscription.FrequencyDays,
                Quantity = subscription.Quantity,
                Price = subscription.Price,
                DiscountPercentage = subscription.DiscountPercentage,
                NextDeliveryDate = subscription.NextDeliveryDate,
                TotalDeliveries = subscription.TotalDeliveries,
                RemainingDeliveries = subscription.RemainingDeliveries
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get user's active subscriptions
    /// GET /api/v1/replenishment-subscriptions
    /// </summary>
    [HttpGet("replenishment-subscriptions")]
    public async Task<ActionResult<List<SubscriptionDto>>> GetSubscriptions([FromQuery] string? status = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var subscriptions = await _subscriptionRepository.GetUserSubscriptionsAsync(userId, status);

            var result = subscriptions.Select(s => new SubscriptionDto
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductName = s.Product?.Name ?? "Unknown",
                Status = s.Status,
                FrequencyDays = s.FrequencyDays,
                Quantity = s.Quantity,
                Price = s.Price,
                DiscountPercentage = s.DiscountPercentage,
                NextDeliveryDate = s.NextDeliveryDate,
                LastDeliveryDate = s.LastDeliveryDate,
                TotalDeliveries = s.TotalDeliveries,
                RemainingDeliveries = s.RemainingDeliveries,
                IsNotificationEnabled = s.IsNotificationEnabled
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscriptions");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Pause a subscription
    /// PUT /api/v1/replenishment-subscriptions/{id}/pause
    /// </summary>
    [HttpPut("replenishment-subscriptions/{id:guid}/pause")]
    public async Task<ActionResult> PauseSubscription(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var subscription = await _subscriptionRepository.GetByIdAsync(id);
            
            if (subscription == null || subscription.UserId != userId)
            {
                return NotFound("Subscription not found");
            }

            subscription.Pause();
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Subscription paused" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing subscription");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Resume a paused subscription
    /// PUT /api/v1/replenishment-subscriptions/{id}/resume
    /// </summary>
    [HttpPut("replenishment-subscriptions/{id:guid}/resume")]
    public async Task<ActionResult> ResumeSubscription(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var subscription = await _subscriptionRepository.GetByIdAsync(id);
            
            if (subscription == null || subscription.UserId != userId)
            {
                return NotFound("Subscription not found");
            }

            subscription.Resume();
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Subscription resumed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming subscription");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cancel a subscription
    /// DELETE /api/v1/replenishment-subscriptions/{id}
    /// </summary>
    [HttpDelete("replenishment-subscriptions/{id:guid}")]
    public async Task<ActionResult> CancelSubscription(Guid id, [FromQuery] string? reason = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var subscription = await _subscriptionRepository.GetByIdAsync(id);
            
            if (subscription == null || subscription.UserId != userId)
            {
                return NotFound("Subscription not found");
            }

            subscription.Cancel(reason ?? "User requested cancellation");
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Subscription cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription");
            return StatusCode(500, "Internal server error");
        }
    }
}

// DTOs
public record UserProductDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime PurchasedDate { get; init; }
    public DateTime OpenedDate { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public int InitialQuantity { get; init; }
    public int CurrentQuantity { get; init; }
    public string? QuantityUnit { get; init; }
    public int UsageCount { get; init; }
    public DateTime? LastUsedDate { get; init; }
    public bool IsExpiryAlertEnabled { get; init; }
    public bool IsReplenishEnabled { get; init; }
    public int? DaysUntilExpiry { get; init; }
}

public record AddUserProductRequest
{
    public Guid ProductId { get; init; }
    public DateTime PurchasedDate { get; init; }
    public DateTime? OpenedDate { get; init; }
    public int ShelfLifeMonths { get; init; }
    public int PaOMonths { get; init; }
    public int InitialQuantity { get; init; }
    public string? QuantityUnit { get; init; }
}

public record LogUsageRequest
{
    public DateTime UsageDate { get; init; } = DateTime.UtcNow;
    public int QuantityUsed { get; init; } = 1;
    public string? ApplicationArea { get; init; }
    public string? TimeOfDay { get; init; }
    public string? Notes { get; init; }
    public int? SkinReactionRating { get; init; }
}

public record ExpiryAlertDto
{
    public Guid Id { get; init; }
    public string AlertType { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime TriggerDate { get; init; }
    public DateTime? SentDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public string NotificationChannel { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
    public DateTime? DismissedDate { get; init; }
    public DateTime? ActionedDate { get; init; }
}

public record SubscriptionDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int FrequencyDays { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal DiscountPercentage { get; init; }
    public DateTime NextDeliveryDate { get; init; }
    public DateTime? LastDeliveryDate { get; init; }
    public int TotalDeliveries { get; init; }
    public int RemainingDeliveries { get; init; }
    public bool IsNotificationEnabled { get; init; }
}

public record CreateSubscriptionRequest
{
    public Guid ProductId { get; init; }
    public Guid? UserProductId { get; init; }
    public int FrequencyDays { get; init; } = 30;
    public int Quantity { get; init; } = 1;
    public decimal Price { get; init; }
    public decimal DiscountPercentage { get; init; } = 10;
    public int RemainingDeliveries { get; init; } = 0; // 0 = unlimited
    public string? ShippingAddressJson { get; init; }
}
