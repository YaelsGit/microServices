# ADR-003: MongoDB for CatalogService Gifts (Polyglot Persistence)

## Status
✅ **ACCEPTED**

## Date
2026-07-02

## Context

ADR-002 deferred polyglot persistence to a later phase. This ADR implements it.

The `CatalogService` manages three entities:
- **Gifts** — product catalog items with varying attributes per category (a book has ISBN/author, electronics have voltage/warranty, clothing has size/color)
- **Donors** — stable relational data (name, email, address)
- **Categories** — stable lookup table

The `Gifts` entity is a poor fit for a rigid relational schema because:
- Different gift categories require different attributes
- Adding a new category attribute in SQL requires an ALTER TABLE migration across all rows
- Product catalogs in e-commerce are the canonical use case for document databases

## Decision

**Migrate `Gifts` to MongoDB. Keep `Donors` and `Categories` in SQL Server.**

- `Gifts` → MongoDB collection `gifts` in database `Mechira-CatalogService`
- `Donors` → SQL Server table `Catalog.Donors` (unchanged)
- `Categories` → SQL Server table `Catalog.Categories` (unchanged)

This is **partial polyglot persistence within a single service** — the service uses two databases, each for the data family it fits best.

## Rationale

### Why MongoDB for Gifts?

**1. Document model fits varying product schemas (BASE over ACID)**

A gift catalog is read-heavy and schema-heterogeneous. MongoDB's document model allows each gift to carry only the attributes relevant to its category:

```json
// Electronics gift
{ "giftId": 1, "name": "Laptop", "price": 2999, "voltage": "220V", "warranty": "2 years" }

// Book gift
{ "giftId": 2, "name": "Clean Code", "price": 89, "isbn": "978-0132350884", "author": "Robert Martin" }
```

In SQL, this would require either a wide sparse table (many NULLs) or an EAV pattern (complex queries).

**2. CAP Theorem — AP model acceptable for catalog reads**

Catalog reads tolerate **eventual consistency** (BASE):
- A product showing stale price for a few seconds is acceptable
- Availability is more important than strict consistency for browsing
- Inventory reservation (the critical path) still goes through the Saga → SQL Server in OrderService

**3. Horizontal scalability**

MongoDB scales horizontally via sharding. If the gift catalog grows to millions of items, MongoDB handles it natively. SQL Server requires expensive vertical scaling.

**4. Redis cache already provides read consistency**

The `CacheService` (cache-aside, 10-minute TTL) sits in front of MongoDB reads. Cache invalidation on update ensures clients see fresh data within the TTL window.

### Why SQL Server stays for Donors and Categories?

- **Donors** have a stable, well-defined schema (name, email, address, phone) — no benefit from document model
- **Categories** is a small lookup table — relational is simpler
- Both are referenced by `GiftId` (foreign key concept) — keeping them relational preserves referential integrity within the service

## CAP Theorem Alignment

| Entity | Consistency Model | Database | Justification |
|--------|------------------|----------|---------------|
| **Gifts** | AP (eventual) | MongoDB | Catalog reads tolerate staleness; Redis cache bridges the gap |
| **Donors** | CP (strong) | SQL Server | Donor data is authoritative reference data |
| **Categories** | CP (strong) | SQL Server | Lookup table, rarely changes |
| **Orders** (OrderService) | CP (ACID) | SQL Server | Financial data — must be consistent |

## ACID vs BASE

| Property | SQL Server (Donors/Categories) | MongoDB (Gifts) |
|----------|-------------------------------|-----------------|
| **Atomicity** | Full ACID transactions | Single-document atomic |
| **Consistency** | Schema-enforced | Application-enforced |
| **Isolation** | Row-level locking | Document-level |
| **Durability** | WAL + fsync | Journal + write concern |
| **BASE** | No | Yes — eventually consistent reads acceptable |

## Implementation

### New files
- `Data/MongoGiftDocument.cs` — MongoDB document model
- `Repository/MongoGiftsRepository.cs` — replaces `GiftsRepository` for Gifts

### Changed files
- `CatalogService.csproj` — added `MongoDB.Driver 2.28.0`
- `Program.cs` — registers `IMongoDatabase`, swaps `IGiftsRepository` → `MongoGiftsRepository`
- `appsettings.json` — added `ConnectionStrings:MongoConnection`
- `docker-compose.yml` — added `mongodb` service (mongo:7.0), wired env var to catalog-service

### Unchanged
- `GiftsRepository.cs` — kept for reference (not registered)
- `CatalogDbContext.cs` — still manages Donors and Categories in SQL Server
- All controllers, services, interfaces — zero changes (repository pattern abstraction)

## Consequences

### Positive ✅
- Flexible gift schema — new category attributes require no migrations
- Demonstrates polyglot persistence as required by Phase 2
- Read performance improved (MongoDB + Redis cache-aside)
- Independent scaling of gift catalog

### Negative ⚠️
- Two databases to manage in CatalogService
- No cross-collection JOIN between Gifts and Donors/Categories — resolved at service layer
- Auto-increment GiftId implemented in application layer (not DB-native) — acceptable for this scale

## Alternatives Considered

### ❌ Full migration of all CatalogService data to MongoDB
- Donors and Categories don't benefit from document model
- Adds unnecessary complexity

### ❌ SQL Server JSON columns (`FOR JSON PATH`)
- Possible but loses MongoDB's native query operators, indexing, and horizontal scaling
- Doesn't demonstrate polyglot persistence

### ✅ MongoDB for Gifts only (CHOSEN)
- Right tool for the right data
- Minimal disruption — repository pattern means zero controller/service changes

## Related Decisions
- **ADR-001:** Database-per-service (this extends it with polyglot within a service)
- **ADR-002:** SQL Server for all services in Phase 1 — this ADR supersedes it for Gifts
- **Phase 4:** Redis cache-aside on top of MongoDB reads

## References
- MongoDB Use Cases: Product Catalog (mongodb.com/use-cases/catalog)
- CAP Theorem: Brewer's theorem — choose 2 of 3
- BASE: Basically Available, Soft state, Eventually consistent
