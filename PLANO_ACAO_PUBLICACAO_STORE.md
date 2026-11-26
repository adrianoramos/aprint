# 🚀 PLANO DE AÇÃO: PUBLICAÇÃO DO APRINT NA MICROSOFT STORE

**Objetivo:** Preparar e publicar o Aprint na Microsoft Store em 3-4 semanas.

---

## FASE 1: PREPARAÇÃO (Dias 1-3)

### 1.1 Registrar no Partner Center
**Responsável:** Proprietário do aplicativo  
**Duração:** 2-3 horas

```
[ ] Acesso ao Microsoft Partner Center (https://partner.microsoft.com)
    └─ Email corporativo preferível
    └─ Verificação de identidade (2FA recomendado)
    
[ ] Registrar novo aplicativo
    └─ Nome: "Aprint"
    └─ ID único: será gerado automaticamente
    └─ Tipo: Desktop Application (Windows)
    
[ ] Obter Publisher Identity
    └─ CN será fornecido automaticamente
    └─ Corresponde a: CN=D6D0E027-49F9-4265-8244-75AFE2DAE05B
    └─ **Validar se corresponde ao manifesto existente**
```

### 1.2 Revisar Manifesto MSIX
**Responsável:** Desenvolvedor  
**Duração:** 1-2 horas

Checklist do arquivo [AppxManifest.xml](installer/msix/AppxManifest.xml):

```xml
✅ Identity Name: "AdrianoSilva.Aprint"
✅ Publisher CN: "CN=D6D0E027-49F9-4265-8244-75AFE2DAE05B"
✅ Capability: runFullTrust
✅ TargetDeviceFamily: Windows.Desktop
✅ MinVersion: 10.0.17763.0 (Windows 10 1809) ✅
✅ MaxVersionTested: 10.0.19041.0 (Windows 10 1909)
   ⚠️ CONSIDERAR: Atualizar para 10.0.22621.0 (Windows 11 22H2)
✅ StartupTask: Enabled="false" (correto - usuário escolhe)

[ ] Executor: Validar que todos os elementos acima estão presentes
[ ] Resultado: Nenhuma alteração necessária / Documentar mudanças
```

---

## FASE 2: CONTEÚDO PARA A LOJA (Dias 4-10)

### 2.1 Criar Política de Privacidade

**Arquivo:** `PRIVACY_POLICY.md` (na raiz do repo)  
**Template fornecido:** Ver próxima seção  
**Duração:** 1-2 horas

**Localização final:**
```
https://raw.githubusercontent.com/adrianoramos/aprint/main/PRIVACY_POLICY.md
```

**Atualizar manifesto:**
```xml
<Properties>
  <PrivacyUri>https://github.com/adrianoramos/aprint/blob/main/PRIVACY_POLICY.md</PrivacyUri>
</Properties>
```

### 2.2 Preparar Screenshots

**Requerimentos Microsoft Store:**
- Mínimo: 1 screenshot
- Recomendado: 5-9 screenshots
- Formato: PNG ou JPEG
- Tamanho: Exatamente 1920x1080 (ou 1280x720)
- Sem logos/branding de terceiros

**Sugestão de screenshots a capturar:**
```
1️⃣  Interface principal com system tray
    └─ Menu aberto mostrando "Capture", "Settings", "Session"

2️⃣  Region selection (captura em progresso)
    └─ Toolbar flutuante com ferramentas

3️⃣  Editor de imagem
    └─ Demonstrando: desenho, formas, texto, máscaras

4️⃣  OCR automático (redação sugerida)
    └─ Sugestões de máscara ao lado da imagem

5️⃣  Integração Word
    └─ Documento aberto com capturas anexadas

6️⃣  Settings (configurações)
    └─ Abas: Shortcut, Capture, Session, Editor, General

7️⃣  Multiple monitors (se aplicável)
    └─ Seleção de monitor durante captura

8️⃣  Notification de captura completa
    └─ Feedback visual do aplicativo
```

**Ferramentas sugeridas:**
- Usar o próprio Aprint para capturar e anotar screenshots! 🎯
- Redimensionar para 1920x1080 no editor
- Adicionar anotações destacando recursos-chave

**Deadline:** Dia 7

### 2.3 Descrição Localizada

**Arquivo:** Preparar em Partner Center  
**Idiomas:** en-US, pt-BR, es  
**Duração:** 2 horas

#### 2.3.1 Descrição em Português (pt-BR)

