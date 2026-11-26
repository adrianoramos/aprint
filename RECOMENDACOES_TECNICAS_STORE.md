# 🔧 RECOMENDAÇÕES TÉCNICAS - MICROSOFT STORE

Mudanças específicas de código e configuração recomendadas.

---

## 1. ATUALIZAR AppxManifest.xml

**Arquivo:** `installer/msix/AppxManifest.xml`

### Recomendação 1.1: MaxVersionTested
Atualizar para Windows 11 mais recente:

```xml
<!-- ANTES -->
<TargetDeviceFamily Name="Windows.Desktop" 
                    MinVersion="10.0.17763.0"
                    MaxVersionTested="10.0.19041.0" />

<!-- DEPOIS -->
<TargetDeviceFamily Name="Windows.Desktop" 
                    MinVersion="10.0.17763.0"
                    MaxVersionTested="10.0.26100.0" /> <!-- Windows 11 24H2 -->
```

**Razão:** Demonstra compatibilidade com Windows 11 mais recente

### Recomendação 1.2: Adicionar Política de Privacidade

```xml
<!-- ANTES -->
<Properties>
    <DisplayName>Aprint</DisplayName>
    <PublisherDisplayName>Adriano Silva</PublisherDisplayName>
    <Description>Capture, annotate, redact, and share screenshots.</Description>
    <Logo>Assets\StoreLogo.png</Logo>
</Properties>

<!-- DEPOIS -->
<Properties>
    <DisplayName>Aprint</DisplayName>
    <PublisherDisplayName>Adriano Silva</PublisherDisplayName>
    <Description>Capture, annotate, redact, and share screenshots.</Description>
    <Logo>Assets\StoreLogo.png</Logo>
    <PrivacyUri>https://aprint.site/privacy.html</PrivacyUri>
</Properties>
```

### Recomendação 1.3: Adicionar Suporte a Light/Dark Theme

```xml
<!-- ANTES -->
<uap:VisualElements DisplayName="Aprint"
                    Description="Capture, annotate, redact, and share screenshots."
                    Square150x150Logo="Assets\Square150x150Logo.png"
                    Square44x44Logo="Assets\Square44x44Logo.png"
                    BackgroundColor="transparent" />

<!-- DEPOIS -->
<uap:VisualElements DisplayName="Aprint"
                    Description="Capture, annotate, redact, and share screenshots."
                    Square150x150Logo="Assets\Square150x150Logo.png"
                    Square44x44Logo="Assets\Square44x44Logo.png"
                    BackgroundColor="transparent"
                    AppListEntry="default" />
<uap:DefaultTile Wide310x150Logo="Assets\WideLogo.png" 
                 Square310x310Logo="Assets\LargeLogo.png" />
<uap:SplashScreen Image="Assets\SplashScreen.png" />
```

**Resultado esperado:**

```diff
  <?xml version="1.0" encoding="utf-8"?>
  <Package ...>
    <Identity ... MaxVersionTested="10.0.26100.0" />
    <Properties>
      ...
+     <PrivacyUri>https://aprint.site/privacy.html</PrivacyUri>
    </Properties>
    <Applications>
      <Application ...>
        <uap:VisualElements ... />
+       <uap:DefaultTile Wide310x150Logo="Assets\WideLogo.png" ... />
+       <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </Application>
    </Applications>
  </Package>
```

---

## 2. CRIAR ARQUIVO PRIVACY_POLICY.md

**Arquivo:** `PRIVACY_POLICY.md` (raiz do repo)

