# Comprehensive testing script for all microservices

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MICROSERVICES COMPREHENSIVE TEST" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Service URLs
$services = @{
    "API Gateway"     = "http://localhost:5000"
    "AuthService"     = "http://localhost:5001"
    "CatalogService"  = "http://localhost:5002"
    "OrderService"    = "http://localhost:5003"
    "LotteryService"  = "http://localhost:5004"
}

# TEST 1: Service Availability
Write-Host "[TEST 1] Checking Service Availability..." -ForegroundColor Yellow
Write-Host ""

$results = @{}
foreach ($name in $services.Keys) {
    $url = $services[$name]
    try {
        $response = Invoke-WebRequest -Uri "$url/health" -Method Get -TimeoutSec 2 -ErrorAction Stop
        $results[$name] = "[OK] ONLINE (HTTP $($response.StatusCode))"
        Write-Host "  [OK] $name : ONLINE" -ForegroundColor Green
    } catch {
        try {
            $response = Invoke-WebRequest -Uri "$url/swagger/index.html" -Method Get -TimeoutSec 2 -ErrorAction Stop
            $results[$name] = "[OK] ONLINE (Swagger available)"
            Write-Host "  [OK] $name : ONLINE (Swagger)" -ForegroundColor Green
        } catch {
            $results[$name] = "[FAIL] OFFLINE"
            Write-Host "  [FAIL] $name : OFFLINE" -ForegroundColor Red
        }
    }
}

Write-Host ""

# TEST 2: Auth Service - User Registration & Login
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
    
    Write-Host "  [OK] User Registration: SUCCESS" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "  [WARN] User Registration: Already exists (expected)" -ForegroundColor Yellow
    } else {
        Write-Host "  [FAIL] User Registration: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

# Login with test user
$loginPayload = @{
    email = "test@example.com"
    password = "Test123!@#"
} | ConvertTo-Json

$token = $null
try {
    $loginResponse = Invoke-WebRequest -Uri "$authUrl/api/auth/login" `
        -Method Post `
        -Headers @{"Content-Type" = "application/json"} `
        -Body $loginPayload `
        -TimeoutSec 5 -ErrorAction Stop
    
    $tokenData = $loginResponse.Content | ConvertFrom-Json
    $token = $tokenData.token
    
    Write-Host "  [OK] User Login: SUCCESS" -ForegroundColor Green
    Write-Host "    Token: $($token.Substring(0, 50))..." -ForegroundColor Cyan
} catch {
    Write-Host "  [FAIL] User Login: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# TEST 3: API Gateway - Routing
Write-Host "[TEST 3] Testing API Gateway Routing..." -ForegroundColor Yellow
Write-Host ""

@("http://localhost:5000/catalog", "http://localhost:5000/orders", "http://localhost:5000/lottery") | ForEach-Object {
    try {
        $response = Invoke-WebRequest -Uri $_ -Method Get -TimeoutSec 2 -ErrorAction Stop
        Write-Host "  [OK] $_ : Routed" -ForegroundColor Green
    } catch {
        if ($_.Exception.Response.StatusCode -in @(401, 400, 404)) {
            Write-Host "  [OK] $_ : Routed (Status: $($_.Exception.Response.StatusCode))" -ForegroundColor Green
        } else {
            Write-Host "  [FAIL] $_ : Error" -ForegroundColor Red
        }
    }
}

Write-Host ""

# TEST 4: Catalog Service - List Products
Write-Host "[TEST 4] Testing Catalog Service..." -ForegroundColor Yellow
Write-Host ""

$catalogUrl = $services["CatalogService"]

try {
    $response = Invoke-WebRequest -Uri "$catalogUrl/api/products" -Method Get -TimeoutSec 5 -ErrorAction Stop
    $products = $response.Content | ConvertFrom-Json
    Write-Host "  [OK] List Products: SUCCESS" -ForegroundColor Green
} catch {
    Write-Host "  [WARN] List Products: Status $($_.Exception.Response.StatusCode)" -ForegroundColor Yellow
}

Write-Host ""

# TEST 5: JWT Token Validation
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
        Write-Host "  [OK] JWT Validation: SUCCESS" -ForegroundColor Green
        Write-Host "    User: $($profile.email)" -ForegroundColor Cyan
    } catch {
        Write-Host "  [FAIL] JWT Validation: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "[TEST 5] Skipping JWT Authentication Test (No token)" -ForegroundColor Yellow
}

Write-Host ""

# SUMMARY
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$onlineCount = ($results.Values | Where-Object { $_ -like "*OK*" }).Count
$totalCount = $results.Count

Write-Host "Services Status:" -ForegroundColor Yellow
foreach ($name in $results.Keys) {
    Write-Host "  $name : $($results[$name])" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Summary: $onlineCount/$totalCount services online" -ForegroundColor Cyan
Write-Host ""

if ($onlineCount -eq $totalCount) {
    Write-Host "[SUCCESS] ALL SYSTEMS GO!" -ForegroundColor Green
} else {
    Write-Host "[WARNING] Some services offline - check logs" -ForegroundColor Yellow
}

Write-Host ""
