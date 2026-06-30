# Start all microservices
Write-Host "================================" -ForegroundColor Green
Write-Host "Starting All Microservices" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""

$services = @(
    @{Name="API Gateway"; Path="Gateway\ApiGateway"; Port="5000"},
    @{Name="AuthService"; Path="Services\AuthService"; Port="5001"},
    @{Name="CatalogService"; Path="Services\CatalogService"; Port="5002"},
    @{Name="OrderService"; Path="Services\OrderService"; Port="5003"},
    @{Name="LotteryService"; Path="Services\LotteryService"; Port="5004"}
)

foreach ($service in $services) {
    Write-Host "Starting $($service.Name)..." -ForegroundColor Cyan
    $fullPath = Join-Path (Get-Location) $service.Path
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$fullPath'; dotnet run" -WindowStyle Normal
    Start-Sleep -Seconds 2
}

Write-Host "================================" -ForegroundColor Green
Write-Host "All services started!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""
Write-Host "Services on:" -ForegroundColor Yellow
Write-Host "  API Gateway:    http://localhost:5000" -ForegroundColor White
Write-Host "  AuthService:    http://localhost:5001" -ForegroundColor White
Write-Host "  CatalogService: http://localhost:5002" -ForegroundColor White
Write-Host "  OrderService:   http://localhost:5003" -ForegroundColor White
Write-Host "  LotteryService: http://localhost:5004" -ForegroundColor White
