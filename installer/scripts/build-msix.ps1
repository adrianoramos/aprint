[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$parts = $Version.Split('.')
if ($parts[0] -eq '0' -or @($parts | Where-Object { [decimal]$_ -gt 65535 }).Count -gt 0) {
    throw 'MSIX versions require a nonzero major version and components no greater than 65535.'
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repositoryRoot 'src\Aprint\Aprint.csproj'
$manifestPath = Join-Path $repositoryRoot 'installer\msix\AppxManifest.xml'
$assetsPath = Join-Path $repositoryRoot 'installer\msix\Assets'
$outputDirectory = Join-Path $repositoryRoot 'artifacts\msix'
$stage = Join-Path ([System.IO.Path]::GetTempPath()) "aprint-msix-$([guid]::NewGuid())"
$output = Join-Path $outputDirectory "Aprint-$Version-x64.msix"

$makeAppx = Get-Command makeappx.exe -ErrorAction SilentlyContinue
if ($null -ne $makeAppx) {
    $makeAppxPath = $makeAppx.Source
} else {
    $sdkBin = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    $makeAppxPath = Get-ChildItem -LiteralPath $sdkBin -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
        Sort-Object { [version]$_.Name } -Descending |
        ForEach-Object { Join-Path $_.FullName 'x64\makeappx.exe' } |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Select-Object -First 1
}
if ([string]::IsNullOrWhiteSpace($makeAppxPath)) {
    throw 'MakeAppx.exe was not found. Install the Windows SDK or add MakeAppx.exe to PATH.'
}

New-Item -ItemType Directory -Path $stage | Out-Null
try {
    dotnet publish $project --configuration Release --runtime win-x64 --self-contained true --output $stage "-p:Version=$Version"
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    [xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw
    $manifest.Package.Identity.Version = "$Version.0"
    $manifest.Save((Join-Path $stage 'AppxManifest.xml'))
    Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE') -Destination $stage

    # Copy pre-generated PNG assets
    $stageAssets = Join-Path $stage 'Assets'
    New-Item -ItemType Directory -Path $stageAssets | Out-Null
    foreach ($file in Get-ChildItem -LiteralPath $assetsPath -Filter '*.png') {
        Copy-Item -LiteralPath $file.FullName -Destination $stageAssets
    }

    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    & $makeAppxPath pack /o /h SHA256 /d $stage /p $output
    if ($LASTEXITCODE -ne 0) {
        throw "MakeAppx failed with exit code $LASTEXITCODE."
    }
    Write-Host "Unsigned MSIX created: $output"
} finally {
    Remove-Item -LiteralPath $stage -Recurse -Force
}
