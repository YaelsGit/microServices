# Architecture Document — Mechira Microservices Platform

## 1. System Overview

This project transforms a monolithic e-commerce/auction API into a production-grade microservices platform. The system supports browsing products, placing orders, reserving inventory, and notifying customers — all through distributed, independently deployable services.

---

## 2. Final Architecture Diagram

```
                        ┌─────────────────────────────────┐
         Client         │       API Gateway (Ocelot)       │
         ──────►        │           Port 5000              │
                        │  • JWT validation                │
                        │  • Correlation ID generation     │
                        │  • Route to downstream services  │
                        └────────────┬────────────────────┘
                                     │
              ┌──────────────────────┼──────────────────────┐
              │                      │                       │
     ┌────────▼──────┐    ┌──────────▼──────┐    ┌─────────▼──────┐
     │  AuthService  │    │  Nginx LB :5002  │    │  OrderService  │
     │    :5001      │    │  (round-robin)   │    │    :5003       │
     │  SQL Server   │    └────────┬─────────┘    │  SQL Server    │
     └───────────────┘            │               └────────┬───────┘
                          ┌───────┴────────┐               │
                   ┌──────▼──────┐  ┌──────▼──────┐        │
                   │  Catalog-1  │  │  Catalog-2  │        │
                   │   :5002     │  │   :5002     │        │
                   │  MongoDB +  │  │  MongoDB +  │        │
                   │  SQL Server │  │  SQL Server │        │
                   └─────────────┘  └─────────────┘        │
                                                            │
              ┌─────────────────────────────────────────────┘
              │         RabbitMQ (MassTransit)
              │
              │  OrderPlaced ──────────► CatalogService
              │                              │
              │         InventoryReserved ◄──┤
              │         InventoryFailed  ◄──┘
              │
              │  OrderConfirmed / OrderCancelled ──► NotificationService
              │
     ┌────────▼──────┐    ┌───────────────┐    ┌───────────────┐
     │LotteryService │    │NotificationSvc│    │     Seq       │
     │    :5004      │    │    :5005      │    │  Log Aggreg.  │
     │  SQL Server   │    │  (consumers)  │    │    :8081      │
     └───────────────┘    └───────────────┘    └───────────────┘

INFRASTRUCTURE:
┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  SQL Server  │  │   MongoDB    │  │    Redis     │  │  RabbitMQ    │
│    :1433     │  │   :27017     │  │    :6379     │  │  :5672/15672 │
│  5 databases │  │  gifts coll. │  │  catalog     │  │  saga events │
└──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘
```

---

## 3. Services Summary

| Service | Port | Database | Role |
|---------|------|----------|------|
| API Gateway | 5000 | — | JWT validation, routing, Correlation ID |
| AuthService | 5001 | SQL Server | User registration, login, JWT issuance |
| CatalogService (×2) | 5002 | MongoDB + SQL Server | Gifts (MongoDB), Donors/Categories (SQL), Redis cache |
| OrderService | 5003 | SQL Server | Order management, saga orchestration |
| LotteryService | 5004 | SQL Server | Lottery draw management |
| NotificationService | 5005 | — | Event-driven email notifications |

---

## 4. Architecture Decision Records (ADRs)

### ADR-001 — Database-Per-Service Pattern

**Status:** ✅ Accepted

**Context:** The original monolith used a single shared database (`Mechira-sinit-microservices`). All services read and wrote to the same schema, creating tight coupling and preventing independent deployment.

**Decision:** Each service owns its own SQL Server database. No service queries another service's database directly.

**Rationale:**
- **Autonomy:** Services can be deployed, scaled, and migrated independently
- **CAP alignment:** Each service chooses its own consistency model (CP for Orders and Auth, eventual consistency acceptable for Catalog)
- **Saga pattern:** Cross-service transactions handled via RabbitMQ choreography (not distributed SQL transactions)

**Tradeoffs:**
- ❌ No cross-service JOINs → mitigated by BFF aggregation layer
- ❌ No distributed ACID transactions → mitigated by saga + compensation

---

### ADR-002 — SQL Server for Relational Data (Orders, Auth, Lottery)

**Status:** ✅ Accepted

**Context:** OrderService handles financial transactions. AuthService manages user credentials. Both require strong consistency guarantees.

**Decision:** SQL Server with EF Core for OrderService, AuthService, and LotteryService.

