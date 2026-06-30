# Quick Start Guide - Running the Microservices System

## System Overview

The system consists of:
- **API Gateway** (Port 5000) - Central routing and authentication
- **Auth Service** (Port 5001) - User authentication and JWT tokens
- **Catalog Service** (Port 5002) - Product catalog
- **Order Service** (Port 5003) - Order management
- **Lottery Service** (Port 5004) - Lottery draws and management
- **Client** (Angular) - Frontend application

---

## Prerequisites

1. **.NET 8.0 SDK** installed
   ```bash
   dotnet --version
   ```

2. **SQL Server** (LocalDB or full instance)
   - Required by all services for data persistence

3. **Visual Studio Code** or **Visual Studio**

4. **Node.js & npm** (for Angular client)

---

## Starting the System

### Option 1: Using PowerShell Script (Easiest)

From the `server` directory, run:

```powershell
cd d:\Documents\שרי\לימודים\microservise\server
powershell -File start-all-services.ps1
```

This will:
- Start all 5 services in separate terminal windows
- Show real-time logs from each service
- Auto-stop all services on script exit

### Option 2: Manual - Start Each Service in Separate Terminal

**Terminal 1 - API Gateway**
```bash
cd d:\Documents\שרי\לימודים\microservise\server\Gateway\ApiGateway
dotnet run
# Should show: "Now listening on: http://localhost:5000"
```

**Terminal 2 - Auth Service**
```bash
cd d:\Documents\שרי\לימודים\microservise\server\Services\AuthService
dotnet run
# Should show: "Now listening on: http://localhost:5001"
```

**Terminal 3 - Catalog Service**
```bash
cd d:\Documents\שרי\לימודים\microservise\server\Services\CatalogService
dotnet run
# Should show: "Now listening on: http://localhost:5002"
```

**Terminal 4 - Order Service**
```bash
cd d:\Documents\שרי\לימודים\microservise\server\Services\OrderService
dotnet run
# Should show: "Now listening on: http://localhost:5003"
```

**Terminal 5 - Lottery Service**
```bash
cd d:\Documents\שרי\לימודים\microservise\server\Services\LotteryService
dotnet run
# Should show: "Now listening on: http://localhost:5004"
```

**Terminal 6 - Angular Client**
```bash
cd d:\Documents\שרי\לימודים\microservise\client\mecira_sinit_Angular
npm install  # First time only
npm start
# Should open browser at http://localhost:4200
```

---

## Verifying Services Are Running

### Quick Health Check

```bash
# Check each service
curl http://localhost:5000/health           # API Gateway
curl http://localhost:5001/health           # Auth Service
curl http://localhost:5002/health           # Catalog Service
curl http://localhost:5003/health           # Order Service
curl http://localhost:5004/health           # Lottery Service
```

### Check Listening Ports

```powershell
# Windows - Check all ports are listening
netstat -ano | findstr :500[0-4]

# Should show:
# :5000 - API Gateway
# :5001 - Auth Service
# :5002 - Catalog Service
# :5003 - Order Service
# :5004 - Lottery Service
```

---

## Testing the Gateway

### 1. Login (Get JWT Token)

```bash
curl -X POST http://localhost:5000/auth/api/users/login \
  -H "Content-Type: application/json" \
  -d {\"email\":\"user@example.com\",\"password\":\"password123\"}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600
}
```

### 2. Use Token to Access Protected Resources

```bash
# Get orders (requires JWT)
curl -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  http://localhost:5000/orders/api/orders

# Get lottery draws (requires JWT)
curl -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  http://localhost:5000/lottery/api/draws
```

### 3. Access Public Endpoints (No Token)

```bash
# Get products (no JWT required)
curl http://localhost:5000/catalog/api/products

# Get product by ID (no JWT required)
curl http://localhost:5000/catalog/api/products/1
```

---

## Gateway Architecture

```
Angular Client (4200)
    ↓
    └─→ API Gateway (5000)
        ├─→ /auth/* → Auth Service (5001)
        ├─→ /catalog/* → Catalog Service (5002)
        ├─→ /orders/* → Order Service (5003)
        └─→ /lottery/* → Lottery Service (5004)
```

