using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;

namespace BeautyCommerce.Api.Middleware;

/// <summary>
/// Middleware xử lý Idempotency cho các write operations.
/// Sử dụng Redis để lưu trữ response của request trước đó dựa trên Idempotency-Key.
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Chỉ xử lý các method viết (POST, PUT, PATCH, DELETE)
        if (!IsWriteMethod(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers["X-Idempotency-Key"].FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            // Yêu cầu bắt buộc phải có Idempotency-Key cho write operations
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "X-Idempotency-Key header is required for write operations" });
            return;
        }

        // Tạo lock per-key để tránh race condition
        var semaphore = _locks.GetOrAdd(idempotencyKey, _ => new SemaphoreSlim(1, 1));
        try
        {
            await semaphore.WaitAsync();

            // Kiểm tra xem request này đã được xử lý chưa (lấy từ Redis trong thực tế)
            // Ở đây giả lập bằng InMemory, thực tế sẽ dùng IDistributedCache hoặc Redis
            var cachedResponse = await GetCachedResponseAsync(idempotencyKey);
            
            if (cachedResponse != null)
            {
                _logger.LogInformation("Idempotent request detected: {Key}", idempotencyKey);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = cachedResponse.StatusCode;
                await context.Response.Body.WriteAsync(cachedResponse.Body);
                return;
            }

            // Lưu body response để cache
            var originalBodyStream = context.Response.Body;
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await _next(context);

            // Đọc lại response để lưu cache
            memoryStream.Position = 0;
            var responseBody = await memoryStream.ToArrayAsync();
            
            // Chỉ cache thành công (2xx)
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                await CacheResponseAsync(idempotencyKey, context.Response.StatusCode, responseBody);
            }

            // Ghi response về client
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            semaphore.Release();
            // Cleanup lock sau khi hoàn tất để tránh memory leak
            if (_locks.TryGetValue(idempotencyKey, out var s))
            {
                if (s.CurrentCount == 1) // Không còn ai đang chờ
                {
                    _locks.TryRemove(idempotencyKey, out _);
                }
            }
        }
    }

    private bool IsWriteMethod(string method)
    {
        return method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("PATCH", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("DELETE", StringComparison.OrdinalIgnoreCase);
    }

    private Task<byte[]?> GetCachedResponseAsync(string key)
    {
        // TODO: Implement với Redis: await _cache.GetAsync($"idempotency:{key}")
        // Giả lập trả về null (chưa có cache)
        return Task.FromResult<byte[]?>(null);
    }

    private Task CacheResponseAsync(string key, int statusCode, byte[] body)
    {
        // TODO: Implement với Redis: await _cache.SetAsync($"idempotency:{key}", data, expiration: 24h)
        // Cấu trúc dữ liệu: { StatusCode, Body, ContentType }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Middleware xử lý exception toàn cục, trả về format lỗi chuẩn.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var (statusCode, error) = exception switch
        {
            ArgumentException ae => (StatusCodes.Status400BadRequest, ae.Message),
            KeyNotFoundException knfe => (StatusCodes.Status404NotFound, knfe.Message),
            UnauthorizedAccessException uae => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            InvalidOperationException ioe => (StatusCodes.Status409Conflict, ioe.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        context.Response.StatusCode = statusCode;
        
        var response = new
        {
            error = error,
            traceId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
