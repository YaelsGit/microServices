# Phase 1 — Monolith Baseline

## Overview

Before splitting into microservices, the system was a single .NET 8 WebAPI backed by one SQL Server database (`Mechira-sinit-microservices`). All business logic — users, products, orders, inventory, lottery — lived in one process and one schema.

---

## Monolith Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│              Client (HTTP requests)                     │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│              Mechira Monolith API                       │
│                   Port 5000                             │
│                                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │  /api/users │  │ /api/gifts  │  │ /api/orders │     │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘     │
│         │                │                │             │
│  ┌──────▼──────┐  ┌──────▼──────┐  ┌──────▼──────┐     │
│  │UsersService │  │GiftsService │  │OrdersService│     │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘     │
│         │                │                │             │
│  ┌──────▼────────────────▼────────────────▼──────┐      │
│  │              Single DbContext                 │      │
│  │         (EF Core — all tables)                │      │
│  └───────────────────────┬───────────────────────┘      │
└──────────────────────────┼──────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────┐
│         SQL Server — Mechira-sinit-microservices        │
│                                                         │
│  Tables: Users | Gifts | Categories | Donors |          │
│          Orders | Lotteries                             │
└─────────────────────────────────────────────────────────┘
```

---

## Endpoints (Monolith)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/users/register` | Register a new user |
| POST | `/api/users/login` | Login, receive JWT |
| GET | `/api/users/{id}` | Get user by ID |
| GET | `/api/gifts` | List all gifts |
| GET | `/api/gifts/{id}` | Get gift by ID |
| POST | `/api/gifts` | Create a gift |
| PUT | `/api/gifts/{id}` | Update a gift (including quantity) |
| DELETE | `/api/gifts/{id}` | Delete a gift |
| GET | `/api/categories` | List categories |
| GET | `/api/donors` | List donors |
| POST | `/api/orders` | Place an order (decrements inventory inline) |
| GET | `/api/orders/{id}` | Get order by ID |
| GET | `/api/orders/user/{userId}` | Get all orders for a user |
| POST | `/api/lottery` | Create a lottery draw |
| GET | `/api/lottery/{id}` | Get lottery result |

---

## docker-compose (Monolith)

```yaml
services:
  api:
    build: .
    ports:
      - "5000:5000"
    environment:
      ConnectionStrings__DefaultConnection: "Server=db;Database=Mechira-sinit-microservices;..."
    depends_on:
      db:
        condition: service_healthy

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "YourPasswordHere123!"
    ports:
      - "1433:1433"
    healthcheck:
      test: ["CMD-SHELL", "sqlcmd -S localhost -U sa -P YourPasswordHere123! -Q 'SELECT 1' -C"]
      interval: 10s
      retries: 5
```

**Checkpoint:** `docker compose up` → create a gift, place an order, see inventory decrease in the same DB.

---

## 3 Problems at Scale

### Problem 1 — Tight Coupling (No Independent Deployment)
All features are in one process. Deploying a fix to the `Orders` module requires redeploying the entire application — including `Users`, `Gifts`, and `Lottery`. A bug in any module can crash the whole system.

**At scale:** A team of 10 developers all committing to the same codebase creates merge conflicts, deployment bottlenecks, and a single point of failure.

### Problem 2 — Shared Database = No Isolation
All services read and write to the same SQL Server database. A slow query in the `Lottery` module can lock tables used by `Orders`. Schema changes (e.g., adding a column to `Gifts`) require coordinating with every other module.

**At scale:** You cannot scale the `Gifts` catalog independently from `Orders`. If the catalog gets 10× more traffic, you must scale the entire monolith — wasting resources on modules that don't need it.

### Problem 3 — Synchronous Inventory Reservation = Fragility
When a user places an order, the monolith decrements inventory in the same HTTP request:
```
POST /api/orders
  → validate user
  → check gift exists
  → decrement gift.Quantity  ← inline, synchronous
  → create order record
  → return 200
```
If the DB write for inventory succeeds but the order write fails (network blip, timeout), inventory is decremented but no order exists — **data inconsistency with no recovery path**.

**At scale:** Under high concurrency, two simultaneous orders for the last item can both pass the `quantity > 0` check before either decrements — **race condition / overselling**.

---

## Before vs. After

| Aspect | Monolith | Microservices |
|--------|----------|---------------|
| Deployment | All-or-nothing | Per-service |
| Database | 1 shared schema | 5 isolated databases |
| Scaling | Vertical only | Horizontal per service |
| Inventory reservation | Inline HTTP (fragile) | Async saga (compensating) |
| Failure isolation | None — one crash = all down | Circuit breaker per service |
| Team autonomy | One codebase, one pipeline | Independent repos/pipelines |
