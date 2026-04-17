# Báo Cáo Kiểm Tra Bảo Mật Toàn Diện - my-pham-online

**Ngày kiểm tra:** 2025
**Người kiểm tra:** Application Security Engineer (OSCP/CISSP)
**Khung tiêu chuẩn:** OWASP Top 10:2025
**Phạm vi:** Backend (.NET/C#), Frontend (Next.js/TypeScript), Cấu hình hệ thống

---

## Bảng Tổng Hợp Kết Quả

| Hạng mục | Mức độ rủi ro | Số lỗi phát hiện | Trạng thái |
|----------|---------------|------------------|------------|
| A01 - Broken Access Control | 🟡 Trung bình | 3 | ⚠️ Cần cải thiện |
| A02 - Security Misconfiguration | 🔴 Nghiêm trọng | 5 | 🔴 Khẩn cấp |
| A03 - Software Supply Chain Failures | 🟡 Trung bình | 2 | ⚠️ Cần cập nhật |
| A04 - Cryptographic Failures | 🔴 Nghiêm trọng | 4 | 🔴 Khẩn cấp |
| A05 - Injection | 🟡 Trung bình | 3 | ⚠️ Cần cải thiện |
| A06 - Insecure Design | 🟡 Trung bình | 3 | ⚠️ Cần cải thiện |
| A07 - Identification & Authentication Failures | 🟢 Thấp | 2 | ✅ Chấp nhận được |
| A08 - Software & Data Integrity Failures | 🟢 Thấp | 1 | ✅ Chấp nhận được |
| A09 - Security Logging & Monitoring Failures | 🟡 Trung bình | 2 | ⚠️ Cần cải thiện |
| A10 - Mishandling of Exceptional Conditions | 🟡 Trung bình | 2 | ⚠️ Cần cải thiện |

---

## Chi Tiết Kiểm Tra Theo Hạng Mục

### A01 - Broken Access Control

**Mô tả rủi ro:** Người dùng có thể truy cập dữ liệu hoặc chức năng mà họ không được phép, bao gồm IDOR (Insecure Direct Object Reference), thiếu kiểm tra quyền ở cấp độ API, và lộ endpoint admin.

#### Phát hiện:

**1. Thiếu kiểm tra Authorization cho một số endpoint Admin**
- **File:** `/workspace/src/Backend/ECommerce.Modules/Admin/Endpoints/AdminEndpoints.cs`
- **Đoạn code:**
```csharp
// Line 14-16
var adminGroup = app.MapGroup("/admin/api/v1")
    .RequireAuthorization("AdminMfaPolicy")
    .WithOpenApi();
```
- **Vấn đề:** Mặc dù có `RequireAuthorization("AdminMfaPolicy")`, nhưng policy này cần được định nghĩa rõ ràng trong Program.cs với yêu cầu MFA bắt buộc. Hiện tại chưa thấy định nghĩa policy này.
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:**
```csharp
// Trong Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminMfaPolicy", policy =>
        policy.RequireRole("Admin")
              .RequireClaim("mfa_verified", "true"));
});
```

**2. Endpoint Orders có kiểm tra role nhưng chưa đồng nhất**
- **File:** `/workspace/src/Api/Controllers/OrdersController.cs`
- **Đoạn code:**
```csharp
[Authorize(Roles = "Admin")]
public class OrdersController : ControllerBase { }
```
- **Vấn đề:** Một số endpoint dùng `[Authorize(Roles = "Admin")]` trong khi admin endpoints khác dùng `.RequireAuthorization("AdminMfaPolicy")`. Thiếu đồng nhất trong cơ chế authorization.
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:** Chuẩn hóa tất cả admin endpoints sử dụng cùng một policy.

**3. Thiếu kiểm tra IDOR trong Customer endpoints**
- **File:** `/workspace/src/Backend/ECommerce.Modules/Admin/Endpoints/AdminEndpoints.cs`
- **Đoạn code:**
```csharp
// Line 37-39
adminGroup.MapGet("/customers/{customerId:guid}", GetCustomerProfile)
    .WithName("GetCustomerProfile")
    .WithSummary("Get customer profile with full details");
```
- **Vấn đề:** Endpoint nhận `customerId` làm parameter nhưng không kiểm tra xem admin có quyền truy cập customer đó không (ví dụ: customer thuộc shop/quản lý của admin).
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:** Thêm logic kiểm tra quyền trong handler:
```csharp
private static async Task<IResult> GetCustomerProfile(
    ISender sender,
    Guid customerId,
    CancellationToken ct,
    ClaimsPrincipal user) // Thêm user context
{
    // Kiểm tra admin có quyền truy cập customer này
    var currentAdminId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var query = new GetCustomerProfileQuery(customerId);
    query.RequiredAdminId = currentAdminId; // Thêm validation
    var result = await sender.Send(query, ct);
    return Results.Ok(result);
}
```

---

### A02 - Security Misconfiguration

**Mô tả rủi ro:** Cấu hình bảo mật sai như debug mode bật, error reporting lộ thông tin, default credentials, hardcoded secrets, thiếu security headers.

#### 🚨 KHẨN CẤP - Các phát hiện nghiêm trọng:

**1. Hardcoded Database Password trong appsettings.json**
- **File:** `/workspace/src/Api/appsettings.json`
- **Đoạn code:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=beauty_ecommerce;Username=postgres;Password=postgres123;Include Error Detail=true"
}
```
- **Vấn đề:**
  - Password database được hardcode (`postgres123`)
  - Connection string chứa `Include Error Detail=true` - lộ thông tin lỗi chi tiết ra client
- **Mức độ:** 🔴 Nghiêm trọng
- **Khắc phục:**
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=beauty_ecommerce;Username=postgres;Password=${DB_PASSWORD}"
  }
  ```
  Sử dụng environment variables hoặc Azure Key Vault/AWS Secrets Manager.

