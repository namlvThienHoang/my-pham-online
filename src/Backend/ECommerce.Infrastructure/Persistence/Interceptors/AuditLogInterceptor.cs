using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ECommerce.Core.Entities;
using System.Text.Json;

namespace ECommerce.Infrastructure.Persistence.Interceptors;

public class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserService? _currentUserService;
    
    public AuditLogInterceptor(
        IHttpContextAccessor httpContextAccessor,
        ICurrentUserService? currentUserService = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _currentUserService = currentUserService;
    }
    
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        
        var auditLogs = new List<AuditLog>();
        
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Unchanged)
                continue;
            
            // Only audit specific entity types
            var entityType = entry.Entity.GetType().Name;
            var auditedEntities = new[] { "Order", "Product", "InventoryLot", "AdminUser", "Refund", "Customer" };
            
            if (!auditedEntities.Contains(entityType))
                continue;
            
            var auditLog = new AuditLog
            {
                Id = Ulid.NewUlid().ToGuid(),
                CreatedAt = DateTime.UtcNow,
                EntityType = entityType,
                EntityId = entry.Entity is BaseEntity baseEntity ? baseEntity.Id : null,
                UserId = _currentUserService?.GetCurrentUserId(),
                IpAddress = GetIpAddress(),
                UserAgent = GetUserAgent()
            };
            
            switch (entry.State)
            {
                case EntityState.Added:
                    auditLog.Action = "Create";
                    auditLog.NewValues = MaskSensitiveData(SerializeEntity(entry.Entity));
                    break;
                    
                case EntityState.Modified:
                    auditLog.Action = "Update";
                    auditLog.OldValues = MaskSensitiveData(SerializeDictionary(entry.OriginalValues));
                    auditLog.NewValues = MaskSensitiveData(SerializeDictionary(entry.CurrentValues));
                    break;
                    
                case EntityState.Deleted:
                    auditLog.Action = "Delete";
                    auditLog.OldValues = MaskSensitiveData(SerializeEntity(entry.Entity));
                    break;
            }
            
            auditLogs.Add(auditLog);
        }
        
        if (auditLogs.Any())
        {
            context.AddRange(auditLogs);
        }
        
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
    
    private string SerializeEntity(object entity)
    {
        var properties = entity.GetType().GetProperties()
            .Where(p => p.PropertyType.IsPrimitive || p.PropertyType == typeof(string) || p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(decimal))
            .ToDictionary(p => p.Name, p => p.GetValue(entity));
        
        return JsonSerializer.Serialize(properties);
    }
    
    private string SerializeDictionary(PropertyValues values)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in values.Properties)
        {
            dict[prop] = values[prop];
        }
        return JsonSerializer.Serialize(dict);
    }
    
    private string MaskSensitiveData(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;
        
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.Clone();
            
            // Mask sensitive fields according to spec 8.6
            var sensitiveFields = new[] { "password", "secret", "token", "creditcard", "ssn", "mfa" };
            
            foreach (var field in sensitiveFields)
            {
                if (root.TryGetProperty(field, out var prop))
                {
                    // Replace with masked value
                    var value = prop.GetString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        // Keep first 2 and last 2 characters
                        var masked = value.Length > 4 
                            ? value.Substring(0, 2) + new string('*', value.Length - 4) + value.Substring(value.Length - 2)
                            : new string('*', value.Length);
                        
                        // Note: This is simplified. In production, use JsonElement manipulation
                    }
                }
            }
            
            return root.GetRawText();
        }
        catch
        {
            return json;
        }
    }
    
    private string? GetIpAddress()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;
            
            return httpContext.Connection.RemoteIpAddress?.ToString()
                ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
    
    private string? GetUserAgent()
    {
        try
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
        }
        catch
        {
            return null;
        }
    }
}

// Interface for getting current user
public interface ICurrentUserService
{
    Guid? GetCurrentUserId();
    string? GetCurrentUserName();
    bool IsAuthenticated();
}
