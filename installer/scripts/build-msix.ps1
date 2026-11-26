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
$iconPath = Join-Path $repositoryRoot 'src\Aprint\Resources\AppIcon.ico'
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

    Add-Type -AssemblyName System.Drawing
    $assets = Join-Path $stage 'Assets'
    New-Item -ItemType Directory -Path $assets | Out-Null
    
    # Map desired PNG sizes to nearest .ico sizes (preserve transparency)
    $sizes = @{
        50  = 48      # StoreLogo.png
        150 = 128     # Square150x150Logo.png
        44  = 48      # Square44x44Logo.png
    }
    
    # Create PNG files from native .ico images, then resample to target size
    foreach ($pngSize in @(50, 150, 44)) {
        $pngName = switch ($pngSize) {
            50  { 'StoreLogo.png' }
            150 { 'Square150x150Logo.png' }
            44  { 'Square44x44Logo.png' }
        }
        $icoSize = $sizes[$pngSize]
        
        # Extract and decode the native .ico image
        $icon = New-Object System.Drawing.Icon($iconPath, $icoSize, $icoSize)
        try {
            # ToBitmap() preserves transparency for 32-bit icons
            $source = $icon.ToBitmap()
            try {
                if ($source.Width -eq $pngSize) {
                    # Already the right size; save directly
                    $source.Save((Join-Path $assets $pngName), [System.Drawing.Imaging.ImageFormat]::Png)
                } else {
                    # Resample to PNG size with explicit ARGB format for transparency
                    $bitmap = New-Object System.Drawing.Bitmap($pngSize, $pngSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
                    try {
                        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
                        try {
                            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                            $graphics.DrawImage($source, 0, 0, $pngSize, $pngSize)
                        } finally {
                            $graphics.Dispose()
                        }
                        $bitmap.Save((Join-Path $assets $pngName), [System.Drawing.Imaging.ImageFormat]::Png)
                    } finally {
                        $bitmap.Dispose()
                    }
                }
            } finally {
                $source.Dispose()
            }
        } finally {
            $icon.Dispose()
        }
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
