
# PowerShell script to build, run, and test the Ce.Gateway.Api project

$projectPath = "D:\project\cyberworks-github\axe-gateway\Ce.Gateway.Api"
$apiPort = 5000
$apiUrl = "http://localhost:$apiPort/api/monitor/logs"
$apiProcess = $null

function Start-Api {
    param (
        [string]$Path
    )
    Write-Host "Starting API from $Path..."
    $scriptBlock = {
        param($Path)
        Set-Location $Path
        dotnet run --urls "http://localhost:5000"
    }
    $job = Start-Job -ScriptBlock $scriptBlock -ArgumentList $Path
    return $job
}

function Stop-Api {
    param (
        [System.Management.Automation.Job]$Job
    )
    if ($Job -ne $null) {
        Write-Host "Stopping API process (Job ID: $($Job.Id))..."
        # Get the process ID from the job's child processes
        $processId = $Job.ChildJobs | Select-Object -ExpandProperty ChildJobs | Select-Object -ExpandProperty ProcessId
        if ($processId) {
            try {
                Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
                Write-Host "Killed process with ID $processId."
            } catch {
                Write-Warning "Could not kill process $processId. An error occurred."
            }
        }
        Stop-Job -Job $Job -ErrorAction SilentlyContinue
        Remove-Job -Job $Job -ErrorAction SilentlyContinue
    }
}

try {
    Write-Host "Navigating to project path: $projectPath"
    Set-Location $projectPath

    Write-Host "Cleaning project..."
    $cleanResult = dotnet clean
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Project clean failed!"
        exit 1
    }
    Write-Host "Clean successful."

    Write-Host "Building project..."
    $buildOutput = dotnet build 2>&1
    Write-Host $buildOutput
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Project build failed! See output above for details."
        exit 1
    }
    Write-Host "Build successful."

    $apiJob = Start-Api -Path $projectPath
    $apiProcess = $apiJob # Store the job object

    Write-Host "Waiting for API to start (20 seconds)..."
    Start-Sleep -Seconds 20

    Write-Host "Sending GET request to $apiUrl..."
    $response = Invoke-WebRequest -Uri $apiUrl -Method Get -ErrorAction SilentlyContinue

    if ($response -and $response.StatusCode) {
        Write-Host "API Test Successful!"
        Write-Host "Status Code: $($response.StatusCode)"
        Write-Host "Response (first 500 chars): $($response.Content.Substring(0, [System.Math]::Min(500, $response.Content.Length)))"
        # You can add more assertions here, e.g., check for specific content in $response
    } else {
        Write-Error "API Test Failed! Could not get a response from $apiUrl. Status Code: $($response.StatusCode)"
        if ($response) {
            Write-Error "Response Content: $($response.Content)"
        }# Check if the job produced any errors
        $jobErrors = Receive-Job -Job $apiJob -ErrorVariable jobErr -ErrorAction SilentlyContinue
        if ($jobErrors) {
            Write-Error "API Job Output/Errors:"
            $jobErrors | ForEach-Object { Write-Error $_ }
        }
        if ($jobErr) {
            Write-Error "API Job ErrorVariable:"
            $jobErr | ForEach-Object { Write-Error $_ }
        }
        exit 1
    }

} catch {
    Write-Error "An unexpected error occurred: $($_.Exception.Message)"
    exit 1
} finally {
    Stop-Api -Job $apiJob
    Write-Host "Script finished."
}