**Rationale:**
- **ACID:** Money and authentication data must be atomic, consistent, isolated, and durable
- **CAP — CP model:** These services choose Consistency over Availability. A failed transaction is better than an inconsistent one
- **Transactions:** `BEGIN TRANSACTION / COMMIT` ensures inventory and order state never diverge within a service

**Why not NoSQL here:**
- BASE (Basically Available, Soft state, Eventually consistent) is unacceptable for financial data
- A user placing an order must see the exact current state, not a stale cached value

---

### ADR-003 — MongoDB for CatalogService Gifts (Polyglot Persistence)

**Status:** ✅ Accepted

**Context:** The gift catalog contains items from many categories (electronics, books, clothing). Each category has different attributes. A relational schema requires either a wide sparse table (many NULLs) or a complex EAV pattern.

**Decision:** Migrate `Gifts` to MongoDB. Keep `Donors` and `Categories` in SQL Server.

**Rationale:**
- **Document model:** Each gift document carries only the attributes relevant to its category — no NULLs, no schema migrations when adding a new attribute
- **CAP — AP model:** Catalog reads tolerate eventual consistency. A product showing a slightly stale price for seconds is acceptable. Availability is more important than strict consistency for browsing
- **BASE:** The catalog is read-heavy. MongoDB's BASE model (eventually consistent reads) combined with Redis cache-aside (10-minute TTL) provides excellent read performance
- **Horizontal scaling:** MongoDB shards natively if the catalog grows to millions of items

**Example — flexible schema:**
```json
// Electronics
{ "giftId": 1, "name": "Laptop", "price": 2999, "voltage": "220V", "warranty": "2 years" }

// Book
{ "giftId": 2, "name": "Clean Code", "price": 89, "isbn": "978-0132350884", "author": "Robert Martin" }
```

**Tradeoffs:**
- ❌ Two databases in CatalogService → mitigated by repository pattern (zero controller/service changes)
- ❌ No native foreign keys between MongoDB and SQL → resolved at service layer

---

### ADR-004 — RabbitMQ for Async Messaging (Saga Pattern)

**Status:** ✅ Accepted

**Context:** The order flow requires coordination between OrderService (create order), CatalogService (reserve inventory), and NotificationService (notify customer). Synchronous HTTP calls create tight coupling and cascading failures.

**Decision:** RabbitMQ with MassTransit for choreography-based saga.

**Saga flow:**
```
OrderService    → publishes OrderPlaced
CatalogService  → consumes OrderPlaced → publishes InventoryReserved OR InventoryFailed
OrderService    → consumes InventoryReserved → publishes OrderConfirmed
                → consumes InventoryFailed   → publishes OrderCancelled (compensation)
NotificationSvc → consumes OrderConfirmed / OrderCancelled → sends email
```

**Why RabbitMQ over alternatives:**

| | RabbitMQ | Kafka | Azure Service Bus |
|---|---|---|---|
| **Model** | Push (broker delivers) | Pull (consumer polls) | Push |
| **Ordering** | Per-queue | Per-partition | Per-session |
| **Replay** | ❌ No log retention | ✅ Log-based replay | ❌ Limited |
| **Complexity** | Low | High | Medium |
| **Best for** | Task queues, sagas | Event streaming, analytics | Azure-native apps |
| **Our choice** | ✅ | — | — |

**Why RabbitMQ fits here:** Our saga is a short-lived workflow (order → inventory → notification). We need reliable delivery and dead-letter queues, not log replay. RabbitMQ's push model with MassTransit's consumer abstraction is the simplest fit.

**Idempotency:** Consumers check order status before updating — processing the same message twice produces the same result.

---

### ADR-005 — Redis for Catalog Caching (Cache-Aside Pattern)

**Status:** ✅ Accepted

**Context:** CatalogService reads (gift listings) are the most frequent operation. MongoDB reads are fast but adding a cache layer reduces latency and database load.

**Decision:** Redis with cache-aside pattern. TTL: 10 minutes. Invalidation on gift update/delete.

**Cache-aside flow:**
```
Read:  Check Redis → HIT: return cached value
                   → MISS: read MongoDB → store in Redis → return value

Write: Update MongoDB → DELETE Redis key (invalidate)
```

**Why Redis over in-memory cache:**
- Shared across both CatalogService replicas (behind Nginx) — in-memory cache would be inconsistent between replicas
- Survives service restarts
- TTL-based expiry handles stale data automatically

---

## 5. Phase 3 — Gateway, BFF, Load Balancing

