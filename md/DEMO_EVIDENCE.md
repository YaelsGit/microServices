# Demo Evidence Guide

This document shows how to reproduce all 4 required demo artifacts.
Run `docker compose up -d` and wait ~60 seconds before starting.

---

## 1. Saga Happy Path

**Scenario:** Place an order for an in-stock gift → inventory reserved → order confirmed → notification sent.

### Step 1 — Register and login
```bash
# Register
curl -s -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"username":"demouser","email":"demo@example.com","password":"Demo1234!","role":"User"}'

# Login → copy the token from the response
curl -s -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@example.com","password":"Demo1234!"}'
```

### Step 2 — Create a gift with stock
```bash
TOKEN="<paste token here>"

curl -s -X POST http://localhost:5002/api/gifts \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Gift","description":"For demo","price":99.90,"quantity":10,"donorId":1,"categoryId":1}'
# Note the returned giftId (e.g. 1)
```

### Step 3 — Place an order
```bash
curl -s -X POST http://localhost:5003/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"giftId":1,"quantity":2}'
# Response: {"status":"Pending","orderId":1,...}
```

### Step 4 — Wait ~3 seconds, then check order status
```bash
curl -s http://localhost:5003/api/orders/1 \
  -H "Authorization: Bearer $TOKEN"
# Expected: "status":"Confirmed"
```

### Expected log sequence (Seq → http://localhost:8081)
```
[OrderService]      [SAGA] Order 1 created with status Pending. CorrelationId: <guid>
[OrderService]      [SAGA] OrderPlaced event published for Order 1
[CatalogService]    [OrderPlacedConsumer] Received OrderPlaced for Order: <guid>
[CatalogService]    [OrderPlacedConsumer] Stock reserved in MongoDB for GiftId: 1. Remaining: 8
[CatalogService]    [OrderPlacedConsumer] InventoryReserved published for Order: <guid>
[OrderService]      [InventoryReservedConsumer] Received InventoryReserved event for Order: <guid>
[OrderService]      [InventoryReservedConsumer] Order <guid> status updated to Confirmed
[OrderService]      [InventoryReservedConsumer] OrderConfirmed event published for Order: <guid>
[NotificationSvc]   [OrderConfirmedConsumer] Received OrderConfirmed event for Order: <guid>
[NotificationSvc]   [EmailSimulation] Confirmation email sent to User: <guid> for Order: <guid>
```

---

## 2. Compensation Path (Out-of-Stock)

**Scenario:** Place an order for a gift with quantity=0 → inventory fails → order cancelled → cancellation notification sent.

### Step 1 — Create a gift with zero stock
```bash
curl -s -X POST http://localhost:5002/api/gifts \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Out of Stock Gift","description":"No stock","price":50.00,"quantity":0,"donorId":1,"categoryId":1}'
# Note the returned giftId (e.g. 2)
```

### Step 2 — Place an order for the out-of-stock gift
```bash
curl -s -X POST http://localhost:5003/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"giftId":2,"quantity":1}'
# Response: {"status":"Pending","orderId":2,...}
```

### Step 3 — Wait ~3 seconds, then check order status
```bash
curl -s http://localhost:5003/api/orders/2 \
  -H "Authorization: Bearer $TOKEN"
# Expected: "status":"Cancelled"
```

### Expected log sequence (Seq → http://localhost:8081)
```
[OrderService]      [SAGA] Order 2 created with status Pending. CorrelationId: <guid>
[OrderService]      [SAGA] OrderPlaced event published for Order 2
[CatalogService]    [OrderPlacedConsumer] Received OrderPlaced for Order: <guid>
[CatalogService]    [OrderPlacedConsumer] Insufficient stock for GiftId: 2. Available: 0, Requested: 1
[CatalogService]    [OrderPlacedConsumer] InventoryFailed published for Order: <guid> — Insufficient stock...
[OrderService]      [InventoryFailedConsumer] Received InventoryFailed event for Order: <guid>, Reason: Insufficient stock...
[OrderService]      [InventoryFailedConsumer] Order <guid> status updated to Cancelled due to inventory failure
[OrderService]      [InventoryFailedConsumer] OrderCancelled event published for Order: <guid>
[NotificationSvc]   [OrderCancelledConsumer] Received OrderCancelled event for Order: <guid>
[NotificationSvc]   [EmailSimulation] Cancellation email sent to User: <guid> for Order: <guid>. Reason: Inventory failed...
```

