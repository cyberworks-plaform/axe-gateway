param (
    [int]$K6VUs = 1 # Default to 1 VU if not specified
)

# Define paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$gatewayApiPath = Join-Path $scriptDir "Ce.Gateway.Api"
$mockOcrApiPath = Join-Path $scriptDir "MockOcrApi"
$k6ScriptPath = Join-Path $scriptDir "k6_test.js"
$gatewayLogPath = Join-Path $scriptDir "gateway_api.log"

# Define ports
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

# Array to hold process IDs for cleanup
$processesToClean = @()

# Function to start a .NET API
function Start-DotNetApi {
    param (
        [string]$ProjectPath,
        [int]$Port,
        [string]$ApiName,
        [string]$LogFilePath = $null
    )

    Write-Host "`n🚀 Starting $ApiName on port $Port..."

    $env:ASPNETCORE_URLS = "http://localhost:$Port"
    $env:ASPNETCORE_ENVIRONMENT = "Development"

    $arguments = "run --no-launch-profile --project `"$ProjectPath`" --urls http://localhost:$Port"
    if ($LogFilePath) {
        $arguments += " >> `"$LogFilePath`" 2>&1"
    }

    # Khởi động tiến trình và không chiếm console
    $process = Start-Process dotnet -ArgumentList $arguments -PassThru -WindowStyle Hidden -ErrorAction SilentlyContinue

    Remove-Item Env:ASPNETCORE_URLS -ErrorAction SilentlyContinue
    Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue

    if (-not $process) {
        throw ("❌ Failed to start {0} on port {1}" -f $ApiName, $Port)
    }

    Write-Host "⏳ Waiting for $ApiName to initialize (PID: $($process.Id))..."
    Start-Sleep -Seconds 3

    # Thử kiểm tra nhiều cách
    $maxAttempts = 30
    $isReady = $false
    for ($i = 0; $i -lt $maxAttempts; $i++) {
        # 1️⃣ Ưu tiên Test-NetConnection nếu có
        try {
            $conn = Test-NetConnection -ComputerName "localhost" -Port $Port -WarningAction SilentlyContinue
            if ($conn.TcpTestSucceeded) {
                $isReady = $true
                break
            }
        } catch { }

        # 2️⃣ Fallback bằng netstat nếu Test-NetConnection bị chặn
        $isListening = (netstat -ano | Select-String -Pattern ":$Port\s+LISTENING")
        if ($isListening) {
            $isReady = $true
            break
        }

        Start-Sleep -Seconds 1
    }

    if ($isReady) {
        Write-Host ("✅ {0} is listening on port {1} (PID: {2})" -f $ApiName, $Port, $process.Id)
        Start-Sleep -Seconds 1
        return $process
    } else {
        throw ("❌ Failed to confirm {0} is listening on port {1} after multiple attempts. Check logs for details." -f $ApiName, $Port)
    }
}
# ==========================
# 🧹 CLEANUP HANDLER
# ==========================

$processesToClean = @()

function Cleanup {
    Write-Host "`n🧹 Cleaning up all processes and ports..."
    foreach ($procId in $processesToClean) {
        try {
            $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
            if ($null -ne $proc) {
                Write-Host ("🧨 Terminating PID {0} ({1})..." -f $procId, $proc.ProcessName)
                Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
                cmd /c "taskkill /F /T /PID $procId" | Out-Null
                Start-Sleep -Milliseconds 500
            } else {
                Write-Host ("PID {0} already terminated." -f $procId)
            }
        } catch {
            $errMsg = $_.Exception.Message
            Write-Warning ("⚠️ Unable to terminate PID {0}: {1}" -f $procId, $errMsg)
        }
    }

    Stop-ProcessUsingPort $gatewayPort
    Stop-ProcessUsingPort $mockOcrPort1
    Stop-ProcessUsingPort $mockOcrPort2

    Write-Host "✅ Cleanup completed."
}

Register-EngineEvent PowerShell.Exiting -Action { Cleanup }

# ==========================
# 🚦 MAIN FLOW
# ==========================

Write-Host "`n==============================="
Write-Host "🧪 Starting Automated Test Setup"
Write-Host "==============================="

Stop-ProcessUsingPort $gatewayPort
Stop-ProcessUsingPort $mockOcrPort1
Stop-ProcessUsingPort $mockOcrPort2

$mock1 = Start-DotNetApi -ProjectPath $mockOcrApiPath -Port $mockOcrPort1 -ApiName "Mock OCR API 1"
$mock2 = Start-DotNetApi -ProjectPath $mockOcrApiPath -Port $mockOcrPort2 -ApiName "Mock OCR API 2"
$gateway = Start-DotNetApi -ProjectPath $gatewayApiPath -Port $gatewayPort -ApiName "Gateway API" -LogFilePath $gatewayLogPath

$processesToClean += @($mock1.Id, $mock2.Id, $gateway.Id)

# ==========================
# ▶️ RUN LOAD TEST (K6)
# ==========================
if (Test-Path $k6ScriptPath) {
    Write-Host "`n⚙️ Running K6 load test with $K6VUs Virtual Users..."
    & k6 run --vus $K6VUs --iterations $K6VUs $k6ScriptPath
} else {
    Write-Warning "⚠️ K6 script not found: $k6ScriptPath"
}



Write-Host "`n🎯 All tests finished successfully!"
Write-Host "You can now safely run Visual Studio debugging."
Start-Sleep -Seconds 5