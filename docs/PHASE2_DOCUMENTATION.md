# Phase 2: Thương mại cốt lõi - Documentation

## Tổng quan

Phase 2 triển khai các module thương mại điện tử cốt lõi với stack .NET 8, PostgreSQL, Redis, Elasticsearch và BullMQ (thông qua Hangfire).

## 1. Module Đơn hàng với 17 trạng thái

### Các trạng thái đơn hàng:

| STT | Status | Mô tả |
|-----|--------|-------|
| 0 | Pending | Đơn hàng chờ xử lý |
| 1 | PaymentPending | Chờ thanh toán |
| 2 | PaymentProcessing | Đang xử lý thanh toán |
| 3 | Paid | Đã thanh toán |
| 4 | Confirmed | Đã xác nhận |
| 5 | Processing | Đang chuẩn bị hàng |
| 6 | Packing | Đang đóng gói |
| 7 | Shipped | Đã giao cho đơn vị vận chuyển |
| 8 | Delivering | Đang giao hàng |
| 9 | Delivered | Đã giao thành công |
| 10 | Completed | Hoàn thành |
| 11 | Cancelled | Đã hủy |
| 12 | RefundRequested | Yêu cầu hoàn tiền |
| 13 | RefundProcessing | Đang xử lý hoàn tiền |
| 14 | Refunded | Đã hoàn tiền |
| 15 | Failed | Thất bại |
| 16 | Expired | Hết hạn |

### API Endpoints:

```
POST   /api/orders              - Tạo đơn hàng mới
GET    /api/orders              - Lấy danh sách đơn hàng của user
GET    /api/orders/{id}         - Lấy chi tiết đơn hàng
PATCH  /api/orders/{id}/status  - Cập nhật trạng thái (Admin)
POST   /api/orders/apply-voucher - Áp dụng voucher
GET    /api/orders/{id}/payment-link - Lấy link thanh toán

GET    /api/admin/orders        - Admin: Danh sách đơn hàng với filter
PATCH  /api/admin/orders/{id}/status - Admin: Cập nhật trạng thái
```

## 2. Tích hợp Thanh toán

### Stripe Configuration:

```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_..."
  }
}
```

**Webhook Endpoint:** `POST /api/webhooks/stripe`

Events cần handle:
- `payment_intent.succeeded` → Chuyển order status sang "Paid"
- `payment_intent.payment_failed` → Chuyển order status sang "Failed"
- `charge.refunded` → Xử lý hoàn tiền

### PayOS Configuration (Vietnam):

```json
{
  "PayOS": {
    "ClientId": "...",
    "ApiKey": "...",
    "ChecksumKey": "..."
  }
}
```

**Webhook Endpoint:** `POST /api/webhooks/payos`

Payload received:
```json
{
  "orderCode": "1234567890",
  "amount": 100000,
  "status": "PAID",
  "transactionId": "abc123"
}
```

## 3. Tích hợp Vận chuyển (GHN)

### GHN Configuration:

```json
{
  "GHN": {
    "ShopId": "12345",
    "Token": "your-token-here"
  },
  "Store": {
    "Name": "Beauty Ecommerce",
    "Phone": "0901234567",
    "Address": "Hà Nội",
    "City": "Hà Nội"
  }
}
```

**Webhook Endpoint:** `POST /api/webhooks/ghn`

GHN sẽ gửi webhook khi:
- Đơn được pick up
- Đang giao hàng
- Giao thành công
- Giao thất bại

## 4. Order Saga Pattern với Compensation

### Luồng Saga chính:

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│ ReserveInventory│────▶│ ProcessPayment   │────▶│ CreateShipment  │────▶│ ConfirmOrder    │
└─────────────────┘     └──────────────────┘     └─────────────────┘     └─────────────────┘
        │                       │                        │                       │
        ▼                       ▼                        ▼                       ▼
   Keep stock             Charge card            Create GHN order        Mark as confirmed
   reserved               or create intent       and get tracking
```

### Compensation (Rollback) khi thất bại:

```
If CreateShipment fails:
  ┌──────────────────┐     ┌─────────────────┐     ┌──────────────────┐
  │ RefundPayment    │◀────│ ReleaseInventory│◀────│ CancelShipment   │
  └──────────────────┘     └─────────────────┘     └──────────────────┘
         │                        │                        │
         ▼                        ▼                        ▼
   Refund via              Decrement              Cancel with GHN
   Stripe/PayOS            reserved qty
```

### Implementation với Hangfire (thay thế BullMQ):

```csharp
// Program.cs configuration
builder.Services.AddHangfire(config => config
    .UseRedisStorage(redisConfig, new RedisStorageOptions
    {
        Db = 1,
        Prefix = "hf:",
        FetchTimeout = TimeSpan.FromMinutes(5)
    })
    .UseFilter(new AutomaticRetryAttribute { Attempts = 3 }));

