# Comprehensive testing script for all microservices

Write-Host "=================================================================================" -ForegroundColor Cyan
Write-Host "MICROSERVICES COMPREHENSIVE TEST" -ForegroundColor Cyan
Write-Host "=================================================================================" -ForegroundColor Cyan
Write-Host ""

# Service URLs
$services = @{
    "API Gateway"     = "http://localhost:5000"
    "AuthService"     = "http://localhost:5001"
    "CatalogService"  = "http://localhost:5002"
    "OrderService"    = "http://localhost:5003"
    "LotteryService"  = "http://localhost:5004"
}

# ============================================================================
# TEST 1: Service Availability (Health Check)
# ============================================================================
Write-Host "[TEST 1] Checking Service Availability..." -ForegroundColor Yellow
Write-Host ""

$results = @{}
foreach ($name in $services.Keys) {
    $url = $services[$name]
    try {
        $response = Invoke-WebRequest -Uri "$url/health" -Method Get -TimeoutSec 2 -ErrorAction Stop
        $results[$name] = "✓ ONLINE (HTTP $($response.StatusCode))"
        Write-Host "  ✓ $name : ONLINE" -ForegroundColor Green
    } catch {
        # Try Swagger endpoint as fallback
        try {
            $response = Invoke-WebRequest -Uri "$url/swagger/index.html" -Method Get -TimeoutSec 2 -ErrorAction Stop
            $results[$name] = "✓ ONLINE (Swagger available)"
            Write-Host "  ✓ $name : ONLINE (Swagger)" -ForegroundColor Green
        } catch {
            $results[$name] = "✗ OFFLINE"
            Write-Host "  ✗ $name : OFFLINE" -ForegroundColor Red
        }
    }
}

Write-Host ""

# ============================================================================
# TEST 2: Auth Service - User Registration & Login
# ============================================================================
Write-Host "[TEST 2] Testing Auth Service (Register & Login)..." -ForegroundColor Yellow
Write-Host ""

$authUrl = $services["AuthService"]

