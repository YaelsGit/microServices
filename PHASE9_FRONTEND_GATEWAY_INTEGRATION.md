# Phase 9: Angular Frontend API Gateway Integration

## Overview
Updated the Angular frontend to route all API calls through the API Gateway (port 5000) instead of calling services directly.

---

## Changes Made

### 1. Base API URLs Updated

#### Before (Direct Service Calls)
```typescript
// authService
private apiUrl = "https://localhost:7261"

// donorService  
private apiUrl = 'https://localhost:7261/donors/Donors'

// giftService
private apiUrl = 'https://localhost:7261/gifts/Gifts'

// orderService
private apiUrl = "https://localhost:7261/Orders/Orders"

// lotteryService
private apiUrl = 'https://localhost:7261/lottery/Lottery'
```

#### After (API Gateway Routes)
```typescript
// authService
private apiUrl = "http://localhost:5000/auth"

// donorService
private apiUrl = 'http://localhost:5000/catalog/donors'

// giftService
private apiUrl = 'http://localhost:5000/catalog/gifts'

// orderService
private apiUrl = "http://localhost:5000/orders"

// lotteryService
private apiUrl = 'http://localhost:5000/lottery'
```

---

## API Endpoint Mappings

### Auth Service → /auth/*
```
POST   /auth/login              → AuthService Login
POST   /auth/register           → AuthService Register
GET    /auth/users              → AuthService GetUsers
GET    /auth/users/basket/:id   → UserService GetBasket
```

### Catalog Service → /catalog/*
```
GET    /catalog/donors                      → DonorService GetAllDonors
GET    /catalog/donors/by-name/:name        → DonorService GetByNameAsync
GET    /catalog/donors/by-email/:email      → DonorService GetByEmailAsync
GET    /catalog/donors/by-gift/:gift        → DonorService GetByGiftAsync
POST   /catalog/donors                      → DonorService CreateDonorAsync
DELETE /catalog/donors/:id                  → DonorService DeleteDonorAsync
PUT    /catalog/donors/:id                  → DonorService UpdateDonorAsync
PUT    /catalog/donors/add-donation/:d/:g   → DonorService AddDonotationAsync

GET    /catalog/gifts                           → GiftService GetAllGiftsAsync
GET    /catalog/gifts/:id                       → GiftService GetByIdAsync
GET    /catalog/gifts/by-name/:name            → GiftService GetGiftsByNameAsync
GET    /catalog/gifts/by-num-of-users          → GiftService GetGiftsByNumOfUsersAsync
GET    /catalog/gifts/by-donor-name            → GiftService GetGiftsByDonorNameAsync
GET    /catalog/gifts/donors/:id               → GiftService GetDonorsAsync
GET    /catalog/gifts/order-by-price-category  → GiftService GetOrderByPrice_CategoryAsync
POST   /catalog/gifts                          → GiftService CreateGiftAsync
DELETE /catalog/gifts/:id                      → GiftService DeleteGiftAsync
PUT    /catalog/gifts/:id                      → GiftService UpdateGiftAsync
```

### Order Service → /orders/*
```
GET    /orders/users             → OrderService GetAllUsersAsync
GET    /orders/gifts-by-orders   → OrderService GetGiftsOrderByOrdersAsync
GET    /orders/gifts-by-price    → OrderService GetGiftsOrderByPriceAsync
GET    /orders/by-gift           → OrderService GetOrdersByGiftAsync
POST   /orders/:giftId/:userId   → OrderService CreateOrderAsync
DELETE /orders/:id               → OrderService DeleteOrderAsync
PUT    /orders/:id               → OrderService UpdateOrderAsync
```

### Lottery Service → /lottery/*
```
POST   /lottery                  → LotteryService LotteryAsync
GET    /lottery/winners          → LotteryService GetAllWinnersAsync
GET    /lottery/revenue          → LotteryService GetAllRevenueAsync
GET    /lottery/done             → LotteryService LotteryDone
POST   /lottery/send-email/:gift → LotteryService SendingEmailAsync
```

---

## HTTP Interceptor - JWT Token Injection

The auth interceptor automatically adds JWT tokens to all requests (except public endpoints).

### Public Endpoints (No Token Required)
```
POST   /auth/login
POST   /auth/register
GET    /catalog/gifts/by-name/*
GET    /catalog/gifts/by-donor-name/*
```

### Protected Endpoints (Token Required)
All other requests automatically include:
```
Authorization: Bearer {token}
```

**Example Request Header:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│            Angular Frontend (Port 4200)                     │
├─────────────────────────────────────────────────────────────┤
│  AuthService  UserService  DonorService  GiftService        │
│  OrderService  LotteryService                               │
│                                                             │
│  All → http://localhost:5000                                │
└──────────────────────┬──────────────────────────────────────┘
                       │
                ▼──────────────────────┐
        ┌───────────────────────────────┴──────────────────┐
        │                                                  │
        ▼                                                  ▼
