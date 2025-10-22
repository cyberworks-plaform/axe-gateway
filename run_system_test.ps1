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
    param (
        [int]$Port
    )

    Write-Host "Checking if port $Port is in use..."
    $processId = (netstat -ano | Select-String -Pattern ":$Port\s+LISTENING" | ForEach-Object { $_ -match '(\d+)$' | Out-Null; $matches[1] } | Select-Object -First 1)

    if ($processId) {
        Write-Warning "Port $Port is in use by process with PID: $processId. Attempting to stop it."
        try {
            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
            Write-Host "Successfully stopped process with PID: $processId."
            # Give it a moment to release the port
            Start-Sleep -Seconds 2
        } catch {
            Write-Error "Failed to stop process with PID: $processId. Error: $($_.Exception.Message)"
            return $false
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

    Write-Host "Starting $ApiName on port $Port..."

    # Use `Start-Process` to run dotnet in a new, hidden window
    # We need to wrap the dotnet command in `powershell -Command` to ensure proper execution and output redirection
    $dotnetCommand = "dotnet run --project `"$ProjectPath`" --urls http://localhost:$Port"

    if ($LogFilePath) {
        $commandArgs = "-NoExit -Command `"$dotnetCommand | Out-File -FilePath `"$LogFilePath`" -Append`""
    } else {
        $commandArgs = "-NoExit -Command `"$dotnetCommand`""
    }

    $process = Start-Process powershell -ArgumentList $commandArgs -PassThru -NoNewWindow

    if ($process) {
        $processesToClean += $process.Id
        Write-Host "$ApiName started with PID: $($process.Id)"
        # Give it some time to fully initialize
        Start-Sleep -Seconds 15 # Increased sleep time

        # Assume it's running if dotnet run didn't immediately fail and we got a PID
        Write-Host "$ApiName assumed to be running on port $Port."
        return $true
    } else {
        Write-Error "Failed to start $ApiName."
        return $false
    }
}

# Cleanup function
function Stop-Processes {
    param (
        [int[]]$Pids
    )
    Write-Host "Cleaning up processes..."
    foreach ($pid in $Pids) {
        try {
            Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
            Write-Host "Stopped process with PID: $pid"
        } catch {
            Write-Warning "Could not stop process with PID: $pid. Error: $($_.Exception.Message)"
        }
    }
}

# --- Main Script Logic ---
try {
    # Pre-check and kill processes using ports
    if (-not (Stop-ProcessUsingPort -Port $mockOcrPort1)) { throw "Failed to clear port $mockOcrPort1." }
    if (-not (Stop-ProcessUsingPort -Port $mockOcrPort2)) { throw "Failed to clear port $mockOcrPort2." }
    if (-not (Stop-ProcessUsingPort -Port $gatewayPort)) { throw "Failed to clear port $gatewayPort." }

    # 1. Start Mock APIs
    if (-not (Start-DotNetApi -ProjectPath $mockOcrApiPath -Port $mockOcrPort1 -ApiName "Mock OCR API 1")) {
        throw "Mock OCR API 1 failed to start."
    }
    if (-not (Start-DotNetApi -ProjectPath $mockOcrApiPath -Port $mockOcrPort2 -ApiName "Mock OCR API 2")) {
        throw "Mock OCR API 2 failed to start."
    }

    # 2. Start Gateway API
    if (-not (Start-DotNetApi -ProjectPath $gatewayApiPath -Port $gatewayPort -ApiName "Gateway API" -LogFilePath $gatewayLogPath)) {
        throw "Gateway API failed to start."
    }

    # 3. Run k6 Test
    Write-Host "Running k6 test..."
    # Ensure k6 is in your PATH or provide the full path to k6.exe
    $k6Result = & k6 run $k6ScriptPath
    Write-Host "k6 test completed."
    Write-Output $k6Result

    # 4. Monitor Gateway API Logs for errors
    Write-Host "Analyzing Gateway API logs for errors..."
    if (Test-Path $gatewayLogPath) {
        $logContent = Get-Content $gatewayLogPath
        $errorPatterns = @("error", "fail", "exception", "500 Internal Server Error")
        $foundErrors = $false
        foreach ($pattern in $errorPatterns) {
            if ($logContent | Select-String -Pattern $pattern -CaseSensitive -Quiet) {
                Write-Warning "Found potential error pattern '$pattern' in Gateway API logs."
                $logContent | Select-String -Pattern $pattern -CaseSensitive | ForEach-Object { Write-Output $_.Line }
                $foundErrors = $true
            }
        }
        if (-not $foundErrors) {
            Write-Host "No obvious error patterns found in Gateway API logs."
        }
    } else {
        Write-Warning "Gateway API log file not found at $gatewayLogPath."
    }

} catch {
    Write-Error "An error occurred during the test execution: $($_.Exception.Message)"
} finally {
    Stop-Processes -Pids $processesToClean
    Write-Host "Test script finished."
}