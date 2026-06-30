# System Architecture - Phase 7 Complete

## 🏗️ Complete Microservices Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                               │
│                         CLIENT LAYER                                         │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                                                                      │   │
│  │  Angular App (Port 4200)                                            │   │
│  │  ┌──────────────────────────────────────────────────────────────┐  │   │
│  │  │  Dashboard | Products | Orders | Lottery Draws              │  │   │
│  │  └──────────────────────────────────────────────────────────────┘  │   │
│  │                                                                      │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                │                                             │
│                                │ HTTP/JSON                                   │
│                                │ CORS Enabled                                │
│                                ▼                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                 │
                                 │
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                               │
│                      API GATEWAY LAYER (Phase 7) ✅                          │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                                                                      │   │
│  │  Ocelot API Gateway (Port 5000)                                    │   │
│  │  ┌────────────────────────────────────────────────────────────┐   │   │
│  │  │                                                            │   │   │
│  │  │  ┌──────────────────────────────────────────────────────┐ │   │   │
│  │  │  │  Serilog Request Logging                            │ │   │   │
│  │  │  └──────────────────────────────────────────────────────┘ │   │   │
│  │  │  ┌──────────────────────────────────────────────────────┐ │   │   │
│  │  │  │  CORS Policy (AllowAnyOrigin)                       │ │   │   │
│  │  │  └──────────────────────────────────────────────────────┘ │   │   │
│  │  │  ┌──────────────────────────────────────────────────────┐ │   │   │
│  │  │  │  JWT Validation Middleware                          │ │   │   │
│  │  │  │  • Validates Bearer token                           │ │   │   │
│  │  │  │  • Checks signature (HS256)                         │ │   │   │
│  │  │  │  • Validates expiry                                 │ │   │   │
│  │  │  │  • Returns 401 if invalid                           │ │   │   │
│  │  │  └──────────────────────────────────────────────────────┘ │   │   │
│  │  │  ┌──────────────────────────────────────────────────────┐ │   │   │
│  │  │  │  Ocelot Routing Engine                              │ │   │   │
│  │  │  │  • Routes to correct microservice                   │ │   │   │
│  │  │  │  • Forwards request                                 │ │   │   │
│  │  │  │  • Returns response to client                       │ │   │   │
│  │  │  └──────────────────────────────────────────────────────┘ │   │   │
│  │  │                                                            │   │   │
│  │  └────────────────────────────────────────────────────────────┘   │   │
│  │                                                                      │   │
│  │  Routes:                                                             │   │
│  │  • /auth/*      → Port 5001 (requires JWT for protected endpoints) │   │
│  │  • /catalog/*   → Port 5002 (public - no JWT required)            │   │
│  │  • /orders/*    → Port 5003 (requires JWT)                        │   │
│  │  • /lottery/*   → Port 5004 (requires JWT)                        │   │
│  │                                                                      │   │
│  │  Configuration:                                                      │   │
│  │  • ocelot.json        - Route definitions                          │   │
│  │  • appsettings.json   - JWT secrets & service URLs                 │   │
│  │  • Program.cs         - Middleware pipeline                         │   │
│  │  • Logs: daily rolling files (logs/ directory)                     │   │
│  │                                                                      │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
    │              │                 │                │
    │              │                 │                │
    │ /auth        │ /catalog        │ /orders        │ /lottery
    ▼              ▼                 ▼                ▼
┌──────────┐  ┌──────────┐  ┌──────────────┐  ┌──────────────┐
│  AUTH    │  │ CATALOG  │  │    ORDER     │  │   LOTTERY    │
│ SERVICE  │  │ SERVICE  │  │   SERVICE    │  │   SERVICE    │
│ :5001    │  │  :5002   │  │    :5003     │  │    :5004     │
└──────────┘  └──────────┘  └──────────────┘  └──────────────┘
    │              │                 │                │
    │  Features:   │  Features:      │  Features:    │  Features:
    │  • Login     │  • Products     │  • Create     │  • Draw
    │  • Register  │  • Categories   │    Orders     │    Management
    │  • Profile   │  • Search       │  • Track      │  • View Results
    │  • JWT Gen   │  • Reviews      │    Orders     │  • Statistics
    │              │                 │  • Update     │
    │              │                 │                │
    └──────────────┴─────────────────┴────────────────┘
              │
              │ All services share database
              │
              ▼
    ┌────────────────────────┐
    │   SQL Server Database  │
    │                        │
    │  • Users (Auth)        │
    │  • Products (Catalog)  │
    │  • Orders (Orders)     │
    │  • Draws (Lottery)     │
    │  • Shared Tables       │
    │                        │
    └────────────────────────┘
```

---

## 🔐 Request Flow with JWT Validation

```
CLIENT REQUEST
    │
    ▼ GET http://localhost:5000/orders/api/orders
    │ Headers: { Authorization: "Bearer eyJ..." }
    │
    ▼ GATEWAY (Port 5000)
    │
    ├─► Serilog Logging: Record request
    │
    ├─► CORS Check: Allow cross-origin
    │
    ├─► JWT Validation Middleware
    │   ├─ Extract token from Authorization header
    │   ├─ Validate signature (HS256)
    │   ├─ Validate expiry
    │   ├─ Validate issuer
    │   └─► ✅ Token valid? → Continue
    │       ❌ Token invalid? → 401 Unauthorized
    │
    ├─► Authentication: Verify Bearer scheme
    │
    ├─► Authorization: Check permissions
    │
    ├─► Ocelot Routing Engine
    │   └─ Match /orders/* → localhost:5003
    │
    ▼ Forward to Order Service (Port 5003)
    │ Headers: { Authorization: "Bearer eyJ..." } (passed through)
    │
    ▼ ORDER SERVICE
    │ ├─ Process request
    │ ├─ Validate JWT again (defense in depth)
    │ ├─ Query database
    │ └─ Return response
    │
    ▼ Response returned to GATEWAY
    │
    ▼ Logging: Record response
    │
    ▼ Response sent to CLIENT
    │
    ✅ Client receives data
```

---

## 🔑 JWT Token Lifecycle

```
┌────────────────────────────────────────────────────────┐
│                 JWT TOKEN LIFECYCLE                     │
└────────────────────────────────────────────────────────┘

STEP 1: USER LOGIN
┌─────────────┐
│   Client    │
│   (Angular) │
└──────┬──────┘
       │ POST /auth/users/login
       │ { email, password }
       ▼
┌──────────────────────┐
│  API Gateway :5000   │ (No JWT needed for login)
└──────┬───────────────┘
       │ Route to Auth Service
       ▼
┌──────────────────────────┐
│ Auth Service :5001       │
│ ├─ Validate credentials  │
│ ├─ Create JWT token      │
│ └─ Sign with secret key  │
└──────┬───────────────────┘
       │ Return token
       ▼
┌──────────────────────┐
│  API Gateway :5000   │
└──────┬───────────────┘
       │ Return token to client
       ▼
┌─────────────┐
│   Client    │
│   Stores    │ ← "eyJhbGciOiJIUzI1NiIs..."
│   Token     │
└─────────────┘

STEP 2: USE TOKEN TO ACCESS PROTECTED RESOURCE
┌─────────────┐
│   Client    │
│   Sends:    │ Authorization: Bearer <token>
│   Request   │ GET /orders/api/orders
└──────┬──────┘
       ▼
┌──────────────────────┐
│  API Gateway :5000   │
│  ├─ Extract token    │
│  ├─ Validate sig     │ ← Signed with same secret
│  ├─ Check expiry     │ ← Not expired?
│  ├─ Validate issuer  │ ← "AuthService"
│  └─► ✅ Valid?       │
└──────┬───────────────┘
       │ ✅ Token valid → Forward request
       ▼
┌──────────────────────────┐
│ Order Service :5003      │
│ ├─ Receive JWT token     │
│ ├─ Validate again        │ (Defense in depth)
│ ├─ Check user claims     │
│ ├─ Query database        │
│ └─ Return orders         │
└──────┬───────────────────┘
       │ Response
       ▼
┌──────────────────────┐
│  API Gateway :5000   │
└──────┬───────────────┘
       │ Return to client
       ▼
┌─────────────┐
│   Client    │
│   Displays  │ Orders loaded successfully
│   Orders    │
└─────────────┘

STEP 3: TOKEN EXPIRED
┌─────────────┐
│   Client    │ (Later, after 60 minutes)
│   Sends:    │ Authorization: Bearer <expired-token>
│   Request   │
└──────┬──────┘
       ▼
┌──────────────────────┐
│  API Gateway :5000   │
│  ├─ Extract token    │
│  ├─ Validate sig     │ ✅
│  └─ Check expiry     │ ❌ EXPIRED!
└──────┬───────────────┘
       │ ❌ Token expired
       ▼
┌──────────────────────┐
│   Error Response     │
│   401 Unauthorized   │
│   "Token expired"    │
└──────────────────────┘
       │
       ▼
┌─────────────┐
│   Client    │
│   Redirect  │ → Login again to get new token
│   to Login  │
└─────────────┘
```

---

## 📊 Configuration Architecture

```
┌────────────────────────────────────────────────────────┐
│         GATEWAY CONFIGURATION FILES                     │
└────────────────────────────────────────────────────────┘

┌─ appsettings.json (Production Config)
│  ├─ Jwt
│  │  ├─ SecretKey: "32+ char key" ⚠️ CRITICAL
│  │  ├─ Issuer: "AuthService"
│  │  ├─ Audience: "MicroservicesApi"
│  │  └─ ExpiryMinutes: 60
│  │
│  └─ ServiceUrls
│     ├─ AuthService: http://localhost:5001
│     ├─ CatalogService: http://localhost:5002
│     ├─ OrderService: http://localhost:5003
│     └─ LotteryService: http://localhost:5004

┌─ appsettings.Development.json (Dev Enhanced Logging)
│  └─ Logging
│     ├─ Default: Debug
│     ├─ Ocelot: Debug
│     └─ ApiGateway.Middleware: Debug

┌─ ocelot.json (Route Configuration)
│  └─ Routes[]
│     ├─ /auth/{everything} → :5001
│     ├─ /catalog/{everything} → :5002
│     ├─ /orders/{everything} → :5003
│     └─ /lottery/{everything} → :5004

┌─ Program.cs (Middleware Pipeline)
│  ├─ Ocelot Registration
│  ├─ Serilog Setup
│  ├─ JWT Bearer Config
│  ├─ CORS Setup
│  └─ Middleware Order (CRITICAL)
│     1. Serilog
│     2. CORS
│     3. JWT Validation
│     4. Authentication
│     5. Authorization
│     6. Ocelot
```

---

## 🔄 Service Discovery & Communication

```
┌────────────────────────────────────────────────────────┐
│     SERVICE-TO-SERVICE COMMUNICATION PATTERN            │
└────────────────────────────────────────────────────────┘

Client Request Flow:
    Client → Gateway → Service A → Service B

Example: Place Order (requires user + product verification)
    
    Client
      │ POST /orders/api/orders
      │ { productId: 5, quantity: 2, Authorization: Bearer token }
      ▼
    Gateway :5000
      │ 1. Validate JWT
      │ 2. Route to Order Service
      ▼
    Order Service :5003
      │ 1. Validate JWT again
      │ 2. Create order
      │ 3. Maybe call Catalog Service to verify product exists
      │    (Optional: Service-to-Service calls with shared token)
      │ 4. Store in database
      ▼
    Response: Order Created
      │
      ▼
    Gateway :5000
      │ Forward response
      ▼
    Client
      │ Order confirmation
      ▼
    Complete ✅
```

---

## 📝 Logging Architecture

```
┌────────────────────────────────────────────────────────┐
│            LOGGING FLOW & STORAGE                       │
└────────────────────────────────────────────────────────┘

Request
   │
   ▼
Serilog Middleware
   │
   ├─► Console Output
   │   [2024-01-15 14:23:45] [INF] GET /orders/api/orders
   │
   └─► File Output (Daily Rolling)
       logs/api-gateway-2024-01-15.txt
       logs/api-gateway-2024-01-16.txt
       logs/api-gateway-2024-01-17.txt
       │
       └─ Retention: Indefinite (manual cleanup)

Log Levels:
  🔴 Error   - System errors, critical failures
  🟠 Warning - Auth failures, token issues
  🟢 Info    - Successful operations, request flow
  🔵 Debug   - Detailed info (dev only)
```

---

## 🎯 Security Model

```
┌────────────────────────────────────────────────────────┐
│           MULTI-LAYER SECURITY MODEL                   │
└────────────────────────────────────────────────────────┘

Layer 1: Gateway Level
┌─────────────────────────────────────┐
│ • JWT validation (gateway)          │
│ • Bearer token extraction           │
│ • Signature verification (HS256)    │
│ • Expiry checking                   │
│ • Issuer validation                 │
│ • Error handling (no info leaks)    │
└─────────────────────────────────────┘

Layer 2: Service Level
┌─────────────────────────────────────┐
│ • JWT re-validation (defense)       │
│ • Authorization checks              │
│ • Role-based access control         │
│ • Data validation                   │
│ • SQL injection prevention          │
└─────────────────────────────────────┘

Layer 3: Database Level
┌─────────────────────────────────────┐
│ • Encrypted connections (SQL)       │
│ • Password hashing                  │
│ • Audit logging                     │
│ • Row-level security (future)       │
└─────────────────────────────────────┘

Result: Defense in Depth ✅
   - No single point of failure
   - Multiple validation layers
   - Comprehensive audit trail
```

---

## ✅ Verification Checklist

```
PHASE 7 IMPLEMENTATION VERIFICATION

Architecture
  ✅ Gateway routes to all 4 services
  ✅ JWT validation before forwarding
  ✅ CORS enabled for frontend
  ✅ Centralized logging

Configuration
  ✅ appsettings.json with JWT config
  ✅ ocelot.json with all routes
  ✅ Program.cs with correct middleware order
  ✅ Environment-specific settings

Security
  ✅ JWT token validation
  ✅ HS256 signature verification
  ✅ Token expiry checking
  ✅ Public endpoints excluded

Logging
  ✅ Serilog integration
  ✅ Daily rolling files
  ✅ Appropriate log levels
  ✅ Request tracking

Testing
  ✅ Routes verified
  ✅ JWT validation working
  ✅ Error responses correct
  ✅ CORS functional

Documentation
  ✅ README.md
  ✅ IMPLEMENTATION.md
  ✅ TESTING_GUIDE.md
  ✅ VERIFICATION_CHECKLIST.md
  ✅ QUICK_START_GUIDE.md
  ✅ PHASE7_SUMMARY.md (this file)
```

---

**Architecture finalized and verified ✅**  
**Ready for Phase 8: Integration Testing**