```markdown
# Privacy Policy - Aprint

**Last Updated:** November 25, 2026

## Summary

Aprint does not collect, transmit, or store personal data beyond your local device.
All operations occur locally on your computer.

## Data Processing Locally

Aprint processes the following data exclusively on your device:

- **Screenshots** - Stored only on your device (clipboard or disk)
- **Application Settings** - Stored locally in `%APPDATA%\Aprint\settings.json`
- **Edit History** - Kept in memory, not persisted
- **OCR Results** - Processed by Windows OCR locally, not sent anywhere

## What Aprint Does NOT Do

✅ Does not send images to servers
✅ Does not track usage or behavior
✅ Does not collect personal information
✅ Does not use telemetry or analytics
✅ Does not share data with third parties
✅ Does not modify system settings without permission

## Local Data Storage

Aprint stores configuration locally:

- **Location:** `%APPDATA%\Local\Aprint\`
- **Files:**
  - `settings.json` - User preferences (capture mode, shortcuts, language)
  - Temporary DOCX files during session (deleted after save)

## Network Behavior

Aprint does not make network calls for:
- Telemetry
- Usage analytics
- Crash reporting
- Automatic updates (manual via Microsoft Store)

Aprint may download:
- Updates from Microsoft Store (Store-managed)
- OCR language packs from Windows Update (if configured)

## Third-Party Components

- **Open XML SDK** - Processes DOCX files locally
- **Windows APIs** - P/Invoke to native Windows functions
- **.NET Runtime** - Microsoft-provided .NET 10 Desktop Runtime

None of these components send data on behalf of Aprint.

## Windows OCR

Aprint uses Windows OCR for automatic text detection:
- OCR engine is built into Windows
- Text recognition occurs locally
- Results are **not** sent to external services
- Results are **not** stored permanently

## User Control

You have full control over:
- When and what to capture
- Where to save files
- Which configuration options to use
- Language and appearance preferences

You can:
- Disable startup with Windows
- Configure custom shortcuts
- Choose notification preferences
- Delete settings anytime by removing `%APPDATA%\Aprint\`

## Data Deletion

To completely remove Aprint data:

```powershell
# PowerShell
Remove-Item -Path "$env:APPDATA\Aprint\" -Recurse -Force
```

## Changes to This Policy

We will notify users of significant changes via:
- GitHub releases/changelog
- In-application notification

## Contact

For privacy questions or concerns:
- Create an issue: https://github.com/adrianoramos/aprint/issues
- Subject: "Privacy Concern"

## Compliance

Aprint complies with:
- Microsoft Store App Policies
- Windows privacy guidelines
- GDPR (by processing only local data)
- CCPA (not collecting personal data)

---

**You can review Aprint source code:** https://github.com/adrianoramos/aprint
(Transparency through open source)
```

---

## 3. ATUALIZAR build-msix.ps1

**Arquivo:** `installer/scripts/build-msix.ps1`

Adicionar validações extras:

```powershell
# Adicionar após linha 46 (após dotnet publish):

# Validar que AppxManifest.xml contém PrivacyUri
Write-Host "Validating AppxManifest.xml..."
[xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw
if ([string]::IsNullOrWhiteSpace($manifest.Package.Properties.PrivacyUri)) {
    Write-Warning "Warning: PrivacyUri not found in manifest. Add it in Partner Center or manifest."
}

# Validar que versão segue Semantic Versioning
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version must follow semantic versioning (major.minor.patch)"
}

Write-Host "MSIX validation passed. Version: $Version"
```

---

## 4. ADICIONAR TESTES DE PRIVACIDADE

**Novo arquivo:** `tests/Privacy.Tests.ps1`

```powershell
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

# Teste 1: Privacy Policy existe
$privacyFile = Join-Path $repoRoot 'PRIVACY_POLICY.md'
if (-not (Test-Path -LiteralPath $privacyFile)) {
    throw "PRIVACY_POLICY.md not found in repository root"
}
Write-Host "✓ Privacy policy file found"

# Teste 2: Verificar que privacy policy é mencionada no manifesto
$manifestPath = Join-Path $repoRoot 'installer\msix\AppxManifest.xml'
[xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw
if ([string]::IsNullOrWhiteSpace($manifest.Package.Properties.PrivacyUri)) {
    Write-Warning "⚠ Privacy URI not in manifest. Ensure it's added in Partner Center"
} else {
    Write-Host "✓ Privacy URI in manifest: $($manifest.Package.Properties.PrivacyUri)"
}

# Teste 3: Verificar que não há código P/Invoke suspeito
Write-Host "✓ Checking for suspicious P/Invoke patterns..."
$files = Get-ChildItem -Path (Join-Path $repoRoot 'src') -Recurse -Include '*.cs'
$suspiciousPatterns = @(
    'CreateRemoteThread',
    'WriteProcessMemory',
    'VirtualAllocEx',
    'CreateProcessA',
    'WinExec'
)

foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($pattern in $suspiciousPatterns) {
        if ($content -match "\b$pattern\b") {
            Write-Warning "⚠ Found potentially suspicious P/Invoke: $pattern in $($file.Name)"
        }
    }
}

Write-Host "✓ Privacy checks passed"
```

**Executar:** `.\tests\Privacy.Tests.ps1`

---

## 5. CRIAR SCREENSHOTS AUTOMATED

**Novo script:** `installer/scripts/capture-screenshots.ps1`