**2. Hardcoded JWT Secret Key**
- **File:** `/workspace/src/Api/appsettings.json`
- **Đoạn code:**
```json
"Jwt": {
  "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
  "Issuer": "BeautyCommerce",
  "Audience": "BeautyCommerceUsers"
}
```
- **Vấn đề:** JWT secret key được lưu plaintext trong file config, có thể bị lộ qua version control.
- **Mức độ:** 🔴 Nghiêm trọng
- **Khắc phục:**
```csharp
// Program.cs - Lấy từ environment variable
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? jwtSettings["Key"]; // Fallback chỉ cho development
```

**3. Default Credentials cho Hangfire Dashboard**
- **File:** `/workspace/appsettings.json`
- **Đoạn code:**
```json
"Hangfire": {
  "DashboardEnabled": true,
  "DashboardUsername": "admin",
  "DashboardPassword": "admin123"
}
```
- **Vấn đề:** Username/password mặc định (`admin/admin123`) rất dễ đoán.
- **Mức độ:** 🔴 Nghiêm trọng
- **Khắc phục:**
```csharp
// Program.cs - Sử dụng ASP.NET Core Identity hoặc custom filter
app.MapHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization = new[] { new CustomHangfireAuthorizationFilter() }
});

// CustomHangfireFilter.cs
public class CustomHangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Admin");
    }
}
```

**4. Thiếu Security Headers**
- **File:** `/workspace/src/Api/Program.cs`
- **Vấn đề:** Không tìm thấy cấu hình security headers như:
  - Content-Security-Policy (CSP)
  - X-Frame-Options
  - X-Content-Type-Options
  - Strict-Transport-Security (HSTS)
  - X-XSS-Protection
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:**
```csharp
// Thêm middleware trong Program.cs
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';");
    context.Response.Headers.Append("Strict-Transport-Security",
        "max-age=31536000; includeSubDomains");
    await next();
});
```

**5. SignalR EnableDetailedErrors trong Development**
- **File:** `/workspace/src/Api/Program.cs`
- **Đoạn code:**
```csharp
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    // ...
});
```
- **Vấn đề:** Mặc dù được guard bởi `IsDevelopment()`, nếu environment variable bị set sai, lỗi chi tiết có thể bị lộ.
- **Mức độ:** 🟢 Thấp
- **Khắc phục:** Luôn set `EnableDetailedErrors = false` trong production, sử dụng logging riêng cho debugging.