---

## 3. Cache Hit / Miss

**Scenario:** First GET fetches from MongoDB (MISS), second GET returns from Redis (HIT).

```bash
# First call — cache miss (fetches from MongoDB, stores in Redis)
curl -s http://localhost:5002/api/gifts/1 \
  -H "Authorization: Bearer $TOKEN"

# Second call — cache hit (returns from Redis)
curl -s http://localhost:5002/api/gifts/1 \
  -H "Authorization: Bearer $TOKEN"
```

### Expected logs (CatalogService — `docker compose logs catalog-service-1`)
```
[INF] [CACHE MISS] Key: gifts:1 — fetching from MongoDB
[INF] [CACHE HIT]  Key: gifts:1 — returning gift from Redis
```

### Cache invalidation — update the gift and verify cache is cleared
```bash
curl -s -X PUT http://localhost:5002/api/gifts/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"price":109.90}'

# Expected log:
# [INF] [CACHE INVALIDATE] Keys: gifts:1, gifts:all — gift 1 updated

# Next GET is a cache miss again (fetches fresh data from MongoDB)
curl -s http://localhost:5002/api/gifts/1 -H "Authorization: Bearer $TOKEN"
# [INF] [CACHE MISS] Key: gifts:1 — fetching from MongoDB
```

---

## 4. Full Correlation ID Trace

**Scenario:** Trace a complete order saga using a single Correlation ID across all services and the broker.

### Step 1 — Place an order with a custom Correlation ID
```bash
CORR_ID="demo-trace-$(date +%s)"

curl -s -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Correlation-ID: $CORR_ID" \
  -H "Content-Type: application/json" \
  -d '{"giftId":1,"quantity":1}'

echo "Trace this ID in Seq: $CORR_ID"
```

### Step 2 — Search in Seq
1. Open http://localhost:8081
2. In the search bar enter: `CorrelationId = "<your CORR_ID>"`
3. You will see log entries from **all 3 services** (OrderService, CatalogService, NotificationService) sharing the same ID

### Expected trace in Seq (all lines share the same CorrelationId)
```
[ApiGateway]        Generated/forwarded X-Correlation-ID: demo-trace-...
[OrderService]      [SAGA] Order N created with status Pending. CorrelationId: demo-trace-...
[OrderService]      [SAGA] OrderPlaced event published for Order N
[CatalogService]    [OrderPlacedConsumer] Received OrderPlaced for Order: demo-trace-...
[CatalogService]    [OrderPlacedConsumer] InventoryReserved published for Order: demo-trace-...
[OrderService]      [InventoryReservedConsumer] Order demo-trace-... status updated to Confirmed
[OrderService]      [InventoryReservedConsumer] OrderConfirmed event published
[NotificationSvc]   [OrderConfirmedConsumer] Received OrderConfirmed event for Order: demo-trace-...
[NotificationSvc]   [EmailSimulation] Confirmation email sent for Order: demo-trace-...
```

The Correlation ID travels:
- HTTP header → Gateway → OrderService
- Serilog `LogContext` → all log lines in OrderService
- MassTransit `CorrelationId` → RabbitMQ message header → CatalogService consumer
- CatalogService consumer → `LogContext` → all log lines in CatalogService
- MassTransit → RabbitMQ → OrderService consumer → NotificationService consumer

---

## Load Balancing Proof

```bash
# Call the catalog endpoint 6 times and observe X-Upstream-Server alternating
for i in {1..6}; do
  curl -s -I http://localhost:5002/api/gifts | grep -i x-upstream-server
done

# Expected output (alternating between replicas):
# X-Upstream-Server: 172.x.x.x:5002   ← catalog-service-1
# X-Upstream-Server: 172.x.x.y:5002   ← catalog-service-2
# X-Upstream-Server: 172.x.x.x:5002   ← catalog-service-1
# X-Upstream-Server: 172.x.x.y:5002   ← catalog-service-2
```