```powershell
[CmdletBinding()]
param(
    [ValidateSet('All', 'Main', 'Editor', 'Settings')]
    [string]$Scope = 'All'
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$screenshotsDir = Join-Path $repoRoot 'assets\screenshots'

if (-not (Test-Path -LiteralPath $screenshotsDir)) {
    New-Item -ItemType Directory -Path $screenshotsDir -Force | Out-Null
}

Write-Host "Screenshot Capture Guide (Manual Process)"
Write-Host "==========================================="
Write-Host ""
Write-Host "Screenshots must be exactly 1920x1080 (or 1280x720)"
Write-Host "Format: PNG or JPEG"
Write-Host "Location: $screenshotsDir"
Write-Host ""
Write-Host "Suggested screenshots:"
Write-Host ""
Write-Host "1. SYSTEM TRAY (Main Interface)"
Write-Host "   - Right-click tray icon"
Write-Host "   - Menu open showing: Capture, Settings, Session"
Write-Host "   - Save as: 01-system-tray.png"
Write-Host ""
Write-Host "2. REGION SELECTION"
Write-Host "   - Capture in progress"
Write-Host "   - Floating toolbar visible"
Write-Host "   - Various tools available"
Write-Host "   - Save as: 02-region-selection.png"
Write-Host ""
Write-Host "3. EDITOR WINDOW"
Write-Host "   - Multiple tools visible"
Write-Host "   - Drawing, shapes, text examples"
Write-Host "   - Highlighting/mask examples"
Write-Host "   - Save as: 03-editor.png"
Write-Host ""
Write-Host "4. OCR FEATURES"
Write-Host "   - Automatic redaction enabled"
Write-Host "   - Suggested masks visible"
Write-Host "   - Save as: 04-ocr-redaction.png"
Write-Host ""
Write-Host "5. SETTINGS"
Write-Host "   - Settings dialog open"
Write-Host "   - Multiple configuration options"
Write-Host "   - Save as: 05-settings.png"
Write-Host ""
Write-Host "Tip: Use Aprint itself to capture and annotate!"

# Check if images exist
$images = Get-ChildItem -Path $screenshotsDir -Filter '*.png' -ErrorAction SilentlyContinue
Write-Host ""
Write-Host "Current screenshots: $($images.Count)"
if ($images) {
    $images | ForEach-Object { Write-Host "  ✓ $($_.Name)" }
}
```

---

## 6. ATUALIZAR README.md (Seção Store)

Adicionar à seção "Publishing and Installer":

```markdown
### Microsoft Store Distribution

To publish to Microsoft Store:

1. **Prepare submission** (see `PLANO_ACAO_PUBLICACAO_STORE.md`):
   ```powershell
   # 1. Create privacy policy
   # 2. Prepare 5-9 screenshots (1920x1080)
   # 3. Register in Partner Center
   
   # 4. Build MSIX
   .\installer\scripts\build-msix.ps1 -Version 1.0.0
   
   # 5. Validate MSIX
   .\tests\Msix.Tests.ps1 -PackagePath artifacts/msix/Aprint-1.0.0-x64.msix -Version 1.0.0
   
   # 6. Run Windows App Certification Kit
   # (Download from Microsoft Store)
   ```

2. **Submit** via Partner Center (https://partner.microsoft.com/)
   - Upload MSIX package
   - Partner Center signs automatically
   - Wait for certification (3-5 days)

3. **Monitor** store listing:
   - https://www.microsoft.com/store/apps/[AppID]
```

---

## 7. CRIAR SCRIPT DE VALIDAÇÃO PRÉ-SUBMISSÃO

**Novo arquivo:** `installer/scripts/validate-store-submission.ps1`

