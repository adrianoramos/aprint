# Aprint

O Aprint é um aplicativo Windows para capturar, editar e compartilhar screenshots com rapidez. Ele permanece disponível na bandeja do sistema e permite iniciar uma captura pelo menu da aplicação, por um atalho global configurável ou pela tecla Print Screen.

Depois da captura, a imagem pode ser copiada para o clipboard, salva em uma pasta configurável ou aberta no editor integrado. O editor oferece ferramentas para destacar informações, apagar áreas sensíveis e revisar a imagem antes de copiá-la ou salvá-la.

### Principais recursos

- Captura de tela por atalho global, menu da bandeja ou tecla Print Screen.
- Seleção da região da tela a ser capturada.
- Editor integrado para anotações e ocultação de informações sensíveis.
- Ferramenta opcional **Máscara automática** no editor; selecione as linhas detectadas, revele-as temporariamente com `Espaço` e confirme com `Enter` ou **Aplicar máscaras**.
- Corte da captura no editor: pressione `,`, arraste as alças nas bordas e confirme com Enter ou cancele com Esc.
- Cópia direta da imagem para o clipboard.
- Salvamento em pasta configurável.
- Notificações após capturas, cópias e salvamentos.
- Abertura da imagem capturada, do arquivo salvo ou da pasta de destino pela notificação.
- Preferências de idioma, atalhos, comportamento do editor e inicialização com o Windows.

O Aprint foi desenvolvido para fluxos de trabalho que exigem capturas frequentes, como documentação, suporte técnico, desenvolvimento de software e comunicação entre equipes. A ferramenta opcional de mascaramento e as capturas são processadas localmente, sem enviar imagens ou texto a serviços externos. O idioma de OCR precisa estar disponível no Windows; se a ferramenta não puder ser executada, nenhuma máscara automática será aplicada e o usuário ainda poderá editar ou compartilhar a imagem normalmente.

## Requisitos

- Windows 10 versão 1809 ou superior.
- .NET 8 SDK para desenvolvimento.
- .NET 8 Desktop Runtime para executar uma publicação framework-dependent.
- Inno Setup 6 para gerar o instalador.

## Estrutura

```text
src/Aprint/       Aplicação WinForms e funcionalidades do produto
tests/Aprint.Tests/ Testes automatizados
installer/scripts/ Script do Inno Setup e automação de release
artifacts/        Saídas locais de publicação e build
```

## Desenvolvimento

Restaurar dependências e compilar a solução:

```powershell
dotnet restore Aprint.sln
dotnet build Aprint.sln
```

Executar os testes:

```powershell
dotnet test Aprint.sln
```

Executar a aplicação:

```powershell
dotnet run --project src/Aprint/Aprint.csproj
```

## Publicação e instalador

Publicar a aplicação em `artifacts/publish`:

```powershell
$Version = '1.0.0'
dotnet publish src/Aprint/Aprint.csproj --configuration Release --output artifacts/publish "-p:Version=$Version"
```

Gerar a publicação e o instalador em uma única etapa:

```powershell
.\installer\scripts\build-installer.ps1 -Version 1.0.0
```

O script usa a versão informada no comando para a aplicação e para o instalador. O workflow do GitHub também calcula a versão e a aplica ao build, à publicação e ao instalador. Não há uma versão padrão mantida em um arquivo MSBuild compartilhado.

O instalador é framework-dependent e requer o .NET 8 Desktop Runtime no computador de destino. As saídas de `artifacts/`, `bin/` e `obj/` são geradas localmente e não devem ser versionadas.

## Configurações

As configurações do usuário são carregadas e salvas pelo `SettingsStore`. A aplicação é Windows-only e utiliza APIs do Windows para a bandeja, hotkeys globais, clipboard e captura de tela.