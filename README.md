# Mechira Microservices Platform

[![CI](https://github.com/AidyGit/Final-Project-Arc-AI/actions/workflows/ci.yml/badge.svg)](https://github.com/AidyGit/Final-Project-Arc-AI/actions/workflows/ci.yml)

A production-grade microservices system evolved from a monolithic e-commerce/auction API into a distributed, scalable platform demonstrating enterprise patterns: service isolation, asynchronous messaging, API gateway routing, caching, resilience patterns, and structured observability.

## 🎯 Project Goal

Transform a monolithic API into a microservices architecture with:
- ✅ **Phase 1:** Database-per-service isolation (COMPLETE)
- ✅ **Phase 2:** Correlation ID propagation for distributed tracing (COMPLETE)
- ✅ **Phase 3:** API Gateway, BFF, load balancing (COMPLETE)
- ✅ **Phase 4:** Async messaging, saga pattern, compensation (COMPLETE)
- ✅ **Phase 5:** Centralized logging, observability (COMPLETE)
- ✅ **Bonus:** CI/CD pipeline with GitHub Actions (COMPLETE)

## 🏗 Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                 API Gateway (Ocelot)                 │
│                     Port 5000                        │
└──────────────────────┬──────────────────────────────┘
                       │
        ┌──────────────┼──────────────┬───────────────┐
        │              │              │               │
    ┌───▼────┐   ┌────▼────┐   ┌────▼────┐   ┌────▼────┐
    │  Auth  │   │ Catalog │   │ Orders  │   │Lottery  │ Notification
    │Service │   │ Service │   │ Service │   │Service  │ Service
    │:5001   │   │:5002    │   │:5003    │   │:5004    │ :5005
    └───┬────┘   └────┬────┘   └────┬────┘   └────┬────┘
        │             │             │              │
        └─────┬───────┴─────┬───────┴──────────────┘
              │             │
        ┌─────▼──────┐ ┌───▼──────────┐
        │  RabbitMQ  │ │   Redis      │
        │ (Messaging)│ │  (Caching)   │
        └────────────┘ └──────────────┘

DATABASE-PER-SERVICE (SQL Server):
┌────────────────────────────────────────────┐
│         SQL Server Instance (1433)         │
├────────────────────────────────────────────┤
│ • Mechira-AuthService                      │
│ • Mechira-CatalogService                   │
│ • Mechira-OrderService                     │
│ • Mechira-LotteryService                   │
│ • Mechira-NotificationService              │
└────────────────────────────────────────────┘
```

## 📦 Services

| Service | Port | Database | Purpose |
|---------|------|----------|---------|
| **AuthService** | 5001 | `Mechira-AuthService` | User authentication, JWT tokens |
| **CatalogService** | 5002 | `Mechira-CatalogService` | Product catalog, donations, categories (cached via Redis) |
| **OrderService** | 5003 | `Mechira-OrderService` | Order management, inventory tracking, saga orchestration |
| **LotteryService** | 5004 | `Mechira-LotteryService` | Lottery draw management |
| **NotificationService** | 5005 | `Mechira-NotificationService` | Event-driven notifications (RabbitMQ consumers) |
| **API Gateway** | 5000 | None | Routes all requests, JWT validation (Ocelot) |
| **RabbitMQ** | 5672/15672 | N/A | Message broker for async saga events |
| **Redis** | 6379 | N/A | Distributed cache for catalog reads |

## 🚀 Quick Start

### Prerequisites
- Docker & Docker Compose
- .NET 8.0 SDK (for local development)
- SQL Server Management Studio (for DB inspection, optional)

### Run All Services via Docker
```bash
cd server
docker compose up -d

# Wait for healthy services (30-60 seconds)
docker compose ps

# View logs for any service
docker compose logs -f {service-name}
# Example: docker compose logs -f order-service
```

### Access Services
- **API Gateway:** http://localhost:5000/swagger
- **AuthService:** http://localhost:5001/swagger
- **CatalogService:** http://localhost:5002/swagger
- **OrderService:** http://localhost:5003/swagger
- **LotteryService:** http://localhost:5004/swagger
- **RabbitMQ Management:** http://localhost:15672 (guest:guest)

### Run Locally (Development)

Each service can be run independently:

```bash
# Terminal 1: AuthService
cd server/Services/AuthService
dotnet run

# Terminal 2: CatalogService
cd server/Services/CatalogService
dotnet run

# Terminal 3: OrderService
cd server/Services/OrderService
dotnet run

# Terminal 4: API Gateway
cd server/Gateway/ApiGateway
dotnet run

# All require RabbitMQ & Redis to be running (see docker-compose.yml for setup)
```

## 🔐 Authentication

All services validate JWT tokens. Get a token from AuthService:

```bash
# Login (default user: admin@example.com)
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"AdminPassword123!"}'

# Response contains JWT token
# Use in subsequent requests: Authorization: Bearer {token}
```

## 💬 Async Messaging (RabbitMQ)

Services communicate via RabbitMQ for order sagas:

```
OrderService publishes:
  └─ OrderPlaced event

CatalogService consumes OrderPlaced:
  ├─ Reserves inventory
  └─ Publishes: InventoryReserved OR InventoryFailed

OrderService consumes:
  ├─ InventoryReserved → Order confirmed
  ├─ InventoryFailed → Order cancelled (compensation)
  └─ Publishes: OrderConfirmed OR OrderCancelled

NotificationService consumes:
  └─ Sends email notifications
```

## 💾 Database Isolation (Phase 1 ✅)

**Architecture Decision:** Each service owns its database (database-per-service pattern).

- **Why:** Enables independent deployment, scaling, and schema evolution
- **How:** 5 separate databases in single SQL Server instance
- **Tradeoff:** Multi-service transactions require saga pattern (implemented via RabbitMQ)

See [`md/ADR_001_DATABASE_PER_SERVICE.md`](./md/ADR_001_DATABASE_PER_SERVICE.md) for full rationale.

### Verify Database Isolation
```bash
# Connect to SQL Server (in SSMS or via sqlcmd):
sqlcmd -S localhost,1433 -U sa -P YourPasswordHere123!

# List all microservices databases:
SELECT name FROM sys.databases WHERE name LIKE 'Mechira-%'

# Example output:
# Mechira-AuthService
# Mechira-CatalogService
# Mechira-LotteryService
# Mechira-NotificationService
# Mechira-OrderService
```

## 🔄 Resilience Patterns (Implemented)

### Retry + Circuit Breaker (Polly)
OrderService calls to AuthService and CatalogService use:
- **Retry Policy:** 3 exponential backoff attempts
- **Circuit Breaker:** Opens after 50% failure rate, waits 10s before retry

```csharp
// Example: OrderService calling CatalogService
var policy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .Retry(retryCount: 3)
    .Wrap(
        Policy
            .Handle<HttpRequestException>()
            .CircuitBreaker(handledEventsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(10))
    );
```

## 📊 Observability

### Structured Logging (Serilog)
All services log to:
- **Console:** Stdout (visible in docker compose logs)
- **File:** `logs/{service-name}-{date}.txt` (rotates daily)
- **Format:** ISO 8601 timestamp, log level, message, exception details

### Health Checks
Each service exposes `/api/health` (returns 200 OK + JSON status):
```bash
curl http://localhost:5001/api/health
# {
#   "status": "healthy",
#   "timestamp": "2024-01-15T10:30:00Z",
#   "database": "connected"
# }
```

### Correlation IDs (Phase 2 ✅)
Request flow can be traced via `X-Correlation-ID` header across all services:
- Generated at the Gateway on every request (or forwarded if client provides one)
- Propagated via HTTP headers to all downstream services (AuthService, CatalogService, OrderService)
- Propagated through RabbitMQ message headers (MassTransit CorrelationId)
- Included in every Serilog log entry via `LogContext.PushProperty`
- Aggregated in **Seq** at http://localhost:8081 — search by `CorrelationId` to trace a full saga

## 🧪 Testing

### Run Unit Tests
```bash
# AuthService tests
cd server/Services/AuthService/AuthService.Tests
dotnet test

# All services tests
cd server
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true
```

### Integration Tests (Docker)
```bash
# Start all services
docker compose up -d

# Run health checks
curl http://localhost:5000/api/health
curl http://localhost:5001/api/health
# ... etc for each service

# Test full saga flow (order placement)
curl -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer {token}" \
  -d '{"giftId": 1, "userId": 1, ...}'
```

## 🏗 File Structure

```
server/
├── docker-compose.yml              # All services + RabbitMQ + Redis
├── docker-compose.prod.yml         # Production config
├── init-databases.sql              # Database initialization script
├── Services/
│   ├── AuthService/                # User authentication
│   │   ├── Data/AuthDbContext.cs
│   │   ├── Controllers/
│   │   ├── Services/
│   │   └── Program.cs
│   ├── CatalogService/             # Product catalog
│   │   ├── Data/CatalogDbContext.cs
│   │   ├── Controllers/
│   │   ├── Repository/
│   │   └── Program.cs
│   ├── OrderService/               # Order management + Saga
│   │   ├── Data/OrderDbContext.cs
│   │   ├── Controllers/
│   │   ├── Consumers/              # RabbitMQ message consumers
│   │   └── Program.cs
│   ├── LotteryService/             # Lottery management
│   │   ├── Data/LotteryDbContext.cs
│   │   └── Program.cs
│   └── NotificationService/        # Event-driven notifications
│       ├── Consumers/              # RabbitMQ message consumers
│       └── Program.cs
├── Gateway/
│   └── ApiGateway/                 # Ocelot API Gateway
│       ├── ocelot.json             # Route configuration
│       ├── Middleware/
│       └── Program.cs
├── Shared/
│   └── SharedModels/               # Shared DTOs, models, interfaces
│       ├── Models/
│       └── Interfaces/
└── md/
    ├── PHASE1_DATABASE_ISOLATION.md
    ├── PHASE2_CORRELATION_ID.md (WIP)
    ├── PHASE3_GATEWAY_BFF_LB.md (WIP)
    ├── PHASE4_MESSAGING_SAGA.md
    ├── PHASE5_MONITORING.md (WIP)
    ├── ADR_001_DATABASE_PER_SERVICE.md
    ├── ADR_002_SQL_SERVER_ALL_SERVICES.md
    ├── ADR_003_MESSAGING_RABBITMQ.md (WIP)
    └── README.md (this file)
```

## 📚 Architecture Decision Records (ADRs)

Design decisions are documented as ADRs in `md/` folder:

| ADR | Title | Status |
|-----|-------|--------|
| [ADR-001](./md/ADR_001_DATABASE_PER_SERVICE.md) | Database-Per-Service Pattern | ✅ Accepted |
| [ADR-002](./md/ADR_002_SQL_SERVER_ALL_SERVICES.md) | SQL Server For All Services | ✅ Accepted |
| ADR-003 | RabbitMQ for Messaging | ⏳ WIP |
| ADR-004 | Loki for Log Aggregation | ⏳ WIP |
| ADR-005 | NGINX for Load Balancing | ⏳ WIP |

## 🔍 Troubleshooting

### Services won't start
```bash
# Check database connection
docker compose logs mssql-db | head -20

# Verify SQL Server is healthy
docker compose exec mssql-db /opt/mssql-tools/bin/sqlcmd -U sa -P YourPasswordHere123! -Q "SELECT 1"

# Check service logs
docker compose logs auth-service
```

### RabbitMQ connection errors
```bash
# Verify RabbitMQ is running
docker compose logs rabbitmq-service

# Check management UI
# http://localhost:15672 (guest:guest)
```

### Redis connection errors
```bash
# Verify Redis is running
docker compose logs redis-cache

# Check Redis connectivity
docker compose exec redis-cache redis-cli ping
```

## 🤖 CI/CD Pipeline

GitHub Actions pipeline at `.github/workflows/ci.yml`:

| Job | Trigger | What it does |
|-----|---------|-------------|
| **Build & Test** | Every push & PR | `dotnet build` + `dotnet test` — failing test blocks the pipeline |
| **Docker Build** | Push only (after tests pass) | Builds & pushes 6 Docker images to GHCR tagged with commit SHA |

## 🔗 Related Documentation

- [Architecture Document](./md/ARCHITECTURE_DOCUMENT.md) — Final diagram, ADRs, technology decisions
- [Phase 1: Monolith Baseline](./md/PHASE1_MONOLITH_BASELINE.md) — Before/after diagram, endpoints, 3 scale problems
- [Phase 1: Database Isolation](./md/PHASE1_DATABASE_ISOLATION.md) — Service data autonomy
- [Phase 4: Messaging & Saga](./md/PHASE4_MESSAGING_SAGA.md) — Event-driven order processing
- [Demo Evidence](./md/DEMO_EVIDENCE.md) — Saga happy/compensation path, cache hit/miss, correlation ID trace
- [API Documentation](./Gateway/ApiGateway/README.md) — Gateway configuration

## 📝 License

Private project for educational purposes.

## 📧 Contact

For questions about the microservices architecture, refer to the ADRs and phase documentation.
