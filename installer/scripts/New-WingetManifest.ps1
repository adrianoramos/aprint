[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$')]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$InstallerPath,

    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\..\artifacts\winget')
)

$ErrorActionPreference = 'Stop'

$installer = Get-Item -LiteralPath $InstallerPath
if ($installer.PSIsContainer -or $installer.Name -cne "Aprint-$Version-setup.exe") {
    throw "Expected an installer named Aprint-$Version-setup.exe."
}
if ($installer.Length -eq 0) {
    throw 'The installer is empty.'
}

$sha256 = (Get-FileHash -LiteralPath $installer.FullName -Algorithm SHA256).Hash
$packageIdentifier = 'adrianoramos.Aprint'
$releaseUrl = "https://github.com/adrianoramos/aprint/releases/tag/v$Version"
$installerUrl = "https://github.com/adrianoramos/aprint/releases/download/v$Version/$($installer.Name)"

$manifests = @{
    "$packageIdentifier.yaml" = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.1.9.0.schema.json
PackageIdentifier: $packageIdentifier
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.9.0
"@
    "$packageIdentifier.installer.yaml" = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.9.0.schema.json
PackageIdentifier: $packageIdentifier
PackageVersion: $Version
InstallerType: inno
Scope: machine
MinimumOSVersion: 10.0.17763.0
InstallModes:
- interactive
- silent
- silentWithProgress
UpgradeBehavior: install
ProductCode: '{0EB8385F-7616-43AC-A7E8-7CA5B2F0436A}_is1'
Dependencies:
  PackageDependencies:
  - PackageIdentifier: Microsoft.DotNet.DesktopRuntime.10
    MinimumVersion: 10.0.0
AppsAndFeaturesEntries:
- DisplayName: Aprint $Version
  Publisher: Aprint
  DisplayVersion: $Version
Installers:
- Architecture: x64
  InstallerUrl: $installerUrl
  InstallerSha256: $sha256
ManifestType: installer
ManifestVersion: 1.9.0
"@
    "$packageIdentifier.locale.en-US.yaml" = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.1.9.0.schema.json
PackageIdentifier: $packageIdentifier
PackageVersion: $Version
PackageLocale: en-US
Publisher: Aprint
PublisherUrl: https://github.com/adrianoramos
PublisherSupportUrl: https://github.com/adrianoramos/aprint/issues
PackageName: Aprint
PackageUrl: https://github.com/adrianoramos/aprint
License: GPL-3.0
LicenseUrl: https://github.com/adrianoramos/aprint/blob/v$Version/LICENSE
ShortDescription: Capture, annotate, redact, and share screenshots on Windows.
Tags:
- screenshot
- screen-capture
- annotation
- ocr
ReleaseNotesUrl: $releaseUrl
ManifestType: defaultLocale
ManifestVersion: 1.9.0
"@
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
foreach ($manifest in $manifests.GetEnumerator()) {
    Set-Content -LiteralPath (Join-Path $OutputDirectory $manifest.Key) -Value $manifest.Value -Encoding utf8NoBOM
}

Write-Host "WinGet manifests created in $OutputDirectory."
