# Final, robust version of the test script.

# PRE-TEST CLEANUP
Write-Host "Deleting old database file to ensure a clean migration..."
Remove-Item -Path "D:\project\cyberworks-github\axe-gateway\Ce.Gateway.Api\Data\gateway.db" -ErrorAction SilentlyContinue

# Step 1 & 2: Start background processes and wait
try {
    Write-Host "Starting mock OCR API on port 10501..."
    Start-Process -FilePath "dotnet" -ArgumentList "run --project D:\project\cyberworks-github\axe-gateway\MockOcrApi --urls http://localhost:10501" -WindowStyle Hidden
    
    Write-Host "Starting mock OCR API on port 10502..."
    Start-Process -FilePath "dotnet" -ArgumentList "run --project D:\project\cyberworks-github\axe-gateway\MockOcrApi --urls http://localhost:10502" -WindowStyle Hidden
    
    Write-Host "Waiting 5 seconds for mock APIs to stabilize..."
    Start-Sleep -Seconds 5
    
    $gatewayLog = "D:\project\cyberworks-github\axe-gateway\gateway_startup.log"
    Write-Host "Starting main gateway project and redirecting output to $gatewayLog..."
    Start-Process -FilePath "dotnet" -ArgumentList "run --project D:\project\cyberworks-github\axe-gateway\Ce.Gateway.Api" -RedirectStandardOutput $gatewayLog -WindowStyle Hidden
    
    Write-Host "Waiting 15 seconds for the gateway to perform initial migration and start up..."
    Start-Sleep -Seconds 15
}
catch {
    Write-Host "FATAL: Error starting processes: $_"
    Get-Process -Name dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
    exit 1
}

# Step 3: Send parallel requests with corrected URLs
Write-Host "Preparing to send 50 test requests with corrected URLs..."
$urls = @()
for ($i = 0; $i -lt 25; $i++) {
    # Corrected URLs without the extra '/ocr' segment
    $urls += "http://localhost:5000/gateway/axsdk-api/manual/cccd"
    $urls += "http://localhost:5000/gateway/axsdk-api/auto/hopdong"
}

$scriptBlock = {
    param($url)
    
    Start-Sleep -Milliseconds (Get-Random -Minimum 1000 -Maximum 3000)
    
    $status = 0
    $latency = 0
    
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30
        $status = $response.StatusCode
    }
    catch [System.Net.WebException] {
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        else { $status = -1 }
    }
    catch { $status = -1 }
    $timer.Stop()
    $latency = $timer.Elapsed.TotalMilliseconds
    
    return [PSCustomObject]@{ StatusCode = $status; Latency = $latency }
}

$jobs = @()
foreach ($url in $urls) {
    $jobs += Start-Job -ScriptBlock $scriptBlock -ArgumentList $url
}

Write-Host "All 50 request jobs started. Waiting for completion..."
$requestResults = $jobs | Wait-Job | Receive-Job
Remove-Job -State Completed
Write-Host "All 50 test requests have been completed."

# Step 4: Compile and run DbReader to query the database
$dbLogs = @()
try {
    Write-Host "Compiling DbReader utility..."
    $buildResult = dotnet build "D:\project\cyberworks-github\axe-gateway\DbReader\DbReader.csproj" -c Release
    if ($LASTEXITCODE -ne 0) { throw "DbReader compilation failed." }

    Write-Host "Querying database with DbReader utility..."
    $dbPath = "D:\project\cyberworks-github\axe-gateway\Ce.Gateway.Api\Data\gateway.db"
    $dbReaderExe = "D:\project\cyberworks-github\axe-gateway\DbReader\bin\Release\net6.0\DbReader.exe"
    
    # Ensure the database file exists before querying
    if (-not (Test-Path $dbPath)) { throw "Database file not found at $dbPath" }

    $csvOutput = & $dbReaderExe $dbPath
    if ($LASTEXITCODE -ne 0) { throw "DbReader execution failed." }

    # The first line of CSV is the header, so we skip it for data conversion
    $dbLogs = $csvOutput | ConvertFrom-Csv
}
catch {
    Write-Host "DATABASE VERIFICATION FAILED: $_ "
}

# Step 5: Generate and print the report
Write-Host "---"
Write-Host "TEST AUTOMATION REPORT"
Write-Host "---"

$totalRequests = $requestResults.Count
$totalLogs = $dbLogs.Count
$errorCount = ($requestResults | Where-Object { $_.StatusCode -ne 200 }).Count
$errorRate = if ($totalRequests -gt 0) { [math]::Round(($errorCount / $totalRequests) * 100, 2) } else { 0 }
$avgLatency = if ($totalRequests -gt 0) { ($requestResults.Latency | Measure-Object -Average).Average } else { 0 }
$node10501Count = ($dbLogs | Where-Object { $_.DownstreamNode -like "*10501" }).Count
$node10502Count = ($dbLogs | Where-Object { $_.DownstreamNode -like "*10502" }).Count
$errorLogsCount = ($dbLogs | Where-Object { $_.StatusCode -ne 200 }).Count

Write-Host "Request Summary:"
Write-Host "- Total Requests Sent: $totalRequests"
Write-Host "- Success (200 OK): $($totalRequests - $errorCount)"
Write-Host "- Failures (non-200): $errorCount"
Write-Host "- Average Latency: $($avgLatency.ToString('F2')) ms"
Write-Host ""
Write-Host "Log Verification Summary:"
Write-Host "- Total Logs Recorded in DB: $totalLogs"
Write-Host "- Logs with Errors (non-200): $errorLogsCount"
Write-Host "- Load Distribution:"
Write-Host "  - Logs for Node 10501: $node10501Count"
Write-Host "  - Logs for Node 10502: $node10502Count"
Write-Host "---"

# Step 6: Cleanup
Write-Host "Test complete. Stopping all dotnet processes..."
Get-Process -Name dotnet -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "Cleanup finished."