### API Gateway (Ocelot)
All client traffic enters through the gateway on port 5000. Services are not directly exposed. The gateway:
- Validates JWT tokens before forwarding requests
- Generates/forwards `X-Correlation-ID` header for distributed tracing
- Routes to downstream services via `ocelot.json`

### BFF — Backend for Frontend
Two aggregation endpoints in `BffController`:
- `GET /bff/order-details/{orderId}` — fetches order from OrderService + gift details from CatalogService in a single response
- `GET /bff/user-dashboard/{userId}` — fetches all user orders + enriches each with gift data (parallel requests)

**Why BFF:** Eliminates multiple client round-trips. The web client makes one call instead of N+1.

### Load Balancing (Nginx)
Two CatalogService replicas (`catalog-service-1`, `catalog-service-2`) sit behind an Nginx reverse proxy with round-robin load balancing. The `X-Upstream-Server` response header shows which replica served each request — proof that load balancing works.

---

## 6. Phase 5 — Observability

### Structured Logging (Serilog)
Every service logs to:
- Console (stdout → `docker compose logs`)
- Rolling file (`logs/{service}-YYYY-MM-DD.txt`)
- **Seq** at `http://localhost:8081` — centralized log aggregation

Log format includes `CorrelationId` in every line:
```
[2024-01-15 14:23:45] [INF] [abc-123] [SAGA] Order 42 created with status Pending
```

### Correlation ID Propagation
A single `X-Correlation-ID` traces a request across all services:
1. **Gateway** generates the ID (or forwards client-provided one) and writes it to the request headers before Ocelot routes
2. **Each service middleware** reads the header, stores in `HttpContext.Items`, and pushes to `Serilog.Context.LogContext`
3. **HttpClients** (AuthServiceClient, CatalogServiceClient) forward the header on every outgoing HTTP call via `IHttpContextAccessor`
4. **MassTransit** propagates `CorrelationId` through RabbitMQ message headers automatically
5. **Seq** — search by `CorrelationId` to see the complete saga story across all services

### Health Checks
Every service exposes `GET /api/health` returning:
```json
{ "status": "healthy", "timestamp": "...", "database": "connected" }
```
Wired into `docker-compose.yml` healthchecks so dependent services wait for healthy state.

---

## 7. Resilience Patterns

OrderService uses **Polly** (via `Microsoft.Extensions.Http.Resilience`) on all inter-service HTTP calls:

- **Retry:** 3 attempts with exponential backoff (2s, 4s, 8s) on `HttpRequestException` or 5xx responses
- **Circuit Breaker:** Opens after 50% failure rate over 5+ requests in a 30-second window. Stays open for 30 seconds before attempting recovery

This prevents cascading failures — if AuthService is slow, OrderService retries gracefully and eventually opens the circuit rather than queuing up threads.

---

## 8. CI/CD Pipeline (Bonus)

GitHub Actions at `.github/workflows/ci.yml`:

**Job 1 — Build & Test** (every push and PR):
- `dotnet restore` → `dotnet build` → `dotnet test`
- Failing unit test blocks the pipeline and blocks the PR merge

**Job 2 — Docker Build** (push only, after tests pass):
- Builds 6 Docker images in parallel (matrix strategy)
- Pushes to GitHub Container Registry (GHCR)
- Tags: `ghcr.io/aidygit/mechira-{service}:{commit-sha}` + `:latest`

**Why GitHub Actions over alternatives:**
- Native to GitHub — no external CI server to maintain
- Free for public repositories
- `GITHUB_TOKEN` provides automatic GHCR authentication — no secrets to manage

---

## 9. Technology Stack Summary

| Layer | Technology | Alternative Considered | Why Chosen |
|-------|-----------|----------------------|------------|
| API Gateway | Ocelot | YARP, Traefik | Simple JSON config, .NET native |
| Messaging | RabbitMQ + MassTransit | Kafka, Azure Service Bus | Best fit for saga pattern, low complexity |
| Relational DB | SQL Server | PostgreSQL | ACID, EF Core support |
| Document DB | MongoDB | CosmosDB, RavenDB | Industry standard for catalogs, free |
| Cache | Redis | Memcached | Shared across replicas, TTL support |
| Load Balancer | Nginx | Traefik, HAProxy | Simple config, lightweight |
| Logging | Serilog + Seq | ELK, Loki+Grafana | Easy setup, powerful search UI |
| CI/CD | GitHub Actions | GitLab CI, Azure DevOps | Native to GitHub, no extra infra |
