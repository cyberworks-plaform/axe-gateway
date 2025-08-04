$source = "D:\Project\cyberworks-github\axe-gateway"
$destination = "D:\Project\ce-gitlab\ax-api-getway"

# Copy toàn bộ file/folder, trừ .git và .vs
Get-ChildItem -Path $source -Recurse -Force | Where-Object {
    $_.FullName -notmatch '\\\.git($|\\)' -and $_.FullName -notmatch '\\\.vs($|\\)'
} | ForEach-Object {
    $targetPath = $_.FullName.Replace($source, $destination)

    if ($_.PSIsContainer) {
        if (-not (Test-Path -Path $targetPath)) {
            New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
        }
    } else {
        $targetDir = Split-Path $targetPath -Parent
        if (-not (Test-Path -Path $targetDir)) {
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        }
        Copy-Item -Path $_.FullName -Destination $targetPath -Force
    }
}

Write-Host "✅ Đã copy source code từ GitHub local sang GitLab local (không kèm .git và .vs)"