```
TÍTULO (máx. 50 caracteres):
Aprint - Studio de Captura

DESCRIÇÃO CURTA (máx. 80 caracteres):
Capturar, anotar e redacionar screenshots profissionalmente

DESCRIÇÃO COMPLETA (máx. 10.000 caracteres):

Aprint é um studio de captura profissional para Windows.

Capture qualquer região de sua tela, anote enquanto seleciona ou após no editor dedicado. Com suporte a desenho livre, formas, texto, máscaras e OCR automático.

✨ PRINCIPAIS RECURSOS:

🎯 Captura Rápida
  • Atalho global por padrão: Ctrl+Alt+P
  • Capturar todos os monitores, o monitor atual ou seleção livre
  • Ações configuráveis: copiar, salvar, enviar ao editor

✏️ Editor Integrado
  • Desenho à mão livre, linhas, setas
  • Formas: retângulos, círculos (sólidos ou vazios)
  • Texto formatado (fonte, tamanho, cores, estilos)
  • Realce translúcido
  • Contadores numerados (ótimo para tutoriais)
  • Máscaras: sólidas ou pixeladas

🔍 Redação Inteligente
  • OCR automático (Windows OCR local)
  • Sugestões automáticas de máscaras para texto
  • Controle total - sem envio de dados a serviços externos

📄 Integração Word
  • Crie sessões a partir de templates .docx
  • Adicione capturas a documentos Word
  • Quebras de página automáticas
  • Salve em qualquer pasta configurada

⚙️ Configuração Personalizada
  • Atalhos de teclado customizáveis
  • Preferências de captura (modo, destino)
  • Inicialização automática com Windows
  • Suporte a múltiplos idiomas
  • Notificações (ativa/desativa)

🔒 Privacidade & Performance
  • Processamento 100% local - sem upload de imagens
  • Sem telemetria
  • Sem envio de dados pessoais
  • OCR executa offline no seu Windows
  • Rápido e eficiente

📋 CASOS DE USO:
  • Documentação técnica e tutoriais
  • Treinamento corporativo
  • Suporte ao cliente e bug reports
  • Comunicação visual em equipes
  • Redação de informações sensíveis

REQUISITOS:
  • Windows 10 (versão 1809) ou superior
  • Sem dependências adicionais (runtime incluído)

Código aberto: https://github.com/adrianoramos/aprint
Reporte issues: https://github.com/adrianoramos/aprint/issues
Política de Privacidade: Veja link abaixo
```

#### 2.3.2 Descrição em Inglês (en-US)

```
TITLE:
Aprint - Screenshot Studio

SHORT DESCRIPTION:
Capture, annotate, and redact screenshots professionally

FULL DESCRIPTION:

Aprint is a professional screenshot studio for Windows.

Capture any region of your screen, annotate while selecting or in the dedicated editor. Support for freehand drawing, shapes, text, masks, and automatic OCR.

✨ KEY FEATURES:

🎯 Quick Capture
  • Global keyboard shortcut by default: Ctrl+Alt+P
  • Capture all monitors, current monitor, or free selection
  • Configurable actions: copy, save, send to editor

✏️ Integrated Editor
  • Freehand drawing, lines, arrows
  • Shapes: rectangles, circles (filled or outlined)
  • Formatted text (font, size, colors, styles)
  • Translucent highlighting
  • Numbered markers (great for tutorials)
  • Masks: solid or pixelated

🔍 Smart Redaction
  • Automatic OCR (Windows OCR local)
  • Auto-suggest masks for detected text
  • Full control - no data sent to external services

📄 Word Integration
  • Create sessions from .docx templates
  • Add captures to Word documents
  • Automatic page breaks
  • Save to any configured folder

⚙️ Customizable Setup
  • Custom keyboard shortcuts
  • Capture preferences (mode, destination)
  • Automatic startup with Windows
  • Multi-language support
  • Notifications (enable/disable)

🔒 Privacy & Performance
  • 100% local processing - no image uploads
  • No telemetry
  • No personal data collection
  • OCR runs offline on your Windows
  • Fast and efficient

📋 USE CASES:
  • Technical documentation and tutorials
  • Corporate training
  • Customer support and bug reports
  • Visual team communication
  • Sensitive information redaction

REQUIREMENTS:
  • Windows 10 (version 1809) or later
  • No additional dependencies (runtime included)

Open source: https://github.com/adrianoramos/aprint
Report issues: https://github.com/adrianoramos/aprint/issues
Privacy Policy: See link below
```

#### 2.3.3 Descrição em Espanhol (es)

