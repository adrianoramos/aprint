# Aprint

O Aprint é um aplicativo Windows para capturar, anotar e compartilhar screenshots. Ele fica na bandeja do sistema e permite capturar todos os monitores, o monitor sob o cursor ou uma região escolhida. Antes de finalizar uma captura de região, você pode anotar a imagem na barra flutuante, enviá-la ao editor, copiá-la ou salvá-la.

## Guia de uso

### 1. Iniciar uma captura

Use o atalho global `Ctrl+Shift+P` (padrão), pressione `Print Screen` se essa opção estiver ativada, ou abra o menu do ícone do Aprint na bandeja e escolha **Capturar**. Um clique duplo no ícone também inicia a captura. O atalho e o modo de captura podem ser alterados em **Configurações**.

Em **Configurações > Captura**, escolha o que será capturado:

- **Todas as telas**: cria uma imagem da área combinada dos monitores.
- **Tela do cursor**: captura o monitor onde o ponteiro está.
- **Selecionar região**: permite arrastar uma área da tela. Em uma configuração com vários monitores, use a barra no alto para escolher a tela.

Ao selecionar uma região, arraste para delimitá-la. O comportamento de um clique fora da seleção (iniciar outra seleção, sair do modo de captura ou não fazer nada) também pode ser definido nas configurações.

### 2. Anotar antes de finalizar a seleção

Na captura por região, use a barra flutuante para desenhar ou destacar diretamente sobre a área selecionada. Abra o menu de ferramentas para escolher desenho livre, linha, seta, retângulo, círculo, contador, texto ou máscara. Os menus de opções permitem ajustar cores, preenchimento, espessura, tamanho e formatação de texto; as preferências de ferramentas são reaproveitadas no editor.

Quando terminar, escolha uma ação na barra ou no menu ao lado do botão principal:

- **Capturar** ou `Enter`: concluir a captura.
- **Enviar para o editor** ou `Ctrl+Enter`: continuar a edição na janela do editor.
- **Copiar** ou `Ctrl+C`: enviar a imagem para a área de transferência.
- **Salvar** ou `Ctrl+S`: gravar a imagem na pasta configurada.

### 3. Editar e revisar

Ative uma ferramenta na barra do editor e desenhe sobre a imagem. Arraste para criar linhas, setas, formas, destaques e máscaras; use a ferramenta de texto para inserir uma anotação e a de contador para numerar etapas. Retângulos podem ter preenchimento. Use **Desfazer** e **Refazer**, ou `Ctrl+Z` e `Ctrl+Y`, para revisar as alterações.

As opções da ferramenta ativa ficam no menu correspondente. Por exemplo, escolha a cor e a espessura do traço, a cor e o tamanho do destaque, as cores do texto e do círculo do contador, ou uma cor de preenchimento. Essas preferências são lembradas entre capturas. Para ocultar dados sensíveis, escolha máscara sólida ou pixelada e ajuste a cor ou o tamanho dos blocos; `F8` alterna o modo de máscara.

Para cortar a imagem, pressione `,` ou `F11`, arraste as alças para enquadrar o conteúdo e confirme com `Enter`. Use `Esc` para sair do corte sem aplicá-lo.

### 4. Aplicar máscara automática

No editor, escolha **Máscara automática** ou pressione `Ctrl+F8`. Aguarde a detecção de texto, clique nas regiões para incluir ou excluir cada uma e, se necessário, arraste para adicionar uma região manual. Mantenha `Espaço` pressionado para revelar temporariamente o conteúdo coberto e solte para voltar a ocultá-lo. Pressione `Enter` ou **Aplicar máscaras** para confirmar; use `Esc` para cancelar.

A máscara automática também está disponível na barra flutuante durante a seleção de região. A detecção usa o OCR do Windows e é executada localmente. O idioma de OCR correspondente precisa estar instalado; se o recurso não estiver disponível, o Aprint exibirá um aviso e você poderá aplicar máscaras manualmente.

### 5. Copiar, salvar e abrir o resultado

No editor, use `Ctrl+C` para copiar ou `Ctrl+S` para salvar. Também é possível usar os botões correspondentes na barra. Em **Configurações > Captura**, defina se uma captura deve ir para a área de transferência, ser salva em disco ou fazer as duas coisas, e escolha a pasta de destino. Ao salvar pelo Aprint, os arquivos PNG e JPEG recebem metadados que identificam o software.

As notificações podem ser ativadas ou desativadas. Configure o clique na notificação para não fazer nada, abrir a pasta, abrir o arquivo ou abrir a captura no editor. Também é possível ativar o som após a captura.

### 6. Personalizar o fluxo

Abra **Configurações** pelo menu do Aprint na bandeja para ajustar:

- Atalho global ou uso da tecla `Print Screen`.
- Modo e destino padrão das capturas, pasta de salvamento e ação ao clicar fora da seleção.
- Abertura automática do editor, tamanho inicial da janela e exibição de rótulos nas ferramentas.
- Ferramenta inicial do editor ou opção para lembrar a última ferramenta usada.
- Salvamento sem confirmação, fechamento do editor após copiar/salvar e fechamento automático após um período sem atividade.
- Notificações, ação ao clicar nelas, som, idioma e inicialização com o Windows.

O Aprint é destinado a fluxos que exigem capturas frequentes, como documentação, suporte técnico e desenvolvimento de software. Capturas e detecção de texto são processadas localmente, sem enviar imagens ou texto a serviços externos.

### Atalhos do editor

| Atalho | Ação |
| --- | --- |
| `F2` | Desenho livre |
| `F3` / `F4` | Linha / seta |
| `F5` / `F6` | Retângulo / círculo |
| `F7` / `F8` | Destaque / alternar modo de máscara |
| `F9` / `F10` | Contador / texto |
| `,` / `F11` | Ativar corte |
| `Ctrl+F8` | Iniciar máscara automática |
| `Ctrl+Z` / `Ctrl+Y` | Desfazer / refazer |
| `Ctrl+C` / `Ctrl+S` | Copiar / salvar |
| `Ctrl+N` | Fazer nova captura |

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