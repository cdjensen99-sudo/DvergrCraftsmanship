param(
    [string]$ValheimPath = "D:\SteamLibrary\steamapps\common\Valheim",
    [string]$DeployProfile = "C:\Users\cdjen\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Default",
    [switch]$Deploy,
    [switch]$Package
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\DvergrCraftsmanship\DvergrCraftsmanship.csproj"
$dll = Join-Path $root "artifacts\DvergrCraftsmanship.dll"
$thunderstore = Join-Path $root "thunderstore"
$manifest = Get-Content (Join-Path $thunderstore "manifest.json") | ConvertFrom-Json

dotnet build $project -p:ValheimPath=$ValheimPath -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Built: $dll"

if ($Deploy) {
    $pluginDir = Join-Path $DeployProfile "BepInEx\plugins\DvergrCraftsmanship"
    $duplicateDir = Join-Path $DeployProfile "BepInEx\plugins\Unknown-DvergrCraftsmanship.dll"
    New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
    $dest = Join-Path $pluginDir "DvergrCraftsmanship.dll"

    try {
        Copy-Item $dll $dest -Force
        Copy-Item (Join-Path $thunderstore "manifest.json") (Join-Path $pluginDir "manifest.json") -Force
        Copy-Item (Join-Path $thunderstore "CHANGELOG.md") (Join-Path $pluginDir "CHANGELOG.md") -Force
        Write-Host "Deployed to $dest"
    }
    catch {
        $pending = Join-Path $pluginDir "DvergrCraftsmanship.dll.pending"
        Copy-Item $dll $pending -Force
        Write-Warning "Valheim has the plugin locked. Close the game, then replace DvergrCraftsmanship.dll with DvergrCraftsmanship.dll.pending"
        Write-Host "Built update saved to $pending"
    }

    if (Test-Path $duplicateDir) {
        Remove-Item $duplicateDir -Recurse -Force
        Write-Host "Removed duplicate plugin folder: $duplicateDir"
    }
}

if ($Package) {
    $staging = Join-Path $root "artifacts\thunderstore-staging"
    $packageName = "{0}-{1}.zip" -f $manifest.name, $manifest.version_number
    $packagePath = Join-Path (Join-Path $root "artifacts") $packageName

    if (Test-Path $staging) {
        Remove-Item $staging -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $staging | Out-Null

    Copy-Item $dll (Join-Path $staging "DvergrCraftsmanship.dll") -Force
    Copy-Item (Join-Path $thunderstore "manifest.json") (Join-Path $staging "manifest.json") -Force
    Copy-Item (Join-Path $thunderstore "README.md") (Join-Path $staging "README.md") -Force
    Copy-Item (Join-Path $thunderstore "CHANGELOG.md") (Join-Path $staging "CHANGELOG.md") -Force

    $icon = Join-Path $thunderstore "icon.png"
    if (Test-Path $icon) {
        Copy-Item $icon (Join-Path $staging "icon.png") -Force
    }

    if (Test-Path $packagePath) {
        Remove-Item $packagePath -Force
    }
    Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $packagePath -Force
    Write-Host "Packaged: $packagePath"
}

if (-not $Deploy -and -not $Package) {
    Write-Host "Skipped deploy/package. Pass -Deploy and/or -Package as needed."
}