```
TÍTULO:
Aprint - Estudio de Captura

DESCRIPCIÓN CORTA:
Capturar, anotar y redactar capturas de pantalla profesionalmente

DESCRIPCIÓN COMPLETA:

Aprint es un estudio de captura profesional para Windows.

Capture cualquier región de su pantalla, anote mientras selecciona o en el editor dedicado. Compatible con dibujo libre, formas, texto, máscaras y OCR automático.

✨ CARACTERÍSTICAS PRINCIPALES:

🎯 Captura Rápida
  • Atajo de teclado global por defecto: Ctrl+Alt+P
  • Capturar todos los monitores, monitor actual o selección libre
  • Acciones configurables: copiar, guardar, enviar al editor

✏️ Editor Integrado
  • Dibujo a mano alzada, líneas, flechas
  • Formas: rectángulos, círculos (rellenos u opacos)
  • Texto formateado (fuente, tamaño, colores, estilos)
  • Resaltado translúcido
  • Marcadores numerados (excelente para tutoriales)
  • Máscaras: sólidas o pixeladas

🔍 Redacción Inteligente
  • OCR automático (OCR de Windows local)
  • Sugerencias automáticas de máscaras para texto detectado
  • Control total - sin envío de datos a servicios externos

📄 Integración Word
  • Crear sesiones desde plantillas .docx
  • Agregar capturas a documentos Word
  • Saltos de página automáticos
  • Guardar en cualquier carpeta configurada

⚙️ Configuración Personalizable
  • Atajos de teclado personalizados
  • Preferencias de captura (modo, destino)
  • Inicio automático con Windows
  • Soporte multiidioma
  • Notificaciones (activar/desactivar)

🔒 Privacidad y Rendimiento
  • Procesamiento 100% local - sin carga de imágenes
  • Sin telemetría
  • Sin recopilación de datos personales
  • OCR se ejecuta sin conexión en su Windows
  • Rápido y eficiente

📋 CASOS DE USO:
  • Documentación técnica y tutoriales
  • Capacitación corporativa
  • Soporte al cliente e informes de errores
  • Comunicación visual en equipo
  • Redacción de información sensible

REQUISITOS:
  • Windows 10 (versión 1809) o posterior
  • Sin dependencias adicionales (runtime incluido)

Código abierto: https://github.com/adrianoramos/aprint
Reportar problemas: https://github.com/adrianoramos/aprint/issues
Política de Privacidad: Ver enlace abajo
```

---

## FASE 3: TESTES E VALIDAÇÃO (Dias 11-15)

### 3.1 Build MSIX Local

```powershell
# Terminal: PowerShell 5.1+
$Version = "1.0.0"

cd C:\Users\adria\source\repos\adrianoramos\aprint

# Build
.\installer\scripts\build-msix.ps1 -Version $Version
# Gera: artifacts/msix/Aprint-1.0.0-x64.msix

# Resultado esperado:
# "Unsigned MSIX created: artifacts/msix/Aprint-1.0.0-x64.msix"
```

### 3.2 Validar MSIX

```powershell
# Executar testes automatizados
.\tests\Msix.Tests.ps1 -PackagePath artifacts/msix/Aprint-1.0.0-x64.msix -Version $Version

# Resultado esperado:
# "MSIX package tests passed."
```

### 3.3 Windows App Certification Kit (WACK)

**Download:** https://www.microsoft.com/store/apps/9nblggh4nns1

**Executar:**
```
1. Abrir Windows App Certification Kit
2. Clicar em "Validate App"
3. Selecionar o MSIX gerado
4. Executar testes
5. Revisar relatório detalhado
```

**Pontos críticos a validar:**
- ✅ Estrutura MSIX bem-formada
- ✅ Manifesto XML válido
- ✅ Binários compilados corretamente
- ✅ Assets de ícone dimensões corretas
- ✅ Sem dependências externas não declaradas
- ✅ Comportamento do aplicativo correto

**Se falhar:**
1. Revisar relatório do WACK
2. Corrigir problemas identificados
3. Reconstruir MSIX
4. Re-executar WACK

---

## FASE 4: SUBMISSÃO (Dias 16-18)

### 4.1 Preparar Partner Center

```
Fazer login: https://partner.microsoft.com/

Ir para: "Apps and games" → "Overview"

Preencher:
[ ] Nome do app: Aprint
[ ] Categorias: Produtividade / Ferramentas
[ ] Descrição: (usar textos da Fase 2)
[ ] Screenshots: (5-9 imagens 1920x1080)
[ ] Política de privacidade: https://github.com/adrianoramos/aprint/blob/main/PRIVACY_POLICY.md
[ ] Website: https://github.com/adrianoramos/aprint
[ ] Classificação IARC: "Everyone"
[ ] Requisitos de sistema: Windows 10 (1809+), .NET incluído
```

### 4.2 Upload do MSIX

```
Partner Center → "Packages"

[ ] Clique em "Upload"
[ ] Selecione: Aprint-1.0.0-x64.msix
[ ] Partner Center assinará automaticamente
[ ] Aguarde validação (geralmente 2-4 horas)
```

### 4.3 Verificar Conformidade

Após upload, Partner Center executará:
- ✅ Testes de manifesto
- ✅ Verificação de vírus/malware
- ✅ Testes de assinatura
- ✅ Verificação de requisitos

**Se tudo OK:** Passar para fase de certificação

---