---

### A03 - Software Supply Chain Failures

**Mô tả rủi ro:** Dependency lỗi thời, thư viện có CVE đã biết, sử dụng package từ nguồn không đáng tin cậy.

#### Phát hiện:

**1. Frontend Dependencies cần cập nhật**
- **File:** `/workspace/frontend/package.json`
- **Phân tích:**
```json
{
  "dependencies": {
    "next": "15.0.3",
    "react": "^18.3.1",
    "axios": "^1.7.7",
    "next-auth": "^5.0.0-beta.25"
  }
}
```
- **Vấn đề:**
  - `axios@1.7.7` có phiên bản mới hơn (kiểm tra npm audit)
  - `next-auth@5.0.0-beta.25` đang dùng beta version, có thể có bugs chưa được fix
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:**
```bash
cd frontend
npm audit fix
npm update
# Cập nhật next-auth sang stable version khi available
```

**2. Thiếu package-lock.json/yarn.lock trong phân tích**
- **Vấn đề:** Không thể kiểm tra dependency tree chi tiết và các indirect dependencies.
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:** Commit lock files và sử dụng `npm audit` hoặc Snyk/GitHub Dependabot để tự động quét CVE.

**3. CDN Remote Patterns quá rộng**
- **File:** `/workspace/frontend/next.config.ts`
- **Đoạn code:**
```typescript
images: {
  remotePatterns: [
    {
      protocol: 'https',
      hostname: '**', // Cho phép TẤT CẢ hostname
    },
  ],
},
```
- **Vấn đề:** Cấu hình này cho phép tải ảnh từ bất kỳ domain nào, có thể dẫn đến loading malicious images.
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:**
```typescript
images: {
  remotePatterns: [
    {
      protocol: 'https',
      hostname: 'cdn.beautycommerce.com',
    },
    {
      protocol: 'https',
      hostname: 'storage.googleapis.com',
      port: '',
      pathname: '/my-bucket/**',
    },
  ],
},
```

---

### A04 - Cryptographic Failures

**Mô tả rủi ro:** Mã hóa yếu, truyền dữ liệu nhạy cảm không mã hóa, lưu mật khẩu plaintext, sử dụng thuật toán cryptographic lỗi thời.

#### 🚨 KHẨN CẤP - Các phát hiện nghiêm trọng:

**1. JWT Secret Key không đủ mạnh**
- **File:** `/workspace/src/Application/Settings/JwtSettings.cs`
- **Đoạn code:**
```csharp
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    // ...
}
```
- **File config:** `/workspace/appsettings.json`
```json
"JwtSettings": {
  "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"
}
```
- **Vấn đề:**
  - Secret key mặc định có thể bị brute force nếu không thay đổi
  - Không có rotation mechanism cho JWT keys
- **Mức độ:** 🔴 Nghiêm trọng
- **Khắc phục:**
  - Tạo random secret key tối thiểu 256-bit (32 bytes)
  - Implement key rotation strategy
```csharp
// Generate secure key
var secureKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)); // 512-bit
```

**2. Cookie Session Settings chưa đầy đủ**
- **File:** `/workspace/src/Api/Program.cs`
- **Vấn đề:** Không tìm thấy cấu hình cookie settings với `Secure`, `HttpOnly`, `SameSite` flags cho authentication cookies.
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:**
```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});
```

