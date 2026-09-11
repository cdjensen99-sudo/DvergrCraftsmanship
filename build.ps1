param(
    [string]$ValheimPath = "D:\SteamLibrary\steamapps\common\Valheim",
    [string]$DeployProfile = "C:\Users\cdjen\AppData\Roaming\com.kesomannen.gale\valheim\profiles\New Release",
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
    $pluginDir = Join-Path $DeployProfile "BepInEx\plugins\Hardwire99-DvergrCraftsmanship"
    $legacyDir = Join-Path $DeployProfile "BepInEx\plugins\DvergrCraftsmanship"
    $cacheDir = Join-Path $env:USERPROFILE "AppData\Roaming\com.kesomannen.gale\cache\Hardwire99-DvergrCraftsmanship\$($manifest.version_number)\BepInEx\plugins\Hardwire99-DvergrCraftsmanship"
    New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
    $dest = Join-Path $pluginDir "DvergrCraftsmanship.dll"

    $filesToCopy = @(
        @{ Src = $dll; Name = "DvergrCraftsmanship.dll" },
        @{ Src = (Join-Path $thunderstore "manifest.json"); Name = "manifest.json" },
        @{ Src = (Join-Path $thunderstore "CHANGELOG.md"); Name = "CHANGELOG.md" },
        @{ Src = (Join-Path $thunderstore "README.md"); Name = "README.md" }
    )
    $icon = Join-Path $thunderstore "icon.png"
    if (Test-Path $icon) {
        $filesToCopy += @{ Src = $icon; Name = "icon.png" }
    }

    try {
        foreach ($f in $filesToCopy) {
            if (Test-Path $f.Src) {
                Copy-Item $f.Src (Join-Path $pluginDir $f.Name) -Force
            }
        }
        Write-Host "Deployed to $dest"

        # Gale re-copies from cache on launch; keep cache in sync or the July package comes back.
        if (Test-Path (Split-Path $cacheDir)) {
            New-Item -ItemType Directory -Force -Path $cacheDir | Out-Null
            foreach ($f in $filesToCopy) {
                if (Test-Path $f.Src) {
                    Copy-Item $f.Src (Join-Path $cacheDir $f.Name) -Force
                }
            }
            Write-Host "Synced Gale cache: $cacheDir"
        }
    }
    catch {
        $pending = Join-Path $pluginDir "DvergrCraftsmanship.dll.pending"
        Copy-Item $dll $pending -Force
        Write-Warning "Valheim has the plugin locked. Close the game, then replace DvergrCraftsmanship.dll with DvergrCraftsmanship.dll.pending"
        Write-Host "Built update saved to $pending"
    }

    if (Test-Path $legacyDir) {
        Remove-Item $legacyDir -Recurse -Force
        Write-Host "Removed legacy plugin folder: $legacyDir"
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