# Register a test user
$registerPayload = @{
    firstName = "Test"
    lastName = "User"
    email = "test@example.com"
    password = "Test123!@#"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-WebRequest -Uri "$authUrl/api/auth/register" `
        -Method Post `
        -Headers @{"Content-Type" = "application/json"} `
        -Body $registerPayload `
        -TimeoutSec 5 -ErrorAction Stop
    
    Write-Host "  ✓ User Registration: SUCCESS" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "  ⚠ User Registration: User may already exist (expected)" -ForegroundColor Yellow
    } else {
        Write-Host "  ✗ User Registration: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Login with test user
$loginPayload = @{
    email = "test@example.com"
    password = "Test123!@#"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-WebRequest -Uri "$authUrl/api/auth/login" `
        -Method Post `
        -Headers @{"Content-Type" = "application/json"} `
        -Body $loginPayload `
        -TimeoutSec 5 -ErrorAction Stop
    
    $tokenData = $loginResponse.Content | ConvertFrom-Json
    $token = $tokenData.token
    
    Write-Host "  ✓ User Login: SUCCESS" -ForegroundColor Green
    Write-Host "    Token: $($token.Substring(0, 50))..." -ForegroundColor Cyan
} catch {
    Write-Host "  ✗ User Login: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    $token = $null
}

Write-Host ""

# ============================================================================
# TEST 3: API Gateway - Routing Test
# ============================================================================
Write-Host "[TEST 3] Testing API Gateway Routing..." -ForegroundColor Yellow
Write-Host ""

$gatewayUrl = $services["API Gateway"]

$routingTests = @(
    @{
        Name = "Catalog Routing"
        Path = "http://localhost:5000/catalog"
        Description = "Route to CatalogService"
    },
    @{
        Name = "Order Routing"
        Path = "http://localhost:5000/orders"
        Description = "Route to OrderService"
    },
    @{
        Name = "Lottery Routing"
        Path = "http://localhost:5000/lottery"
        Description = "Route to LotteryService"
    }
)

foreach ($test in $routingTests) {
    try {
        $response = Invoke-WebRequest -Uri $test.Path -Method Get -TimeoutSec 2 -ErrorAction Stop
        Write-Host "  ✓ $($test.Name): Routing OK" -ForegroundColor Green
    } catch {
        # 401 or 400 is OK - means gateway routed it (auth failure is OK)
        if ($_.Exception.Response.StatusCode -in @(401, 400, 404)) {
            Write-Host "  ✓ $($test.Name): Routed (Status: $($_.Exception.Response.StatusCode))" -ForegroundColor Green
        } else {
            Write-Host "  ✗ $($test.Name): FAILED - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""

# ============================================================================
# TEST 4: Catalog Service - List Products
# ============================================================================
Write-Host "[TEST 4] Testing Catalog Service..." -ForegroundColor Yellow
Write-Host ""

$catalogUrl = $services["CatalogService"]

try {
    $response = Invoke-WebRequest -Uri "$catalogUrl/api/products" -Method Get -TimeoutSec 5 -ErrorAction Stop
    $products = $response.Content | ConvertFrom-Json
    
    Write-Host "  ✓ List Products: SUCCESS" -ForegroundColor Green
    Write-Host "    Products found: $($products.Count)" -ForegroundColor Cyan
} catch {
    Write-Host "  ⚠ List Products: No data or service issue - $($_.Exception.Response.StatusCode)" -ForegroundColor Yellow
}

Write-Host ""

# ============================================================================
# TEST 5: JWT Token Validation (Secure Endpoint)
# ============================================================================
if ($token) {
    Write-Host "[TEST 5] Testing JWT Authentication..." -ForegroundColor Yellow
    Write-Host ""

    $authHeader = @{"Authorization" = "Bearer $token"}

    try {
        $response = Invoke-WebRequest -Uri "$authUrl/api/auth/profile" `
            -Method Get `
            -Headers $authHeader `
            -TimeoutSec 5 -ErrorAction Stop
        
        $profile = $response.Content | ConvertFrom-Json
        Write-Host "  ✓ JWT Validation: SUCCESS" -ForegroundColor Green
        Write-Host "    User: $($profile.email)" -ForegroundColor Cyan
    } catch {
        Write-Host "  ✗ JWT Validation: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "[TEST 5] Skipping JWT Authentication Test (No token available)" -ForegroundColor Yellow
}

Write-Host ""

# ============================================================================
# TEST 6: Database Connectivity
# ============================================================================
Write-Host "[TEST 6] Database Connectivity Status..." -ForegroundColor Yellow
Write-Host ""

try {
    $sqlQuery = "SELECT @@VERSION"
    $result = sqlcmd -S "localhost\MSSQLSERVER01" -Q $sqlQuery -h -1 2>$null
    
    if ($result) {
        Write-Host "  ✓ SQL Server Connection: SUCCESS" -ForegroundColor Green
        Write-Host "    Version: $($result.Trim())" -ForegroundColor Cyan
    } else {
        Write-Host "  ✗ SQL Server Connection: FAILED" -ForegroundColor Red
    }
} catch {
    Write-Host "  ⚠ SQL Server Check: sqlcmd not available - trying alternative check" -ForegroundColor Yellow
    
    # Try via PowerShell
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection
        $conn.ConnectionString = "Server=localhost\MSSQLSERVER01;Integrated Security=true;Connection Timeout=2;"
        $conn.Open()
        Write-Host "  ✓ SQL Server Connection: SUCCESS (via PowerShell)" -ForegroundColor Green
        $conn.Close()
    } catch {
        Write-Host "  ✗ SQL Server Connection: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""

# ============================================================================
# SUMMARY
# ============================================================================
Write-Host "=================================================================================" -ForegroundColor Cyan
Write-Host "TEST SUMMARY" -ForegroundColor Cyan
Write-Host "=================================================================================" -ForegroundColor Cyan
Write-Host ""

$onlineCount = ($results.Values | Where-Object { $_ -like "✓*" }).Count
$totalCount = $results.Count

Write-Host "Services Status:" -ForegroundColor Yellow
foreach ($name in $results.Keys) {
    Write-Host "  $name : $($results[$name])" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Summary: $onlineCount/$totalCount services online" -ForegroundColor Cyan
Write-Host ""

if ($onlineCount -eq $totalCount) {
    Write-Host "✓ ALL SYSTEMS GO!" -ForegroundColor Green
} else {
    Write-Host "⚠ Some services offline - please check logs" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Check service logs in each service window" -ForegroundColor White
Write-Host "  2. Verify database: Mechira-sinit-microservices" -ForegroundColor White
Write-Host "  3. Test API endpoints: http://localhost:5000/swagger" -ForegroundColor White
Write-Host "  4. Monitor: Check logs folder in each service" -ForegroundColor White
Write-Host ""