**3. Không có bằng chứng về TLS/SSL cho Database Connection**
- **File:** `/workspace/src/Api/appsettings.json`
- **Đoạn code:**
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=beauty_ecommerce;..."
```
- **Vấn đề:** Connection string không có `SSL Mode=Require` hoặc tương đương. Trong production, traffic database cần được mã hóa.
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:**
```json
"DefaultConnection": "Host=db.production.com;Port=5432;Database=beauty_ecommerce;SSL Mode=Require;..."
```

**4. Refresh Token không được mã hóa khi lưu**
- **File:** `/workspace/src/Application/Features/Auth/Commands/Login/LoginCommandHandler.cs`
- **Đoạn code:**
```csharp
private RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress, string? userAgent)
{
    var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    // Token được lưu trực tiếp vào database
    var refreshToken = RefreshToken.Create(userId, token, expiresAt, null, ipAddress, userAgent);
    _unitOfWork.RefreshTokens.Add(refreshToken);
    return refreshToken;
}
```
- **Vấn đề:** Refresh token được lưu plaintext trong database. Nếu database bị compromise, attacker có thể sử dụng lại tokens.
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:** Hash refresh token trước khi lưu (tương tự password):
```csharp
var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
refreshToken.TokenHash = Convert.ToBase64String(tokenHash);
```

---

### A05 - Injection

**Mô tả rủi ro:** SQL Injection, Command Injection, XSS (Cross-Site Scripting), SSTI (Server-Side Template Injection).

#### Phát hiện:

**1. Raw SQL Queries trong DbContext**
- **File:** `/workspace/src/Backend/ECommerce.Infrastructure/Persistence/ApplicationDbContext.cs`
- **Đoạn code:**
```csharp
// Line 78
await Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW CONCURRENTLY order_summaries", cancellationToken);

