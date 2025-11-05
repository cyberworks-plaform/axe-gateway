$gatewayPort = 5000
$mockOcrPort1 = 10501
$mockOcrPort2 = 10502

function Stop-ProcessUsingPort {
    param ([int]$Port)

    Write-Host "`n🔍 Checking and clearing all processes related to port $Port..."

    $processIds = (netstat -ano |
        Select-String -Pattern ":$Port\s+" |
        ForEach-Object { $_ -match '(\d+)$' | Out-Null; $matches[1] } |
        Where-Object { $_ -ne 0 -and $_ -ne 4 } |
        Select-Object -Unique)

    if ($processIds.Count -gt 0) {
        foreach ($processId in $processIds) {
            Write-Warning "Port $Port is in use by process with PID: $processId. Attempting to terminate its process tree."
            try {
                # Use taskkill to terminate the process and its children
                $taskkillResult = cmd /c "taskkill /F /T /PID $processId 2>&1"
                if ($LASTEXITCODE -ne 0) {
                    Write-Warning "taskkill for PID $processId failed: $taskkillResult"
                } else {
                    Write-Host "Successfully terminated process tree for PID: $processId (Port: $Port)."
                }
                # Give it a moment to release the port
                Start-Sleep -Seconds 2
            } catch {
                Write-Error "Failed to terminate process tree for PID: $processId. Error: $($_.Exception.Message)"
                return $false
            }
        }
    } else {
        Write-Host "Port $Port is free."
    }
    return $true
}

Stop-ProcessUsingPort -Port $mockOcrPort1
Stop-ProcessUsingPort -Port $mockOcrPort2
Stop-ProcessUsingPort -Port $gatewayPort

Write-Host "Starting mock OCR API on port 10501..."
Start-Process -FilePath "dotnet" -ArgumentList "run --project D:\project\cyberworks-github\axe-gateway\MockOcrApi --urls http://localhost:10501"
Start-Sleep -Seconds 2

Write-Host "Starting mock OCR API on port 10502..."
Start-Process -FilePath "dotnet" -ArgumentList "run --project D:\project\cyberworks-github\axe-gateway\MockOcrApi --urls http://localhost:10502" 
Start-Sleep -Seconds 2

Write-Host "Starting main gateway project on port 5000..."
Start-Process -FilePath "dotnet" -ArgumentList "run --project D:\project\cyberworks-github\axe-gateway\Ce.Gateway.Api --urls http://localhost:5000"

Write-Host "Done! Open http://localhost:5000/monitor to test"
