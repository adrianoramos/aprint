[CmdletBinding()]
param(
    [Parameter()]
    [ValidatePattern('^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$')]
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$projectPath = Join-Path $repositoryRoot 'src\Aprint\Aprint.csproj'
$publishDirectory = Join-Path $repositoryRoot 'artifacts\publish'
$installerScript = Join-Path $PSScriptRoot 'Aprint.iss'
$versionMatch = [regex]::Match($Version, '^(\d+)\.(\d+)\.(\d+)')
$fileVersion = '{0}.{1}.{2}.0' -f $versionMatch.Groups[1].Value, $versionMatch.Groups[2].Value, $versionMatch.Groups[3].Value

if (Test-Path $publishDirectory) {
    Remove-Item $publishDirectory -Recurse -Force
}

New-Item $publishDirectory -ItemType Directory -Force | Out-Null

dotnet publish $projectPath --configuration Release --output $publishDirectory "-p:Version=$Version"
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$isccPath = (Get-Command iscc.exe -ErrorAction SilentlyContinue).Source
if ([string]::IsNullOrWhiteSpace($isccPath)) {
    $candidatePaths = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
    )

    $isccPath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($null -eq $isccPath) {
        throw 'Inno Setup 6 (ISCC.exe) was not found. Install it or add it to PATH.'
    }

}

& $isccPath "/DMyAppVersion=$Version" "/DMyFileVersion=$fileVersion" "/DMyPublishDir=$publishDirectory" $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed with exit code $LASTEXITCODE."
}

Write-Host "Installer created in $(Join-Path $repositoryRoot 'installer')."