## FASE 5: CERTIFICAÇÃO E PUBLICAÇÃO (Dias 19-25)

### 5.1 Monitorar Submissão

```
Partner Center → "Submission status"

Estados possíveis:
1. Package validation (1-2 dias) → automático
2. Security testing (1-2 dias) → automático
3. Manual review (1-3 dias) → equipe humana
4. Processing approval (2-6 horas) → preparando publicação
5. Published (automático) → SUCESSO! 🎉
```

### 5.2 Se Rejeitar

**Motivos comuns (e como corrigir):**

1. **Manifesto inválido**
   - Revisar XML
   - Testar novamente com WACK
   - Rebuildar MSIX

2. **Descrição inadequada**
   - Adicionar mais detalhes
   - Remover branding de terceiros
   - Verificar idioma/ortografia

3. **Screenshots com problemas**
   - Redimensionar para 1920x1080 exatamente
   - Remover informações sensíveis
   - Resubmeter

4. **Funcionalidade não corresponde à descrição**
   - Testar app end-to-end
   - Corrigir bugs
   - Atualizar versão
   - Resubmeter

**Resubmissão:** Corrigir → Testar localmente → Rebuildar MSIX → Upload novo

---

## FASE 6: PÓS-PUBLICAÇÃO (Dias 26+)

### 6.1 Monitoramento

```
[ ] Verificar se está visível na Microsoft Store
[ ] Testar instalação via Store
[ ] Monitorar reviews e ratings
[ ] Responder feedback dos usuários
[ ] Rastrear número de downloads
```

### 6.2 Próximas Versões

**Para futuras atualizações:**

```powershell
# Versão 1.0.1 (correção de bug)
$Version = "1.0.1"
.\installer\scripts\build-msix.ps1 -Version $Version
# ... seguir mesmo processo de submissão ...
```

**Dicas:**
- Manter versionamento semântico
- Descrever mudanças no changelog
- Testar com WACK antes de submeter
- Planejar updates a cada 4-8 semanas

---

## 📋 CHECKLIST FINAL

### Antes de Submeter

```
TÉCNICO:
[ ] MSIX gerado sem erros
[ ] Validação MSIX passa (Msix.Tests.ps1)
[ ] Windows App Cert Kit sem erros críticos
[ ] Aplicativo inicia em < 10 segundos
[ ] Sem memory leaks óbvios
[ ] Sem crashes em operações normais
[ ] Funcionalidade de captura funcionando
[ ] OCR funcionando (se Windows OCR disponível)
[ ] Settings persistem entre reinicializações

CONTEÚDO:
[ ] Descrição localizada (en, pt, es)
[ ] Política de privacidade criada e linkada
[ ] Screenshots (mínimo 5) em 1920x1080
[ ] Sem screenshots com informações sensíveis
[ ] Nenhuma propriedade intelectual de terceiros
[ ] Nenhum conteúdo malicioso/ofensivo

CONFORMIDADE:
[ ] Manifesto MSIX com identidade Partner Center
[ ] Capability runFullTrust declarado
[ ] Assets de ícone corretos
[ ] Sem dependências externas não declaradas
[ ] Sem hard-coded credentials/secrets
[ ] Localização de configurações em AppData

PARTNER CENTER:
[ ] Conta registrada e ativa
[ ] Identidade Publisher correspondente
[ ] Categorias selecionadas
[ ] Website/GitHub linkado
[ ] Classificação IARC completa
[ ] Termos de desenvolvedor aceitos
```

### Depois de Publicado

```
[ ] Verificar na Microsoft Store (pode levar 24h)
[ ] Testar instalação completa
[ ] Verificar sistema tray
[ ] Verificar atalhos globais
[ ] Monitorar reviews diários
[ ] Estar pronto para suportar usuários
[ ] Planejar próxima versão
[ ] Documentar lições aprendidas
```

---

## 🎯 CRONOGRAMA REALISTA

| Fase | Duração | Datas (Exemplo) |
|------|---------|-----------------|
| Preparação | 3 dias | 25-27 Nov |
| Conteúdo | 7 dias | 28 Nov - 4 Dez |
| Testes | 5 dias | 5-9 Dez |
| Submissão | 3 dias | 10-12 Dez |
| Certificação | 5-7 dias | 13-19 Dez |
| **PUBLICAÇÃO** | - | **~20 Dez** |

---

## 📞 SUPORTE E RECURSOS

- **Partner Center Help:** https://partner.microsoft.com/help
- **MSIX Docs:** https://learn.microsoft.com/en-us/windows/msix/
- **Store Policies:** https://learn.microsoft.com/en-us/windows/uwp/publish/store-policies
- **WACK Errors:** https://learn.microsoft.com/en-us/windows/uwp/debug-test-perf/windows-app-certification-kit

---

**Próximo passo:** Começar FASE 1 imediatamente! 🚀
