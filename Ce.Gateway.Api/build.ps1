# build.ps1

$projectFile = "./Ce.Gateway.Api.csproj"
$configuration = "Release"
$publishDir = "./publish"
$timestamp = Get-Date -Format "yyyyMMddHHmm"
$targetFolder = "D:/cw_publish/Gateway" # use to store zip file

# Read project info
Write-Host "📦 Reading project info from $projectFile..."

[xml]$csprojXml = Get-Content $projectFile
$version = $csprojXml.Project.PropertyGroup.Version.Trim()
$framework = $csprojXml.Project.PropertyGroup.TargetFramework.Trim()

$appNameNode = $csprojXml.Project.PropertyGroup.AssemblyName
$appName = if ($appNameNode) { $appNameNode.Trim() } else { [System.IO.Path]::GetFileNameWithoutExtension($projectFile) }

# Get Git commit hash if available
try {
    $gitHash = (git rev-parse --short HEAD).Trim()
    if (-not $gitHash) { $gitHash = "nogit" }
} catch {
    $gitHash = "nogit"
}
$IsWinPlatform = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
    [System.Runtime.InteropServices.OSPlatform]::Windows
)

Write-Host "📌 Version: $version"
Write-Host "📌 Framework: $framework"
Write-Host "📌 Timestamp: $timestamp"
Write-Host "📌 Git Commit: $gitHash"
Write-Host "📁 Publish Output: $publishDir"
Write-Host "📌 Is-WindowOS: $IsWinPlatform"

# Prepare ZIP info
$zipFileName = "$appName-v$version-update-$timestamp-g$gitHash.zip"
$zipPath = Join-Path $publishDir $zipFileName

# Determine 7-Zip path
if ($IsWinPlatform) {
    $sevenZip = "C:/Program Files/7-Zip/7z.exe"
} else {
    $sevenZip = "7z"  # assumes p7zip-full installed on Linux
}

# Clean & Build
Write-Host "`n🧹 Cleaning project..."
dotnet clean $projectFile

Write-Host "`n🔨 Building project..."
dotnet build $projectFile -c $configuration 

# Reset thư mục publish để tránh lồng nhau
if (Test-Path $publishDir) {
    Write-Host "🧹 Cleaning old publish directory: $publishDir"
    Remove-Item -Recurse -Force $publishDir
}
New-Item -ItemType Directory -Path $publishDir | Out-Null

Write-Host "`n🚀 Publishing project..."
dotnet publish $projectFile -c $configuration -o $publishDir

# Rename config for safety
$appsettingsPath = Join-Path $publishDir "appsettings.json"
$appsettingsRenamed = Join-Path $publishDir "appsettings.publish.json"
if (Test-Path $appsettingsPath) {
    Write-Host "🔐 Renaming appsettings.json → appsettings.publish.json"
    Move-Item $appsettingsPath $appsettingsRenamed -Force
} else {
    Write-Warning "⚠️ appsettings.json not found"
}

$configurationPath = Join-Path $publishDir "configuration.json"
$configurationRenamed = Join-Path $publishDir "configuration.publish.json"
if (Test-Path $configurationPath) {
    Write-Host "🔐 Renaming configuration.json → configuration.publish.json"
    Move-Item $configurationPath $configurationRenamed -Force
} else {
    Write-Warning "⚠️ configuration.json not found"
}


# ZIP using 7-Zip
Write-Host "`n📦 Creating ZIP file: $zipPath"

if (-not (Get-Command $sevenZip -ErrorAction SilentlyContinue)) {
    Write-Warning "⚠️  7-Zip not found: $sevenZip. Skipping zip step."
} else {
    $args = @(
        'a',
        '-tzip',
        $zipPath,
        "$publishDir/*",
        '-xr!*.zip',
        '-xr!*.rar',
        "-xr!$zipFileName"
    )

    Write-Host "▶️ Running: $sevenZip $($args -join ' ')"
    Start-Process -FilePath $sevenZip -ArgumentList $args -Wait -NoNewWindow

    if (Test-Path $zipPath) {
        Write-Host "✅ ZIP created: $zipPath"
    } else {
        Write-Warning "❌ ZIP was not created."
    }
}

# Optionally copy ZIP to local target folder (only on Windows)
if ($IsWinPlatform) {
    
    if (-not (Test-Path $targetFolder)) {
        Write-Host "📂 Creating target folder: $targetFolder"
        New-Item -Path $targetFolder -ItemType Directory | Out-Null
    }

    if (Test-Path $zipPath) {
        $targetZipPath = Join-Path $targetFolder (Split-Path $zipPath -Leaf)
        Copy-Item $zipPath -Destination $targetZipPath -Force
        Write-Host "📥 Copied ZIP to: $targetZipPath"
    } else {
        Write-Warning "⚠️ ZIP file not found, cannot copy to local target folder."
    }
}
