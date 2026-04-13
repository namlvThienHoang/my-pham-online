# Beauty E-commerce Platform

Nền tảng thương mại điện tử chuyên nghiệp cho mỹ phẩm, được xây dựng theo mô hình **Modular Monolith** với kiến trúc Clean Architecture. Hệ thống hỗ trợ đầy đủ các nghiệp vụ: bán hàng online, quản lý kho (đặc thù hạn dùng, lô sản xuất), CRM, marketing, và báo cáo.

## 📋 Mục lục

- [Tính năng nổi bật](#-tính-năng-nổi-bật)
- [Tech Stack](#-tech-stack)
- [Kiến trúc hệ thống](#-kiến-trúc-hệ-thống)
- [Cấu trúc dự án](#-cấu-trúc-dự-án)
- [Các module nghiệp vụ](#-các-module-nghiệp-vụ)
- [Hướng dẫn cài đặt](#-hướng-dẫn-cài-đặt)
- [API Documentation](#-api-documentation)
- [Testing](#-testing)
- [Deployment](#-deployment)
- [Monitoring & Logging](#-monitoring--logging)

## ✨ Tính năng nổi bật

### Phân hệ Khách hàng (E-commerce)
- ✅ Xem danh mục sản phẩm theo loại da, công dụng, thương hiệu, thành phần
- ✅ Tìm kiếm & lọc nâng cao (full-text search với Elasticsearch)
- ✅ Giỏ hàng, thanh toán đa phương thức (COD, Stripe, PayOS)
- ✅ Quản lý tài khoản, lịch sử đơn hàng, địa chỉ giao hàng
- ✅ Đánh giá & xếp hạng sản phẩm (có kiểm duyệt)
- ✅ Wishlist & Recently Viewed
- ✅ Ví điện tử & Gift Cards
- ✅ Theo dõi đơn hàng realtime (SignalR)
- ✅ Chương trình tích điểm, thành viên (Đồng/Bạc/Vàng/Kim cương)

### Phân hệ Quản trị (Admin/Staff)
- ✅ Quản lý sản phẩm, biến thể, danh mục, thương hiệu
- ✅ Quản lý kho theo lô (batch/lot): ngày sản xuất, hạn dùng, FEFO
- ✅ Quản lý đơn hàng với 17 trạng thái
- ✅ CRM: Hồ sơ khách hàng, phân hạng, lịch sử mua hàng
- ✅ Marketing: Mã giảm giá, combo, chương trình khuyến mãi
- ✅ Xử lý đổi trả, hoàn tiền (Return Management)
- ✅ Báo cáo & thống kê doanh thu, tồn kho, khách hàng

## 🛠 Tech Stack

### Backend
| Component | Technology |
|-----------|------------|
| Framework | .NET 8, ASP.NET Core Web API, Minimal API |
| Architecture | Clean Architecture, Modular Monolith |
| Database ORM | Entity Framework Core 8 |
| Primary DB | PostgreSQL 16 (với Read Replica) |
| Caching | Redis (StackExchange.Redis) |
| Search Engine | Elasticsearch 8.x |
| Message Queue | Hangfire (Redis storage) + Outbox Pattern |
| Orchestration | Saga Pattern (OrderSaga) + Compensating Transactions |
| Resilience | Polly (Retry, Circuit Breaker, Timeout, Fallback) |
| Real-time | SignalR + Redis Backplane |
| Authentication | JWT + Refresh Token Family, MFA (TOTP) |

### Frontend
| Component | Technology |
|-----------|------------|
| Framework | Next.js 15 (App Router) |
| Language | TypeScript 5.7+ |
| Styling | Tailwind CSS 3.4 |
| UI Components | shadcn/ui, Radix UI |
| State Management | Zustand, TanStack Query |
| Forms | React Hook Form + Zod |
| Auth | NextAuth.js v5 |
| Testing | Vitest, React Testing Library |

### Infrastructure & DevOps
| Component | Technology |
|-----------|------------|
| Containerization | Docker, Docker Compose |
| Orchestration | Kubernetes (manifests included) |
| CI/CD | GitHub Actions |
| Logging | Serilog + Seq |
| Load Testing | k6 |
| Database Admin | Adminer |

## 🏗 Kiến trúc hệ thống

```
┌─────────────────────────────────────────────────────────────┐
│                        Client Layer                          │
│  ┌─────────────────┐         ┌─────────────────────────┐   │
│  │   Next.js App   │         │   Admin Dashboard       │   │
│  │   (Port 3000)   │         │   (Internal)            │   │
│  └────────┬────────┘         └────────────┬────────────┘   │
└───────────┼───────────────────────────────┼────────────────┘
            │                               │
            ▼                               ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway / Load Balancer               │
│                         (Port 5000/5001)                     │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   ASP.NET Core Web API                       │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                  Clean Architecture                   │   │
│  │  ┌─────────────────────────────────────────────────┐ │   │
│  │  │              Domain Layer                        │ │   │
│  │  │  Entities, Value Objects, Domain Events          │ │   │
│  │  └─────────────────────────────────────────────────┘ │   │
│  │  ┌─────────────────────────────────────────────────┐ │   │
│  │  │           Application Layer                      │ │   │
│  │  │  Commands/Queries (MediatR), DTOs, Validators    │ │   │
│  │  └─────────────────────────────────────────────────┘ │   │
│  │  ┌─────────────────────────────────────────────────┐ │   │
│  │  │         Infrastructure Layer                     │ │   │
│  │  │  EF Core, Redis, Elasticsearch, Polly, Hangfire  │ │   │
│  │  └─────────────────────────────────────────────────┘ │   │
│  │  ┌─────────────────────────────────────────────────┐ │   │
│  │  │             API Layer                            │ │   │
│  │  │  Controllers, Middleware, SignalR Hubs           │ │   │
│  │  └─────────────────────────────────────────────────┘ │   │
│  └──────────────────────────────────────────────────────┘   │
└───────────────────────────┬─────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
┌───────────────┐  ┌───────────────┐  ┌─────────────────┐
│  PostgreSQL   │  │    Redis      │  │  Elasticsearch  │
│  (Primary +   │  │  (Cache +     │  │  (Product       │
│   Replica)    │  │   Hangfire)   │  │   Search)       │
└───────────────┘  └───────────────┘  └─────────────────┘
```

## 📁 Cấu trúc dự án

```
/workspace
├── src/
│   ├── Domain/                 # Entities, Value Objects, Domain Events
│   ├── Application/            # Commands/Queries, MediatR, DTOs, Validators
│   ├── Infrastructure/         # EF Core, Redis, Elasticsearch, Polly, Hangfire
│   ├── Api/                    # Controllers, Middleware, SignalR Hubs
│   ├── Backend/                # Legacy backend modules
│   │   ├── ECommerce.Core/     # Core entities & interfaces
│   │   ├── ECommerce.Infrastructure/
│   │   ├── ECommerce.Modules/  # Admin, Audit modules
│   │   ├── Modules/            # Feature modules
│   │   │   ├── Reviews/        # Review & Rating system
│   │   │   ├── Wishlist/       # Wishlist & Recently Viewed
│   │   │   ├── Wallet/         # E-wallet & Gift Cards
│   │   │   ├── Returns/        # Return Management
│   │   │   └── Notifications/  # Notification system
│   │   └── Shared/             # Shared helpers, events, interfaces
│   └── Frontend/               # Next.js app (legacy structure)
├── frontend/                   # Next.js 15 application (main)
│   ├── app/                    # App Router pages
│   ├── components/             # Reusable components
│   └── lib/                    # Utilities, API clients
├── tests/
│   ├── Unit/                   # Unit tests (xUnit)
│   ├── Integration/            # Integration tests (Testcontainers)
│   └── k6/                     # Load testing scripts
├── k8s/                        # Kubernetes manifests
│   └── staging/                # Staging environment configs
├── docs/                       # Documentation
│   ├── spec.md                 # Full specification (Vietnamese)
│   └── PHASE2_DOCUMENTATION.md # Phase 2 implementation guide
├── scripts/
│   └── init-db.sql             # Database initialization script
├── .github/workflows/          # CI/CD pipelines
├── docker-compose.yml          # Local development services
├── Dockerfile                  # Production Docker image
└── appsettings.json            # Application configuration
```

## 🧩 Các module nghiệp vụ

### 1. Authentication & Authorization
- JWT + Refresh Token với rotation
- MFA (TOTP) support
- Account lockout sau nhiều lần thử sai
- Token reuse detection

**Endpoints:**
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/auth/register` | Đăng ký user mới |
| POST | `/api/v1/auth/login` | Đăng nhập |
| POST | `/api/v1/auth/refresh-token` | Làm mới access token |
| POST | `/api/v1/auth/enable-mfa` | Kích hoạt MFA |
| POST | `/api/v1/auth/verify-mfa` | Xác thực MFA code |
| POST | `/api/v1/auth/logout` | Đăng xuất |
| GET | `/api/v1/auth/me` | Lấy thông tin user hiện tại |

### 2. Product Management
- Quản lý sản phẩm, biến thể (dung tích, dạng bào chế)
- Danh mục, thương hiệu, tags (thành phần, công dụng)
- Full-text search với Elasticsearch
- Tag-based cache invalidation

### 3. Order Management (17 trạng thái)
| Status | Mô tả |
|--------|-------|
| Pending | Chờ xử lý |
| PaymentPending | Chờ thanh toán |
| PaymentProcessing | Đang xử lý thanh toán |
| Paid | Đã thanh toán |
| Confirmed | Đã xác nhận |
| Processing | Đang chuẩn bị hàng |
| Packing | Đang đóng gói |
| Shipped | Đã giao cho vận chuyển |
| Delivering | Đang giao hàng |
| Delivered | Đã giao thành công |
| Completed | Hoàn thành |
| Cancelled | Đã hủy |
| RefundRequested | Yêu cầu hoàn tiền |
| RefundProcessing | Đang xử lý hoàn tiền |
| Refunded | Đã hoàn tiền |
| Failed | Thất bại |
| Expired | Hết hạn |

### 4. Payment Integration
- **Stripe**: Credit/Debit cards, Apple Pay, Google Pay
- **PayOS**: Vietnamese local payment gateway
- Webhook handling cho async payment updates
- Idempotency protection

### 5. Shipping (GHN - Giao Hàng Nhanh)
- Tự động tính phí ship
- Tạo đơn hàng với GHN API
- Tracking realtime status

### 6. Inventory Management
- Nhập/xuất kho theo lô (batch/lot)
- FEFO (First Expired First Out)
- Cảnh báo hàng sắp hết hạn
- Auto-lock khi hết hạn

### 7. Reviews & Ratings
- Chỉ review khi order đã DELIVERED
- Moderation workflow (pending → approved/rejected)
- Auto-update average rating
- Helpful votes

### 8. Wishlist & Recently Viewed
- Unlimited wishlist items
- Recently viewed (giới hạn 20/sp/user)
- Cursor pagination

### 9. E-wallet & Gift Cards
- Wallet balance management
- Transaction history (earn, spend, refund, expire)
- Gift card với SHA256 hashed codes
- Idempotent redemption

### 10. Return Management
- Yêu cầu đổi/trả với lý do
- Workflow: Pending → Approved → Received → Refunded/Rejected
- Auto-create refund transaction

### 11. Notifications
- Real-time notifications qua SignalR
- Email/SMS integration (planned)
- Notification preferences

## 🚀 Hướng dẫn cài đặt

### Prerequisites

- Docker & Docker Compose
- .NET 8 SDK
- Node.js 20+
- Git

### Quick Start

#### 1. Clone repository

```bash
git clone <repository-url>
cd workspace
```

#### 2. Start infrastructure services

```bash
docker-compose up -d
```

Services sẽ bao gồm:
- PostgreSQL (port 5432)
- Redis (port 6379)
- Elasticsearch (port 9200)
- Seq (port 5341)
- Adminer (port 8080)

#### 3. Run database migrations

```bash
cd src/Api
dotnet ef database update
```

Hoặc sử dụng script khởi tạo:

```bash
docker exec -i beauty_postgres psql -U postgres -d beauty_ecommerce < scripts/init-db.sql
```

#### 4. Start the API

```bash
# From root directory
cd src/Api
dotnet run
```

API sẽ chạy tại: `http://localhost:5000` (HTTP) và `http://localhost:5001` (HTTPS)

#### 5. Start the frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend sẽ chạy tại: `http://localhost:3000`

### Build Docker Image

```bash
docker-compose build api
```

### Health Checks

- API: `http://localhost:5000/health/live`
- PostgreSQL: `docker exec beauty_postgres pg_isready -U postgres`
- Redis: `docker exec beauty_redis redis-cli ping`
- Elasticsearch: `curl http://localhost:9200/_cluster/health`

## 📚 API Documentation

### Base URL
- Development: `http://localhost:5000/api/v1`
- Production: `https://your-domain.com/api/v1`

### Authentication

Hầu hết endpoints yêu cầu JWT token trong header:

```
Authorization: Bearer <your-jwt-token>
```

### Key Endpoints

#### Orders
```
POST   /api/orders                    # Tạo đơn hàng mới
GET    /api/orders                    # Danh sách đơn của user
GET    /api/orders/{id}               # Chi tiết đơn hàng
PATCH  /api/orders/{id}/status        # Cập nhật trạng thái (Admin)
POST   /api/orders/apply-voucher      # Áp dụng voucher
GET    /api/orders/{id}/payment-link  # Lấy link thanh toán
```

#### Products
```
GET    /api/products                  # Danh sách sản phẩm (search, filter)
GET    /api/products/{id}             # Chi tiết sản phẩm
GET    /api/products/search?q=        # Full-text search
```

#### Reviews
```
POST   /api/reviews                   # Tạo review
GET    /api/reviews/product/{id}      # Reviews của sản phẩm
POST   /api/reviews/{id}/helpful      # Vote helpful
```

#### Wallet & Gift Cards
```
GET    /api/wallet/balance            # Số dư ví
GET    /api/wallet/transactions       # Lịch sử giao dịch
POST   /api/gift-cards                # Mua gift card
POST   /api/gift-cards/redeem         # Đổi gift card
```

#### Wishlist
```
GET    /api/wishlist                  # Danh sách yêu thích
POST   /api/wishlist                  # Thêm vào wishlist
DELETE /api/wishlist/{productId}      # Xóa khỏi wishlist
GET    /api/wishlist/recently-viewed  # Sản phẩm đã xem
```

📄 **Postman Collection**: `tests/Phase2.postman_collection.json`

## 🧪 Testing

### Unit Tests

```bash
dotnet test --filter "Category=Unit"
```

### Integration Tests

Yêu cầu Docker đang chạy (sử dụng Testcontainers):

```bash
dotnet test --filter "Category=Integration"
```

### Load Testing (k6)

```bash
cd tests/k6
k6 run smoke.js
```

### Frontend Tests

```bash
cd frontend
npm test
npm run test:ui  # Với giao diện
```

## 🚢 Deployment

### Docker Compose (Production)

```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Kubernetes

```bash
# Deploy to staging
kubectl apply -f k8s/staging/

# Deploy to production
kubectl apply -f k8s/production/
```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | - |
| `ConnectionStrings__Redis` | Redis connection string | - |
| `ConnectionStrings__Elasticsearch` | Elasticsearch URI | - |
| `Jwt__Key` | JWT signing key (32+ chars) | - |
| `Jwt__Issuer` | JWT issuer | `BeautyCommerce` |
| `Jwt__Audience` | JWT audience | `BeautyCommerceUsers` |
| `Seq__Url` | Seq logging URL | - |

## 📊 Monitoring & Logging

### Logging Stack
- **Serilog**: Structured logging
- **Seq**: Log aggregation & querying (port 5341)

Access Seq dashboard at: `http://localhost:5341`

### Health Checks

API cung cấp các endpoints health check:

- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe

### Metrics (Planned)
- Prometheus metrics endpoint
- Grafana dashboards

## 🔐 Security Features

- Password hashing (BCrypt)
- JWT with refresh token rotation
- MFA (TOTP) support
- Account lockout after failed attempts
- SQL Injection prevention (EF Core parameterized queries)
- XSS protection
- CSRF protection
- Rate limiting (planned)
- PCI DSS compliance for payments

## 📈 Performance Optimizations

- Redis caching với tag-based invalidation
- Database read replicas
- Elasticsearch for product search
- Response compression
- Lazy loading disabled
- Pagination cho tất cả list endpoints
- Cursor-based pagination cho infinite scroll

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

## 📄 License

Private - All rights reserved

## 📞 Support

For issues and questions, please create an issue in the repository or contact the development team.

---

**Version**: 3.0  
**Last Updated**: 2024  
**Maintained by**: Beauty Commerce Team
