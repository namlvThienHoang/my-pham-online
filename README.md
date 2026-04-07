# Beauty E-commerce Platform

Production-ready e-commerce system for cosmetics online based on SPEC V3.

## Tech Stack

- **Backend**: .NET 8, ASP.NET Core Web API, Minimal API
- **Frontend**: Next.js 15 (App Router, TypeScript, Tailwind CSS, shadcn/ui)
- **Database**: PostgreSQL 16 (Primary + Read Replica)
- **Caching**: Redis (StackExchange.Redis)
- **Search**: Elasticsearch 8.x
- **Message/Background**: Hangfire (Redis storage) + Outbox Pattern
- **Orchestration**: Saga (OrderSaga) + compensating transactions
- **Resilience**: Polly (Retry, Circuit Breaker, Timeout, Fallback)
- **Realtime**: SignalR + Redis backplane
- **Logging**: Serilog + Seq

## Getting Started

### Prerequisites

- Docker & Docker Compose
- .NET 8 SDK
- Node.js 20+

### Quick Start

1. **Start infrastructure services:**
```bash
docker-compose up -d
```

2. **Run database migrations:**
```bash
cd src/Api
dotnet ef database update
```

3. **Start the API:**
```bash
dotnet run
```

4. **Start the frontend:**
```bash
cd frontend
npm install
npm run dev
```

## Architecture

### Clean Architecture (Modular Monolith)

```
src/
├── Domain/               # Entities, Value Objects, Domain Events, Repository Interfaces
├── Application/          # Commands/Queries, MediatR Handlers, DTOs, Validators
├── Infrastructure/       # EF Core Repositories, Outbox, Saga, Redis, Elasticsearch, Polly
└── Api/                  # Controllers, Middleware, SignalR Hubs, Hangfire Dashboard
```

## Authentication Features

- JWT + Refresh Token Family with rotation
- MFA (TOTP) support
- Account lockout after failed attempts
- Token reuse detection

### Auth Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/auth/register` | Register new user |
| POST | `/api/v1/auth/login` | Login with email/password |
| POST | `/api/v1/auth/refresh-token` | Refresh access token |
| POST | `/api/v1/auth/enable-mfa` | Enable MFA |
| POST | `/api/v1/auth/verify-mfa` | Verify MFA code |
| POST | `/api/v1/auth/logout` | Logout |
| GET | `/api/v1/auth/me` | Get current user |

## Development

### Running Tests

```bash
dotnet test
```

### Building Docker Images

```bash
docker-compose build
```

## License

Private - All rights reserved
