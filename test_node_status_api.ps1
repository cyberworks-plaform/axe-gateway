# Test Node Status API
$baseUrl = "http://localhost:5000"
$now = [DateTime]::UtcNow
$startTime = $now.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
$endTime = $now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")

Write-Host "Testing Node Status with Metrics API..." -ForegroundColor Cyan
Write-Host "StartTime: $startTime" -ForegroundColor Yellow
Write-Host "EndTime: $endTime" -ForegroundColor Yellow
Write-Host ""

$url = "$baseUrl/api/dashboard/nodestatuswithmetrics?startTime=$startTime&endTime=$endTime"
Write-Host "URL: $url" -ForegroundColor Green
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri $url -Method Get
    Write-Host "Response:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}
