$ErrorActionPreference = 'Stop'

$generator = Join-Path $PSScriptRoot '..\installer\scripts\New-WingetManifest.ps1'
$testDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "aprint-winget-$([guid]::NewGuid())"
New-Item -ItemType Directory -Path $testDirectory | Out-Null

try {
    $installer = Join-Path $testDirectory 'Aprint-1.2.3-setup.exe'
    $output = Join-Path $testDirectory 'manifests'
    [System.IO.File]::WriteAllBytes($installer, [byte[]](1, 2, 3))

    & $generator -Version '1.2.3' -InstallerPath $installer -OutputDirectory $output
    $files = @(Get-ChildItem -LiteralPath $output -Filter '*.yaml')
    if ($files.Count -ne 3) {
        throw "Expected three manifests, got $($files.Count)."
    }
    $manifestPath = Join-Path $output 'adrianoramos.Aprint.installer.yaml'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw
    $expectedHash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash
    if (-not $manifest.Contains("InstallerSha256: $expectedHash") -or
        -not $manifest.Contains('InstallerUrl: https://github.com/adrianoramos/aprint/releases/download/v1.2.3/Aprint-1.2.3-setup.exe')) {
        throw 'Installer URL or SHA256 does not match the release artifact.'
    }

    [System.IO.File]::WriteAllBytes($installer, [byte[]](4, 5, 6))
    & $generator -Version '1.2.3' -InstallerPath $installer -OutputDirectory $output
    $updatedManifest = Get-Content -LiteralPath $manifestPath -Raw
    $updatedHash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash
    if ($updatedHash -eq $expectedHash -or -not $updatedManifest.Contains("InstallerSha256: $updatedHash")) {
        throw 'Regeneration did not update the installer checksum.'
    }

    $invalidCases = @(
        @{ Version = '1.2.3-beta.1'; InstallerPath = $installer },
        @{ Version = '01.2.3'; InstallerPath = $installer },
        @{ Version = '1.2.4'; InstallerPath = $installer },
        @{ Version = '1.2.3'; InstallerPath = (Join-Path $testDirectory 'missing.exe') }
    )
    $emptyInstaller = Join-Path $testDirectory 'Aprint-2.0.0-setup.exe'
    New-Item -ItemType File -Path $emptyInstaller | Out-Null
    $invalidCases += @{ Version = '2.0.0'; InstallerPath = $emptyInstaller }

    foreach ($case in $invalidCases) {
        $rejectedOutput = Join-Path $testDirectory "rejected-$([guid]::NewGuid())"
        $rejected = $false
        try {
            & $generator @case -OutputDirectory $rejectedOutput
        } catch {
            $rejected = $true
        }
        if (-not $rejected -or (Test-Path -LiteralPath $rejectedOutput)) {
            throw "Invalid input was not rejected before writing manifests: $($case.Version)."
        }
    }

    Write-Host 'WinGet manifest generation tests passed.'
} finally {
    Remove-Item -LiteralPath $testDirectory -Recurse -Force
}
