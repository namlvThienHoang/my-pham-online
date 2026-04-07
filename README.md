# Beauty E-Commerce System

## Overview

Production-ready e-commerce platform for cosmetics built with:
- **Backend**: .NET 8, ASP.NET Core Web API, Clean Architecture
- **Frontend**: Next.js 15 (App Router), TypeScript, Tailwind CSS, shadcn/ui
- **Database**: PostgreSQL 16 with read replica support
- **Cache**: Redis
- **Search**: Elasticsearch 8.x
- **Message Queue**: Hangfire + Outbox Pattern
- **Real-time**: SignalR with Redis backplane

## Quick Start

### Prerequisites
- Docker & Docker Compose
- Node.js 20+
- .NET 8 SDK

### Development Setup

1. **Start infrastructure**
```bash
docker-compose up -d postgres redis elasticsearch seq adminer
```

2. **Run Backend**
```bash
cd src/Api
dotnet restore
dotnet ef database update
dotnet run
```

3. **Run Frontend**
```bash
cd frontend
npm install
npm run dev
```

4. **Access Services**
- Frontend: http://localhost:3000
- API: http://localhost:5000
- Adminer: http://localhost:8082
- Seq Logs: http://localhost:8081
- Elasticsearch: http://localhost:9200

### Full Docker Setup

```bash
docker-compose up -d
```

## Architecture

### Clean Architecture Layers

```
src/
├── Domain/           # Entities, Value Objects, Domain Events
├── Application/      # CQRS, MediatR, DTOs, Validators
├── Infrastructure/   # EF Core, Redis, Elasticsearch, External APIs
└── Api/             # Controllers, Middleware, Hubs
```

### Key Patterns Implemented

- **Outbox Pattern**: Reliable event publishing with lease-based distributed locking
- **Saga Orchestrator**: Order processing with compensating transactions
- **CQRS**: Command (EF Core) / Query (Dapper) separation
- **Soft Delete**: Global query filters with `deleted_at` column
- **Row Versioning**: Optimistic concurrency with `row_version`
- **Idempotency**: X-Idempotency-Key header for all write operations
- **Circuit Breaker**: Polly policies for external API calls
- **Cursor Pagination**: Efficient pagination without OFFSET

## Database Schema

### Core Tables
- `users`, `user_addresses`, `skin_profiles`
- `products`, `product_translations`, `categories`, `brands`
- `inventory_lots`, `stock_movements`
- `carts`, `cart_items`
- `orders`, `order_items`, `order_status_history`
- `payments`, `refunds`
- `shipments`
- `outbox_messages`
- `order_saga_state`, `saga_compensation_log`
- `wallet_transactions`, `gift_cards`
- `reviews`, `wishlists`, `recently_viewed`
- `audit_logs`

## API Endpoints

### Public APIs
- `GET /api/v1/products` - List products with filters
- `GET /api/v1/products/{slug}` - Product details
- `GET /api/v1/search/suggest?q=` - Search autocomplete

### Customer APIs (Authenticated)
- `GET /api/v1/carts/me` - Get cart
- `POST /api/v1/carts/me/items` - Add to cart
- `POST /api/v1/carts/me/checkout` - Checkout (idempotent)
- `GET /api/v1/orders/me` - Order history (cursor pagination)
- `POST /api/v1/orders/{id}/partial-cancel` - Partial cancel

## Testing

### Unit Tests
```bash
dotnet test --filter "Category=Unit"
```

### Integration Tests
```bash
dotnet test --filter "Category=Integration"
```

### Load Tests (k6)
```bash
k6 run tests/k6/smoke.js
```

## Project Structure

```
/workspace
├── src/
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   └── Api/
├── frontend/
│   ├── src/app/
│   ├── src/components/
│   ├── src/hooks/
│   ├── src/lib/
│   ├── src/store/
│   └── src/types/
├── tests/
├── k8s/
├── .github/workflows/
├── docker-compose.yml
└── docker-compose.override.yml
```

## License

Proprietary - All rights reserved
