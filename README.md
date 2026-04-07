# Beauty E-Commerce Solution

## Solution Structure

```
src/
├── Domain/               # Entities, Value Objects, Domain Events, Repository Interfaces
├── Application/          # Commands/Queries, MediatR Handlers, DTOs, Validators
├── Infrastructure/       # EF Core Repositories, Outbox, Saga, Redis, Elasticsearch, Polly
└── Api/                  # Controllers, Minimal APIs, Middleware, SignalR Hubs, Hangfire Dashboard
frontend/                 # Next.js 15 App Router
k8s/                      # Kubernetes manifests
.github/workflows/        # CI/CD pipelines
docker-compose.yml        # Development environment
```

## Tech Stack

- **Backend**: .NET 8, ASP.NET Core Web API, Minimal API
- **Frontend**: Next.js 15 (App Router, TypeScript, Tailwind CSS, shadcn/ui)
- **Database**: PostgreSQL 16 (Primary + Read Replica)
- **Caching**: Redis (StackExchange.Redis)
- **Search**: Elasticsearch 8.x
- **Message / Background**: Hangfire (Redis storage) + Outbox Pattern
- **Orchestration**: Saga (OrderSaga) + compensating transactions
- **Resilience**: Polly (Retry, Circuit Breaker, Timeout, Fallback)
- **Realtime**: SignalR + Redis backplane
- **Logging**: Serilog + Seq / Elasticsearch
- **Container**: Docker + Docker Compose / Kubernetes