┌─────────────────────────────┐                  ┌─────────────────────┐
│   HTTP Interceptor          │                  │  Auth Interceptor   │
│  - Sets base URL            │                  │  - Adds JWT token   │
│  - Common headers           │                  │  - Excludes public  │
│  - Error handling           │                  │  - Clones request   │
└──────────────────┬──────────┘                  └────────────┬────────┘
                   │                                         │
                   └────────────┬────────────────────────────┘
                                │
                                ▼
                    ┌──────────────────────────┐
                    │   API Gateway             │
                    │  (Port 5000)              │
                    │  - JWT Validation        │
                    │  - Route to Service      │
                    │  - CORS Headers          │
                    │  - Centralized Logging   │
                    └───────────┬──────┬───────┬───────────┐
                                │      │       │           │
                    ┌───────────┴┐    │       │           │
                    ▼            ▼    ▼       ▼           ▼
            ┌──────────────┐ ┌────────────┐ ┌────────────┐ ┌──────────────┐
            │AuthService   │ │CatalogServ.│ │OrderServ.  │ │LotteryServ.  │
            │(5001)        │ │(5002)      │ │(5003)      │ │(5004)        │
            └──────────────┘ └────────────┘ └────────────┘ └──────────────┘
                    │            │              │              │
                    └────┬───────┴──────┬───────┴──────┬───────┘
                         │             │              │
                         ▼             ▼              ▼
                    ┌────────────────────────────────────┐
                    │   SQL Server Database               │
                    │  Mechira-sinit-microservices        │
                    │  - Auth schema                      │
                    │  - Catalog schema                   │
                    │  - Orders schema                    │
                    │  - Lottery schema                   │
                    └────────────────────────────────────┘
```

---

## Key Benefits

1. **Single Entry Point**: All requests go through gateway (port 5000)
2. **Centralized Authentication**: JWT validation at gateway
3. **Service Independence**: Frontend doesn't need service URLs
4. **Resilience**: Retry & circuit breaker at gateway level
5. **Monitoring**: Centralized logging of all API traffic
6. **Scalability**: Easy to scale/replace services without frontend changes

---

## Testing the Integration

### 1. Start All Services
```powershell
cd "d:\Documents\שרי\לימודים\microservise\server"
powershell -File start-services.ps1
```

### 2. Test Gateway Routes (Command Line)
```powershell
# Test Auth
$r = Invoke-WebRequest -Uri "http://localhost:5000/auth/login" `
  -Method Post `
  -Headers @{"Content-Type"="application/json"} `
  -Body '{"email":"john@example.com","password":"Password123!"}'

# Test Catalog
$r = Invoke-WebRequest -Uri "http://localhost:5000/catalog/gifts" -Method Get

# Test Orders (requires token)
$token = "eyJhbGc..." # from login
$headers = @{"Authorization"="Bearer $token"}
$r = Invoke-WebRequest -Uri "http://localhost:5000/orders/users" `
  -Method Get `
  -Headers $headers
```

### 3. Angular Build & Run
```bash
cd client/mecira_sinit_Angular
npm install
ng serve
# Open http://localhost:4200
```

### 4. Verify in Browser Console
```javascript
// Should show gateway calls in network tab
GET  http://localhost:5000/auth/login
GET  http://localhost:5000/catalog/gifts
GET  http://localhost:5000/orders/users
// All with Authorization header if not public
```

---

## Troubleshooting

### Issue: 404 Not Found
**Cause**: Gateway routing rule not configured or endpoint name mismatch
**Solution**: Check ocelot.json routing rules in API Gateway

### Issue: 401 Unauthorized
**Cause**: Missing or invalid JWT token
**Solution**: Login first to get valid token, check token in localStorage

### Issue: CORS Error
**Cause**: CORS policy not configured
**Solution**: Check API Gateway CORS configuration in Program.cs

### Issue: Token Not Sent
**Cause**: Endpoint is in public list or token not in localStorage
**Solution**: Check authInterceptor.ts public rules, verify login stores token as 'authToket'

---

## Files Modified

1. ✅ `auth.service.ts` - Updated apiUrl to gateway
2. ✅ `user.service.ts` - Updated apiUrl to gateway
3. ✅ `donor.service.ts` - Updated apiUrl + endpoints
4. ✅ `gift.service.ts` - Updated apiUrl + endpoints
5. ✅ `order.service.ts` - Updated apiUrl + endpoints
6. ✅ `lottery.service.ts` - Updated apiUrl + endpoints
7. ✅ `auth.interceptor.ts` - Updated public routes for gateway

---

## Next Steps

1. ✅ Phase 8: Backend services + Polly resilience - **COMPLETE**
2. ✅ Phase 9: Angular frontend gateway integration - **COMPLETE**
3. **Pending**: Full end-to-end testing with Angular UI
4. **Pending**: Database seeding with test data
5. **Pending**: Production deployment configuration