// Line 83-98
public IQueryable<OrderSummary> OrderSummaries =>
    Set<OrderSummary>().FromSqlRaw(@"
        SELECT
            o.id as OrderId,
            o.order_number as OrderNumber,
            // ...
        FROM orders o
        INNER JOIN customers c ON o.customer_id = c.id
        // ...
    ");
```
- **Vấn đề:** `FromSqlRaw` và `ExecuteSqlRawAsync` sử dụng raw SQL. Nếu có user input được concat vào queries này, sẽ dẫn đến SQL Injection. Hiện tại queries là static nên an toàn, nhưng cần cảnh giác khi mở rộng.
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:** Luôn sử dụng parameterized queries khi có user input:
```csharp
// GOOD - Parameterized
var orders = context.Orders
    .FromSqlRaw("SELECT * FROM orders WHERE customer_id = {0}", customerId)
    .ToList();

// BAD - String concatenation
var sql = $"SELECT * FROM orders WHERE customer_id = {customerId}";
```

**2. Raw SQL trong Outbox Repository**
- **File:** `/workspace/src/Infrastructure/Outbox/OutboxProcessor.cs` và `OutboxRepository.cs`
- **Đoạn code:**
```csharp
.FromSqlRaw(sql, parameters...)
```
- **Vấn đề:** Tương tự trên, cần đảm bảo `sql` và `parameters` được xử lý an toàn.
- **Mức độ:** 🟢 Thấp (nếu parameters được binding đúng cách)
- **Khắc phục:** Review kỹ code để đảm bảo không có string interpolation với user input.

**3. Không có XSS Protection trong Frontend**
- **File:** Next.js frontend
- **Phát hiện:** Không tìm thấy `dangerouslySetInnerHTML` trong codebase - đây là điểm tốt.
- **Tuy nhiên:** Cần đảm bảo tất cả user input được encode trước khi hiển thị.
- **Mức độ:** 🟢 Thấp
- **Khắc phục:** Tiếp tục tránh `dangerouslySetInnerHTML`, sử dụng React's built-in XSS protection.

---

### A06 - Insecure Design

**Mô tả rủi ro:** Thiếu rate limiting cho critical operations, logic xác thực yếu, thiếu kiểm soát business logic quan trọng.

#### Phát hiện:

**1. Rate Limiting chưa đủ chi tiết**
- **File:** `/workspace/src/Api/Program.cs`
- **Đoạn code:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```
- **Vấn đề:**
  - Rate limit global (100 requests/phút) áp dụng cho tất cả endpoints
  - Không có rate limit riêng cho sensitive endpoints như `/api/auth/login`, `/api/auth/register`
  - Attacker có thể brute force login với 100 attempts/phút
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:**
```csharp
// Thêm rate limiting riêng cho auth endpoints
app.MapPost("/api/v1/auth/login", LoginHandler)
   .RequireRateLimiting("auth");

// Configure
options.AddPolicy("auth", httpContext =>
    RateLimitPartition.GetSlidingWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5)
        }));
```

**2. Thiếu validation số lượng âm trong Cart**
- **File:** `/workspace/src/Application/Features/Cart/Commands/CartCommands.cs`
- **Vấn đề:** Không tìm thấy validation explicit cho `Quantity > 0` trong cart commands.
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:**
```csharp
RuleFor(x => x.Quantity)
    .GreaterThan(0).WithMessage("Quantity must be positive")
    .LessThanOrEqualTo(100).WithMessage("Quantity exceeds maximum allowed");
```

**3. Checkout không validate giá từ client**
- **File:** `/workspace/src/Application/Features/Cart/Handlers/CheckoutCommandHandler.cs`
- **Đoạn code:**
```csharp
Subtotal = cart.Subtotal,
DiscountAmount = cart.DiscountAmount,
Total = cart.Total,
```
- **Vấn đề:** Giá trị được lấy từ cart entity (đã tính toán trước đó). Cần đảm bảo cart calculation được thực hiện server-side với giá từ database, không trust client-submitted prices.
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:** Re-calculate total trong checkout handler:
```csharp
// Fetch fresh product prices from database
var products = await _productRepository.GetByIdsAsync(cart.Items.Select(i => i.ProductId));
var calculatedSubtotal = cart.Items.Sum(item =>
    products.First(p => p.Id == item.ProductId).Price * item.Quantity);

// So sánh với cart.Subtotal để detect tampering
if (Math.Abs(calculatedSubtotal - cart.Subtotal) > 0.01m)
{
    throw new InvalidOperationException("Cart total mismatch. Possible tampering detected.");
}
```

---

### A07 - Identification & Authentication Failures

**Mô tả rủi ro:** Session fixation, mật khẩu yếu, không có MFA, cookie không bảo mật, user enumeration.

#### Phát hiện:

**1. Password Policy đạt yêu cầu**
- **File:** `/workspace/src/Application/Commands/Auth/AuthCommands.cs`
- **Đoạn code:**
```csharp
RuleFor(x => x.Password)
    .NotEmpty().WithMessage("Password is required")
    .MinimumLength(8).WithMessage("Password must be at least 8 characters")
    .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)").WithMessage("Password must contain uppercase, lowercase and number");
```
- **Đánh giá:** ✅ Password policy đạt yêu cầu (8+ chars, uppercase, lowercase, number). Có thể bổ sung special characters.
- **Mức độ:** 🟢 Thấp

**2. Account Lockout Policy được implement**
- **File:** `/workspace/src/Domain/Entities/User.cs`
- **Đoạn code:**
```csharp
public void RecordFailedLogin()
{
    FailedLoginAttempts++;
    if (FailedLoginAttempts >= 5)
    {
        LockedUntil = DateTime.UtcNow.AddMinutes(15);
    }
}
```
- **Đánh giá:** ✅ Account lockout sau 5 lần thất bại, lock trong 15 phút. Đạt yêu cầu.
- **Mức độ:** 🟢 Thấp

**3. MFA được hỗ trợ nhưng chưa bắt buộc**
- **File:** `/workspace/src/Application/Features/Auth/Commands/Login/LoginCommandHandler.cs`
- **Đoạn code:**
```csharp
// Check MFA
if (user.MfaEnabled)
{
    if (string.IsNullOrEmpty(request.MfaCode))
    {
        return Result<AuthResultDto>.Failure("MFA code required", "MFA_REQUIRED");
    }
    // ... verify TOTP
}
```
- **Vấn đề:** MFA là optional. Đối với admin users, MFA nên là bắt buộc.
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:** Bắt buộc MFA cho admin role:
```csharp
if (user.Role == UserRole.Admin && !user.MfaEnabled)
{
    return Result<AuthResultDto>.Failure("MFA is required for admin accounts. Please enable MFA first.");
}
```

**4. User Enumeration qua thông báo lỗi**
- **File:** `/workspace/src/Application/Features/Auth/Commands/Login/LoginCommandHandler.cs`
- **Đoạn code:**
```csharp
var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
if (user == null)
{
    return Result<AuthResultDto>.Failure("Invalid email or password");
}
```
- **Đánh giá:** ✅ Thông báo lỗi giống nhau cho cả "wrong email" và "wrong password" - ngăn chặn user enumeration.
- **Mức độ:** 🟢 Thấp

---

### A08 - Software & Data Integrity Failures

**Mô tả rủi ro:** Deserialization không an toàn, thiếu chữ ký số cho dữ liệu quan trọng, software supply chain attacks.

#### Phát hiện:

**1. Không sử dụng BinaryFormatter (Good)**
- **Quét codebase:** Không tìm thấy `BinaryFormatter`, `unserialize()`, hoặc insecure deserialization patterns.
- **Đánh giá:** ✅ Codebase sử dụng `System.Text.Json` cho serialization - an toàn.
- **Mức độ:** 🟢 Thấp

**2. File Upload Validation chưa rõ ràng**
- **Quét codebase:** Không tìm thấy endpoint file upload rõ ràng trong code được review.
- **Khuyến nghị:** Nếu có file upload, cần:
  - Validate MIME type (không chỉ extension)
  - Scan virus
  - Lưu trữ ngoài web root
  - Đặt giới hạn kích thước
- **Mức độ:** 🟢 Thấp (chưa phát hiện vấn đề)

---

### A09 - Security Logging & Monitoring Failures

**Mô tả rủi ro:** Không ghi log các hành vi đáng ngờ, log chứa thông tin nhạy cảm, thiếu alerting cho security events.

#### Phát hiện:

**1. Audit Logging được implement tốt**
- **File:** `/workspace/src/Backend/ECommerce.Infrastructure/Persistence/Interceptors/AuditLogInterceptor.cs`
- **Đoạn code:**
```csharp
var sensitiveFields = new[] { "password", "secret", "token", "creditcard", "ssn", "mfa" };
// Mask sensitive data before storing in audit logs
```
- **Đánh giá:** ✅ Audit logging được implement với masking cho sensitive fields.
- **Mức độ:** 🟢 Thấp

**2. Thiếu logging cho failed login attempts với IP tracking**
- **File:** `/workspace/src/Application/Features/Auth/Commands/Login/LoginCommandHandler.cs`
- **Vấn đề:** Failed login được record trong User entity nhưng không có log riêng để monitoring/alerting.
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:**
```csharp
_logger.LogWarning("Failed login attempt for email {Email} from IP {IpAddress}. Attempts: {Attempts}",
    request.Email, request.IpAddress, user.FailedLoginAttempts);

// Gửi alert nếu có nhiều failed attempts từ cùng IP
if (user.FailedLoginAttempts >= 3)
{
    await _alertService.SendSecurityAlertAsync(new SecurityAlert
    {
        Type = "BruteForceAttempt",
        Email = request.Email,
        IpAddress = request.IpAddress,
        Timestamp = DateTime.UtcNow
    });
}
```

**3. Payment/Checkout logging chưa đầy đủ**
- **File:** `/workspace/src/Application/Features/Cart/Handlers/CheckoutCommandHandler.cs`
- **Đoạn code:**
```csharp
_logger.LogInformation("Checkout successful for user {UserId}. Order {OrderNumber} created",
    request.UserId, order.OrderNumber);
```
- **Vấn đề:** Log thành công nhưng thiếu log cho failed checkout attempts với lý do cụ thể.
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:** Thêm detailed logging cho fraud detection.

---

### A10 - Mishandling of Exceptional Conditions

**Mô tả rủi ro:** Lộ stack trace, thông báo lỗi quá chi tiết, không xử lý ngoại lệ an toàn.

#### Phát hiện:

**1. Global Exception Handling Middleware được implement**
- **File:** `/workspace/src/Api/Middleware/IdempotencyMiddleware.cs`
- **Đoạn code:**
```csharp
public class GlobalExceptionHandlingMiddleware
{
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
        // ...
        var (statusCode, error) = exception switch
        {
            ArgumentException ae => (StatusCodes.Status400BadRequest, ae.Message),
            KeyNotFoundException knfe => (StatusCodes.Status404NotFound, knfe.Message),
            // ...
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        var response = new
        {
            error = error,
            traceId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow
        };
    }
}
```
- **Vấn đề:**
  - Với `ArgumentException` và `KeyNotFoundException`, nguyên văn message được trả về client - có thể lộ thông tin nội bộ.
  - Stack trace không bị lộ (good).
- **Mức độ:** 🟡 Trung bình
- **Khắc phục:**
```csharp
var (statusCode, error) = exception switch
{
    ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request parameters"),
    KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
    UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
    _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
};
```

**2. Connection String chứa "Include Error Detail=true"**
- **File:** `/workspace/src/Api/appsettings.json`
- **Đoạn code:**
```json
"DefaultConnection": "...;Include Error Detail=true"
```
- **Vấn đề:** PostgreSQL sẽ trả về chi tiết lỗi database, có thể lộ thông tin schema.
- **Mức độ:** 🔴 Nghiêm trọng (đã nêu ở A02)
- **Khắc phục:** Xóa `Include Error Detail=true` khỏi connection string production.

---

## Top 5 Vấn Đề Ưu Tiên Khắc Phục Ngay (P0)

| # | Vấn đề | Mức độ | File liên quan | Khuyến nghị khắc phục |
|---|--------|--------|----------------|----------------------|
| 1 | **Hardcoded Database Password** | 🔴 Critical | `/src/Api/appsettings.json` | Di chuyển secrets ra environment variables hoặc secrets manager |
| 2 | **Hardcoded JWT Secret Key** | 🔴 Critical | `/src/Api/appsettings.json` | Sử dụng environment variable, implement key rotation |
| 3 | **Default Hangfire Credentials** | 🔴 Critical | `/appsettings.json` | Thay đổi default credentials, sử dụng ASP.NET Identity authorization |
| 4 | **Connection String với Error Detail** | 🔴 High | `/src/Api/appsettings.json` | Xóa `Include Error Detail=true` trong production |
| 5 | **Thiếu Security Headers** | 🟡 High | `/src/Api/Program.cs` | Thêm CSP, HSTS, X-Frame-Options, X-Content-Type-Options |

---

## Khuyến Nghị Tổng Thể

### Ngắn hạn (1-2 tuần)
1. ✅ Di chuyển tất cả secrets (database passwords, JWT keys, API keys) ra environment variables
2. ✅ Thêm security headers middleware
3. ✅ Implement rate limiting riêng cho auth endpoints (5 attempts/5 phút)
4. ✅ Xóa `Include Error Detail=true` khỏi production connection strings
5. ✅ Thay đổi default Hangfire credentials

### Trung hạn (1-2 tháng)
1. ✅ Implement proper authorization policies với MFA requirement cho admin
2. ✅ Thêm logging và alerting cho security events (failed logins, checkout failures)
3. ✅ Restrict image remote patterns trong Next.js config
4. ✅ Implement refresh token hashing
5. ✅ Thêm validation cho cart quantities và price recalculation trong checkout

### Dài hạn (3-6 tháng)
1. ✅ Implement key rotation strategy cho JWT
2. ✅ Tích hợp vulnerability scanning vào CI/CD pipeline (Snyk, Dependabot)
3. ✅ Thực hiện penetration testing định kỳ
4. ✅ Xây dựng security incident response plan
5. ✅ Đào tạo security awareness cho development team

---

## Kết Luận

Dự án **my-pham-online** đã có nền tảng bảo mật khá tốt với nhiều thực hành đúng đắn:
- ✅ Password hashing bằng ASP.NET Core Identity (bcrypt)
- ✅ Account lockout policy
- ✅ MFA support (TOTP)
- ✅ Audit logging với data masking
- ✅ Global exception handling
- ✅ Rate limiting cơ bản

Tuy nhiên, vẫn còn **các lỗ hổng nghiêm trọng cần khắc phục ngay**, đặc biệt là việc **hardcoded secrets trong file config** và **thiếu security headers**. Ưu tiên cao nhất là di chuyển tất cả secrets ra môi trường biến hoặc secrets management solution trước khi deploy production.

**Điểm bảo mật tổng thể:** 6.5/10
**Trạng thái:** ⚠️ **CẦN CẢI THIỆN TRƯỚC KHI PRODUCTION**

---

*Báo cáo được tạo dựa trên OWASP Top 10:2025 framework.*
