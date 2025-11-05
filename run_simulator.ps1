# Ce.Gateway Request Simulator Runner
# This script runs the request simulator to generate fake data for testing

Write-Host "=== Ce.Gateway Request Simulator ===" -ForegroundColor Cyan
Write-Host ""

$simulatorPath = "Ce.Gateway.Simulator"
$gatewayDbPath = "Ce.Gateway.Api\data\gateway.development.db"

# Check if gateway database exists
if (-not (Test-Path $gatewayDbPath)) {
    Write-Host "[ERROR] Gateway database not found at: $gatewayDbPath" -ForegroundColor Red
    Write-Host "Please run the gateway application first to create the database." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Run gateway with: .\run_gateway_manual.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host "[OK] Database found: $gatewayDbPath" -ForegroundColor Green
Write-Host ""

# Build simulator
Write-Host "[BUILD] Building simulator..." -ForegroundColor Yellow
Push-Location $simulatorPath
dotnet build --configuration Release --verbosity quiet
$buildResult = $LASTEXITCODE
Pop-Location

if ($buildResult -ne 0) {
    Write-Host "[ERROR] Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Build successful" -ForegroundColor Green
Write-Host ""

# Run simulator
Write-Host "[START] Starting simulator..." -ForegroundColor Cyan
Write-Host ""

Push-Location $simulatorPath
dotnet run --configuration Release
Pop-Location