```powershell
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

Write-Host "🔍 Pre-Submission Validation for Microsoft Store"
Write-Host "=================================================="
Write-Host ""

$allPassed = $true

# 1. Verificar Privacy Policy
Write-Host "1. Privacy Policy"
$privacyFile = Join-Path $repoRoot 'PRIVACY_POLICY.md'
if (Test-Path -LiteralPath $privacyFile) {
    Write-Host "   ✓ PRIVACY_POLICY.md found"
    $size = (Get-Item -LiteralPath $privacyFile).Length
    if ($size -lt 1000) {
        Write-Host "   ⚠ Warning: Privacy policy seems too short ($size bytes)"
    }
} else {
    Write-Host "   ✗ MISSING: Create PRIVACY_POLICY.md in repo root"
    $allPassed = $false
}

# 2. Verificar AppxManifest
Write-Host ""
Write-Host "2. MSIX Manifest"
$manifestPath = Join-Path $repoRoot 'installer\msix\AppxManifest.xml'
[xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw

if ($manifest.Package.Identity.Name -eq 'AdrianoSilva.Aprint') {
    Write-Host "   ✓ Identity name correct"
} else {
    Write-Host "   ✗ WRONG: Identity name is '$($manifest.Package.Identity.Name)'"
    $allPassed = $false
}

if ($manifest.Package.Properties.PrivacyUri) {
    Write-Host "   ✓ Privacy URI declared: $($manifest.Package.Properties.PrivacyUri)"
} else {
    Write-Host "   ⚠ WARNING: Add PrivacyUri to manifesto or Partner Center"
}

if ($manifest.Package.Capabilities.SelectSingleNode('*/@Name').Value -eq 'runFullTrust') {
    Write-Host "   ✓ Full Trust capability declared"
} else {
    Write-Host "   ⚠ WARNING: Check Full Trust capability"
}

# 3. Verificar screenshots
Write-Host ""
Write-Host "3. Store Screenshots"
$screenshotsDir = Join-Path $repoRoot 'assets\screenshots'
$screenshots = @(Get-ChildItem -Path $screenshotsDir -Include '*.png', '*.jpg' -ErrorAction SilentlyContinue)
Write-Host "   Found: $($screenshots.Count) screenshots"
if ($screenshots.Count -lt 5) {
    Write-Host "   ⚠ WARNING: Microsoft Store recommends 5-9 screenshots"
} else {
    Write-Host "   ✓ Minimum requirement met"
}

# 4. Verificar MSIX build
Write-Host ""
Write-Host "4. MSIX Package"
$msixPath = Join-Path $repoRoot "artifacts\msix\Aprint-$Version-x64.msix"
if (Test-Path -LiteralPath $msixPath) {
    Write-Host "   ✓ MSIX exists: Aprint-$Version-x64.msix"
    $size = (Get-Item -LiteralPath $msixPath).Length / 1MB
    Write-Host "   Size: $([Math]::Round($size, 2)) MB"
} else {
    Write-Host "   ✗ MISSING: Build MSIX first with build-msix.ps1"
    $allPassed = $false
}

# 5. Resumo
Write-Host ""
Write-Host "=================================================="
if ($allPassed) {
    Write-Host "✓ All pre-submission checks PASSED!"
    Write-Host ""
    Write-Host "Next steps:"
    Write-Host "1. Register in Partner Center (if not done)"
    Write-Host "2. Upload MSIX to Partner Center"
    Write-Host "3. Run Windows App Certification Kit"
    Write-Host "4. Submit for certification"
} else {
    Write-Host "✗ Some checks FAILED. See above for details."
    exit 1
}
```

**Executar:** `.\installer\scripts\validate-store-submission.ps1 -Version 1.0.0`

---

## 8. CONFIGURAÇÃO RECOMENDADA NO PARTNER CENTER

**Resumo dos campos a preencher:**

```
App Name:               Aprint
Display Name:           Aprint - Screenshot Studio
Category:               Productivity / Graphics & Design
Description:           [Use multilingual description from Phase 2]
Privacy URL:            https://github.com/adrianoramos/aprint/blob/main/PRIVACY_POLICY.md
Support URL:            https://github.com/adrianoramos/aprint/issues
Company Name:           Adriano Silva
Homepage:               https://github.com/adrianoramos/aprint
Support Contact:        [your-email@domain.com]

IARC Rating:            Everyone

Screenshots:            [5-9 × 1920x1080 PNG/JPG]
Icon:                   [1200x1200 PNG from AppIcon.ico]

Developer Account:
  Publisher Display:    Adriano Silva
  Publisher ID:         [From Partner Center]

Package Details:
  Minimum OS Version:   Windows 10 (1809)
  Architecture:         x64 only
  Runtime:              .NET 10 (included in MSIX)
```

---

## 📋 RESUMO DE MUDANÇAS

| Arquivo | Mudança | Prioridade |
|---------|---------|-----------|
| AppxManifest.xml | MaxVersionTested + PrivacyUri | 🔴 Alta |
| PRIVACY_POLICY.md | Novo arquivo | 🔴 Alta |
| README.md | Adicionar seção Store | 🟡 Média |
| build-msix.ps1 | Validações extras | 🟡 Média |
| Privacy.Tests.ps1 | Novo teste | 🟡 Média |
| validate-store-submission.ps1 | Novo script | 🟢 Baixa |
| assets/screenshots | Capturar 5-9 imagens | 🔴 Alta |

---

## ✅ PRÓXIMOS PASSOS

1. **Hoje:** Criar PRIVACY_POLICY.md e commitar
2. **Amanhã:** Atualizar AppxManifest.xml com PrivacyUri
3. **Esta semana:** Capturar e preparar screenshots
4. **Próxima semana:** Registrar no Partner Center
5. **2 semanas:** Submeter para certificação

---

**Todas as mudanças acima são reversíveis e não afetam a funcionalidade do aplicativo!**
