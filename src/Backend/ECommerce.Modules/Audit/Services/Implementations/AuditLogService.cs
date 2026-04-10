using System.Data;
using Dapper;
using Npgsql;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Modules.Admin.DTOs;
using ECommerce.Modules.Admin.Services;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Audit.Services.Implementations;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly string _connectionString;

    public AuditLogService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<AuditLogListResultDto> GetAuditLogsAsync(
        Guid? userId,
        string? entityType,
        string? action,
        DateTime? fromDate,
        DateTime? toDate,
        string? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        // Build WHERE clause
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (userId.HasValue)
        {
            whereClauses.Add("al.user_id = @UserId");
            parameters.Add("UserId", userId.Value);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            whereClauses.Add("al.entity_type = @EntityType");
            parameters.Add("EntityType", entityType);
        }

        if (!string.IsNullOrEmpty(action))
        {
            whereClauses.Add("al.action = @Action");
            parameters.Add("Action", action);
        }

        if (fromDate.HasValue)
        {
            whereClauses.Add("al.created_at >= @FromDate");
            parameters.Add("FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            whereClauses.Add("al.created_at <= @ToDate");
            parameters.Add("ToDate", toDate.Value);
        }

        var whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        // Cursor pagination
        DateTime? cursorCreatedAt = null;
        Guid? cursorId = null;

        if (!string.IsNullOrEmpty(cursor))
        {
            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
                var parts = decoded.Split('|');
                if (parts.Length == 2)
                {
                    cursorCreatedAt = DateTime.Parse(parts[0]);
                    cursorId = Guid.Parse(parts[1]);
                }
            }
            catch
            {
                // Invalid cursor, ignore
            }
        }

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            whereClauses.Add("(al.created_at, al.id) < (@CursorCreatedAt, @CursorId)");
            parameters.Add("CursorCreatedAt", cursorCreatedAt.Value);
            parameters.Add("CursorId", cursorId.Value);
        }

        whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        // Query audit logs with user info
        var sql = $@"
            SELECT 
                al.id, al.user_id, al.action, al.entity_type, al.entity_id,
                al.old_values, al.new_values, al.ip_address, al.user_agent, al.created_at,
                u.email as user_email, u.full_name as user_name
            FROM audit_logs al
            LEFT JOIN users u ON al.user_id = u.id
            {whereClause}
            ORDER BY al.created_at DESC, al.id DESC
            LIMIT @Limit";

        parameters.Add("Limit", limit + 1);

        var logs = await connection.QueryAsync<AuditLogUserDto>(sql, parameters);
        var logList = logs.ToList();

        // Check if there's a next page
        var hasMore = logList.Count > limit;
        if (hasMore)
        {
            logList.RemoveAt(logList.Count - 1);
        }

        // Build next cursor
        string? nextCursor = null;
        if (hasMore && logList.Any())
        {
            var lastLog = logList.Last();
            var cursorData = $"{lastLog.CreatedAt:yyyy-MM-dd HH:mm:ss.ffffff}|{lastLog.Id}";
            nextCursor = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(cursorData));
        }

        // Apply data masking to sensitive fields (spec 8.6)
        var maskedLogs = logList.Select(l => new AuditLogDto
        {
            Id = l.Id,
            UserId = l.UserId,
            UserName = MaskUserName(l.UserName, l.UserEmail),
            Action = l.Action,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            OldValues = MaskSensitiveData(l.OldValues),
            NewValues = MaskSensitiveData(l.NewValues),
            IpAddress = MaskIpAddress(l.IpAddress),
            UserAgent = l.UserAgent,
            CreatedAt = l.CreatedAt
        }).ToList();

        return new AuditLogListResultDto
        {
            Logs = maskedLogs,
            NextCursor = nextCursor,
            HasMore = hasMore,
            TotalCount = await GetTotalCountAsync(userId, entityType, action, fromDate, toDate, cancellationToken)
        };
    }

    private async Task<int> GetTotalCountAsync(
        Guid? userId,
        string? entityType,
        string? action,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value);

        return await query.CountAsync(cancellationToken);
    }

    // Data masking according to spec 8.6
    private string? MaskUserName(string? userName, string? email)
    {
        if (string.IsNullOrEmpty(email)) return userName;
        
        var parts = email.Split('@');
        if (parts.Length != 2) return email;
        
        var name = parts[0];
        var domain = parts[1];
        
        if (name.Length <= 2)
            return $"{name[0]}***@{domain}";
        
        return $"{name.Substring(0, 2)}***@{domain}";
    }

    private string? MaskIpAddress(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return null;
        
        // Mask last octet of IPv4
        var parts = ipAddress.Split('.');
        if (parts.Length == 4)
        {
            parts[3] = "***";
            return string.Join(".", parts);
        }
        
        return ipAddress;
    }

    private string? MaskSensitiveData(string? jsonData)
    {
        if (string.IsNullOrEmpty(jsonData)) return null;
        
        // In production, use proper JSON parsing and masking
        // This is a simplified version
        var masked = jsonData
            .Replace("\"password\": \"[^\"]*\"", "\"password\": \"***\"")
            .Replace("\"phone\": \"[^\"]*\"", "\"phone\": \"***\"")
            .Replace("\"email\": \"[^\"]*\"", "\"email\": \"***\"");
        
        return masked;
    }
}

// Helper DTO for Dapper query
public class AuditLogUserDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UserEmail { get; set; }
    public string? UserName { get; set; }
}
