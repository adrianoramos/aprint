[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,
    [Parameter(Mandatory)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing

$archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)
try {
    foreach ($name in @('Aprint.exe', 'hostfxr.dll', 'coreclr.dll', 'System.Windows.Forms.dll', 'Aprint.runtimeconfig.json',
                         'LICENSE', 'AppxManifest.xml', 'AppxBlockMap.xml')) {
        if ($null -eq $archive.GetEntry($name)) {
            throw "MSIX is missing $name."
        }
    }

    $stream = $archive.GetEntry('Aprint.runtimeconfig.json').Open()
    try {
        $reader = New-Object System.IO.StreamReader($stream)
        try {
            $runtime = $reader.ReadToEnd() | ConvertFrom-Json
        } finally {
            $reader.Dispose()
        }
    } finally {
        $stream.Dispose()
    }
    if (@($runtime.runtimeOptions.includedFrameworks | Where-Object { $_.name -eq 'Microsoft.WindowsDesktop.App' }).Count -ne 1) {
        throw 'MSIX does not contain a self-contained Windows Desktop runtime.'
    }

    $stream = $archive.GetEntry('AppxManifest.xml').Open()
    try {
        $reader = New-Object System.IO.StreamReader($stream)
        try {
            [xml]$manifest = $reader.ReadToEnd()
        } finally {
            $reader.Dispose()
        }
    } finally {
        $stream.Dispose()
    }

    $identity = $manifest.Package.Identity
    if ($identity.Name -ne 'AdrianoSilva.Aprint' -or
        $identity.Publisher -ne 'CN=D6D0E027-49F9-4265-8244-75AFE2DAE05B' -or
        $identity.ProcessorArchitecture -ne 'x64' -or $identity.Version -ne "$Version.0" -or
        $manifest.Package.Properties.PublisherDisplayName -ne 'Adriano Silva') {
        throw 'MSIX identity or version does not match Partner Center.'
    }
    $ns = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
    $ns.AddNamespace('f', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')
    $ns.AddNamespace('uap5', 'http://schemas.microsoft.com/appx/manifest/uap/windows10/5')
    $startup = $manifest.SelectSingleNode('/f:Package/f:Applications/f:Application/f:Extensions/uap5:Extension/uap5:StartupTask', $ns)
    if ($null -eq $startup -or $startup.TaskId -ne 'AprintStartup' -or $startup.Enabled -ne 'false') {
        throw 'MSIX startup task is missing or incorrectly enabled by default.'
    }

    foreach ($asset in @(
        @{ Name = 'Assets/StoreLogo.png'; Size = 50 },
        @{ Name = 'Assets/Square150x150Logo.png'; Size = 150 },
        @{ Name = 'Assets/Square44x44Logo.png'; Size = 44 }
    )) {
        $entry = $archive.GetEntry($asset.Name)
        if ($null -eq $entry) { throw "MSIX is missing $($asset.Name)." }
        $stream = $entry.Open()
        try {
            $image = [System.Drawing.Image]::FromStream($stream)
            try {
                if ($image.Width -ne $asset.Size -or $image.Height -ne $asset.Size) {
                    throw "MSIX asset $($asset.Name) has incorrect dimensions."
                }
            } finally {
                $image.Dispose()
            }
        } finally {
            $stream.Dispose()
        }
    }
} finally {
    $archive.Dispose()
}

$builder = Join-Path $PSScriptRoot '..\installer\scripts\build-msix.ps1'
foreach ($invalid in @('0.1.0', '1.2.65536', '1.2.3-beta', '01.2.3')) {
    $rejected = $false
    try {
        & $builder -Version $invalid
    } catch {
        $rejected = $true
    }
    if (-not $rejected) { throw "Invalid MSIX version $invalid was accepted." }
}
Write-Host 'MSIX package tests passed.'