builder.Services.AddHangfireServer();
```

Saga workers được đăng ký trong DI container và thực thi bất đồng bộ.

## 5. Module Voucher

### Model Voucher:

```csharp
public class Voucher
{
    public string Code { get; set; }           // Mã voucher (unique)
    public string Name { get; set; }           // Tên hiển thị
    public VoucherType Type { get; set; }      // Percentage/FixedAmount/FreeShipping
    public decimal Value { get; set; }         // Giá trị discount
    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int TotalUsageLimit { get; set; }
    public int UsageCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsPublic { get; set; }
    public string? ApplicableProducts { get; set; }  // JSON array
    public string? ApplicableCategories { get; set; } // JSON array
}
```

### API Endpoints:

```
POST   /api/vouchers              - Tạo voucher (Admin)
GET    /api/vouchers              - Danh sách voucher
GET    /api/vouchers/{id}         - Chi tiết voucher
PUT    /api/vouchers/{id}         - Cập nhật voucher (Admin)
DELETE /api/vouchers/{id}         - Xóa voucher (Admin)
POST   /api/orders/apply-voucher  - Áp dụng vào order
```

## Postman Collection

### Import vào Postman:

```json
{
  "info": {
    "name": "Beauty Ecommerce Phase 2",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Orders",
      "item": [
        {
          "name": "Create Order",
          "request": {
            "method": "POST",
            "header": [{ "key": "Authorization", "value": "Bearer {{token}}" }],
            "url": "{{baseUrl}}/api/orders",
            "body": {
              "mode": "raw",
              "raw": "{\n  \"customerNote\": \"Giao giờ hành chính\",\n  \"shippingAddressLine1\": \"123 Đường ABC\",\n  \"shippingWard\": \"Phường XYZ\",\n  \"shippingDistrict\": \"Quận 1\",\n  \"shippingCity\": \"Hồ Chí Minh\",\n  \"paymentMethod\": \"COD\",\n  \"voucherCode\": \"SAVE10\"\n}"
            }
          }
        },
        {
          "name": "Get My Orders",
          "request": {
            "method": "GET",
            "header": [{ "key": "Authorization", "value": "Bearer {{token}}" }],
            "url": "{{baseUrl}}/api/orders?page=1&pageSize=10"
          }
        },
        {
          "name": "Get Order Detail",
          "request": {
            "method": "GET",
            "header": [{ "key": "Authorization", "value": "Bearer {{token}}" }],
            "url": "{{baseUrl}}/api/orders/{{orderId}}"
          }
        },
        {
          "name": "Apply Voucher",
          "request": {
            "method": "POST",
            "header": [{ "key": "Authorization", "value": "Bearer {{token}}" }],
            "url": "{{baseUrl}}/api/orders/apply-voucher",
            "body": {
              "mode": "raw",
              "raw": "{\n  \"orderId\": \"{{orderId}}\",\n  \"voucherCode\": \"SAVE10\"\n}"
            }
          }
        },
        {
          "name": "Get Payment Link",
          "request": {
            "method": "GET",
            "header": [{ "key": "Authorization", "value": "Bearer {{token}}" }],
            "url": "{{baseUrl}}/api/orders/{{orderId}}/payment-link"
          }
        }
      ]
    },
    {
      "name": "Admin Orders",
      "item": [
        {
          "name": "Get All Orders",
          "request": {
            "method": "GET",
            "header": [{ "key": "Authorization", "value": "Bearer {{adminToken}}" }],
            "url": "{{baseUrl}}/api/admin/orders?status=Pending&page=1"
          }
        },
        {
          "name": "Update Order Status",
          "request": {
            "method": "PATCH",
            "header": [{ "key": "Authorization", "value": "Bearer {{adminToken}}" }],
            "url": "{{baseUrl}}/api/admin/orders/{{orderId}}/status",
            "body": {
              "mode": "raw",
              "raw": "{\n  \"status\": \"Confirmed\"\n}"
            }
          }
        }
      ]
    },
    {
      "name": "Vouchers",
      "item": [
        {
          "name": "Create Voucher",
          "request": {
            "method": "POST",
            "header": [{ "key": "Authorization", "value": "Bearer {{adminToken}}" }],
            "url": "{{baseUrl}}/api/vouchers",
            "body": {
              "mode": "raw",
              "raw": "{\n  \"code\": \"SUMMER2024\",\n  \"name\": \"Khuyến mãi hè\",\n  \"type\": \"Percentage\",\n  \"value\": 10,\n  \"maxDiscountAmount\": 100000,\n  \"minOrderAmount\": 500000,\n  \"totalUsageLimit\": 1000,\n  \"startDate\": \"2024-06-01T00:00:00Z\",\n  \"endDate\": \"2024-08-31T23:59:59Z\",\n  \"isPublic\": true\n}"
            }
          }
        }
      ]
    }
  ],
  "variable": [
    { "key": "baseUrl", "value": "http://localhost:5000" },
    { "key": "token", "value": "" },
    { "key": "adminToken", "value": "" },
    { "key": "orderId", "value": "" }
  ]
}
```

## Setup & Configuration

### Environment Variables:

```bash
# Database
ConnectionStrings__DefaultConnection="Host=localhost;Database=beauty_ecommerce;Username=postgres;Password=postgres"

# Redis
ConnectionStrings__Redis="localhost:6379"

# JWT
Jwt__Issuer="BeautyEcommerce"
Jwt__Audience="BeautyEcommerceUsers"
Jwt__Key="YourSuperSecretKeyThatIsAtLeast32CharactersLong!"

# Stripe
Stripe__SecretKey="sk_test_..."
Stripe__WebhookSecret="whsec_..."

# PayOS
PayOS__ClientId="..."
PayOS__ApiKey="..."
PayOS__ChecksumKey="..."

# GHN
GHN__ShopId="12345"
GHN__Token="your-token"

# Frontend URL (for payment redirect)
FrontendUrl="http://localhost:3000"
```

### Running the Application:

```bash
# Build
dotnet build

# Run migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Api

# Run API
dotnet run --project src/Api
```

## Testing với Sandbox

### Stripe Test Cards:
- Success: `4242 4242 4242 4242`
- Decline: `4000 0000 0000 0002`

### PayOS Sandbox:
Sử dụng credentials từ https://payos.vn/docs khi ở chế độ sandbox.

### GHN Sandbox:
Sử dụng endpoint `https://dev-online-gateway.ghn.vn` với token test.