All client requests go through the gateway, which:
1. Validates JWT tokens (for protected routes)
2. Routes to the correct service
3. Logs all requests
4. Handles CORS for Angular frontend

---

## Monitoring Logs

### Gateway Logs

```bash
# View real-time console output
# Logs appear in the Gateway terminal window

# View stored logs (daily rolling)
cd d:\Documents\שרי\לימודים\microservise\server\Gateway\ApiGateway\logs
Get-Content "api-gateway-2024-01-15.txt" | Tail -50  # Last 50 lines
```

### Service Logs

```bash
# Each service logs to:
# cd server/Services/{ServiceName}/logs
cd server/Services/AuthService/logs
ls -la  # See daily log files
```

---

## Common Issues & Solutions

### Issue: Service won't start on port

**Error**: `The address is already in use`

**Solution**:
```powershell
# Find process using port
netstat -ano | findstr :5000

# Kill the process (replace PID)
taskkill /PID 1234 /F

# Or change the port in Program.cs and rerun
```

### Issue: JWT Token Validation Fails

**Error**: `401 Unauthorized - Invalid token signature`

**Solution**:
1. Verify JWT:SecretKey in appsettings.json matches AuthService
2. Check token hasn't expired (default 60 minutes)
3. Ensure token format is: `Authorization: Bearer <token>`

### Issue: Service connection timeout

**Error**: `Failed to connect to localhost:5001`

**Solution**:
1. Verify all services are running: `netstat -ano | findstr :500[0-4]`
2. Check service URLs in Gateway appsettings.json
3. Check service logs for startup errors
4. Try restarting the failed service

### Issue: CORS error from Angular

**Error**: `Access to XMLHttpRequest has been blocked by CORS policy`

**Solution**:
1. Verify CORS policy is configured in Gateway Program.cs
2. Check Angular app is sending requests to http://localhost:5000
3. Verify OPTIONS requests are routed correctly

### Issue: Database connection error

**Error**: `Cannot connect to SQL Server at...`

**Solution**:
1. Ensure SQL Server is running (LocalDB or full instance)
2. Check connection strings in appsettings.json
3. Run migrations if needed: `dotnet ef database update`
4. Verify database exists

---

## Performance Considerations

- Gateway validates tokens for every request (adds ~5ms latency)
- Services perform their own token re-validation (defense in depth)
- Ocelot caches route configurations
- Logging has minimal performance impact in production mode
- Use `appsettings.json` (not Development) in production

---

## Security Notes

⚠️ **Important for Production**:

1. **Never commit secrets** - Keep JWT:SecretKey out of source control
2. **Use HTTPS** - Configure HTTPS/TLS in production
3. **Secret management** - Store secrets in Azure Key Vault
4. **Token expiry** - Set appropriate token expiration times
5. **Rate limiting** - Consider adding rate limiting to gateway
6. **CORS policy** - Restrict origins in production

---

## Troubleshooting Commands

```powershell
# Check all services running
Get-NetTCPConnection -LocalPort 5000,5001,5002,5003,5004 -ErrorAction SilentlyContinue

# View process for specific port
Get-NetTCPConnection -LocalPort 5000 | Select-Object OwningProcess
Get-Process -Id (Get-NetTCPConnection -LocalPort 5000).OwningProcess

# Kill all services
taskkill /F /IM dotnet.exe

# Clear logs
Remove-Item "server/Gateway/ApiGateway/logs/*" -Force
Remove-Item "server/Services/*/logs/*" -Force
```

---

## What's Next?

1. ✅ **Phase 7 Complete** - API Gateway with Ocelot
2. **Phase 8** - Integration Testing
3. **Phase 9** - Authentication Testing
4. **Phase 10** - Deployment & Containerization
5. **Phase 11** - Performance Optimization
6. **Phase 12** - Production Monitoring

---

## Documentation Files

- [API Gateway README](./Gateway/ApiGateway/README.md)
- [Testing Guide](./Gateway/ApiGateway/TESTING_GUIDE.md)
- [Implementation Details](./Gateway/ApiGateway/IMPLEMENTATION.md)
- [Verification Checklist](./Gateway/ApiGateway/VERIFICATION_CHECKLIST.md)

---

**Last Updated**: 2024  
**System Status**: ✅ Ready to Run
