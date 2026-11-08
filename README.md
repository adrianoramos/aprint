# Aprint

Aprint é um aplicativo Windows para capturar, anotar, ocultar informações e compartilhar screenshots. Ele permanece na bandeja do sistema e foi pensado para quem repete esse fluxo em documentação, suporte, treinamento e desenvolvimento: capture, marque o que importa, revise e copie ou salve sem precisar abrir um editor separado para cada imagem.

## Recursos

- Captura de todos os monitores, do monitor sob o cursor ou de uma região retangular selecionada.
- Anotações durante a seleção da região ou depois, em uma janela de edição dedicada.
- Desenho livre, linhas, setas, retângulos, círculos, realce semitransparente, texto formatado e marcadores numerados.
- Máscaras sólidas ou pixeladas, aplicadas manualmente ou em regiões de texto sugeridas pelo OCR do Windows.
- Recorte, desfazer/refazer e preferências de ferramentas lembradas entre capturas.
- Fluxos configuráveis para copiar, salvar, abrir no editor ou copiar e salvar; atalhos globais, notificações e inicialização com o Windows.

## Começar

Depois de iniciar o Aprint, procure o ícone na bandeja do sistema. O aplicativo não precisa manter uma janela principal aberta.

Para iniciar uma captura, use `Ctrl+Shift+P` (atalho padrão), dê um clique duplo no ícone ou escolha **Capturar** no menu da bandeja. Também é possível configurar `Print Screen` como atalho. O menu da bandeja permite alternar entre a captura configurada e a seleção de região. Por padrão, o Aprint inicia no modo de região e copia a captura concluída para a área de transferência.

Em **Configurações > Captura**, escolha o modo:

- **Todas as telas** captura a área virtual combinada dos monitores.
- **Tela do cursor** captura o monitor onde o ponteiro está.
- **Selecionar região** permite arrastar um retângulo sobre a área virtual. Em uma configuração com vários monitores, a barra no alto também permite capturar todos os monitores, o monitor sob o cursor ou uma tela específica.

Na seleção de região, arraste para definir o retângulo. As opções para um clique fora da seleção são iniciar outra seleção, sair do modo de captura ou ignorar o clique. Pressione `Esc` para cancelar.

## Anotar durante a captura

Com uma região selecionada, a barra flutuante permite marcar a imagem antes de concluí-la. Selecione uma ferramenta e arraste sobre a região; abra a seta/menu da ferramenta para ajustar suas opções. Desenho livre, linha, seta, retângulo, círculo, contador, texto, realce e máscaras estão disponíveis nesse fluxo. Retângulos e círculos podem ser preenchidos. As configurações de cor, espessura, tamanho e texto são reutilizadas no editor.

Escolha a ação na barra flutuante ou use:

| Atalho | Ação |
| --- | --- |
| `Enter` | Concluir a captura conforme as configurações |
| `Ctrl+Enter` | Enviar a imagem para o editor |
| `Ctrl+C` | Copiar a imagem para a área de transferência |
| `Ctrl+S` | Salvar a imagem |
| `Esc` | Cancelar a seleção |

## Editar a captura

Use **Enviar para o editor** na barra flutuante ou ative **Abrir editor após a captura** nas configurações. Na janela do editor, selecione uma ferramenta na barra e arraste sobre a imagem. O botão de nova captura inicia outra captura sem exigir que você volte ao menu da bandeja.

- **Desenho e formas:** desenho livre, linha e seta; retângulos e círculos com ou sem preenchimento. Ajuste cor e espessura do traço.
- **Realce:** marca áreas com um traço semitransparente; ajuste cor e largura.
- **Texto:** insira texto e escolha fonte, tamanho, cor e estilos como negrito, itálico, sublinhado e tachado.
- **Contador:** coloque marcadores numerados para indicar etapas; ajuste tamanho e cores do número e do círculo.
- **Máscaras:** cubra uma área com uma cor sólida ou pixelize-a. É possível escolher cor e tamanho dos blocos. `F8` alterna entre os modos de máscara.
- **Recorte:** ative a ferramenta, arraste as alças para ajustar o enquadramento e confirme com `Enter`; `Esc` cancela o recorte.
- **Revisão:** use desfazer/refazer para percorrer alterações recentes.

As opções visuais das ferramentas são persistidas e reaproveitadas em novas capturas. Para copiar ou salvar, use os botões da barra ou `Ctrl+C` e `Ctrl+S`. O salvamento automático usa a pasta configurada; a opção **Salvar sem confirmação** controla se o editor abre o diálogo de gravação. Pelo diálogo, o Aprint salva PNG, JPEG ou BMP. PNG e JPEG recebem metadados que identificam o Aprint.

### Atalhos do editor

| Atalho | Ação |
| --- | --- |
| `F2` | Desenho livre |
| `F3` / `F4` | Linha / seta |
| `F5` / `F6` | Retângulo / círculo |
| `F7` / `F8` | Realce / máscara |
| `F9` / `F10` | Contador / texto |
| `,` / `F11` | Ativar recorte |
| `Ctrl+F8` | Iniciar máscara automática |
| `Ctrl+Z` / `Ctrl+Y` | Desfazer / refazer |
| `Ctrl+C` / `Ctrl+S` | Copiar / salvar |
| `Ctrl+N` | Fazer nova captura |

## Máscara automática

No editor, escolha **Máscara automática** no menu da ferramenta de máscara ou pressione `Ctrl+F8`. Na captura por região, a mesma opção fica no menu de máscaras da barra flutuante.

O Aprint usa o OCR local do Windows para sugerir retângulos em torno de linhas de texto. Aguarde a detecção, clique em cada região para incluir ou excluir a máscara e arraste para adicionar áreas manualmente. Mantenha `Espaço` pressionado para revelar temporariamente a imagem original e solte para ocultá-la novamente. Confirme com `Enter` ou **Aplicar máscaras**; `Esc` cancela.

Esse recurso aplica máscaras visuais; não extrai nem valida o texto detectado, e a detecção pode não encontrar todos os dados sensíveis. Revise as sugestões e acrescente máscaras manuais antes de compartilhar. O reconhecimento ocorre localmente e depende dos idiomas de OCR instalados no Windows. Se o mecanismo ou idioma necessário não estiver disponível, use as máscaras manuais.

## Configurações

Abra **Configurações** pelo menu da bandeja. As opções incluem:

- **Atalho:** escolha uma combinação global de modificadores e tecla, ou use `Print Screen`.
- **Captura:** modo padrão, ação ao concluir (área de transferência, disco ou ambos), pasta de destino e comportamento ao clicar fora da região.
- **Editor:** abertura automática, estado inicial da janela, ferramenta padrão ou última ferramenta usada, rótulos nas ferramentas, salvamento sem diálogo, fechamento após copiar/salvar e fechamento por inatividade (30 segundos, 1, 3 ou 5 minutos).
- **Geral:** notificações e ação ao clicar nelas (não fazer nada, abrir a pasta, abrir o arquivo ou abrir a captura no editor), som após a captura, idioma e inicialização com o Windows.

O idioma pode seguir o Windows ou ser definido como português do Brasil, inglês dos Estados Unidos ou espanhol. As alterações da janela de configurações só são aplicadas ao salvar.

## Aprint e a Snipping Tool do Windows

As ferramentas se sobrepõem na captura de screenshots, mas atendem a fluxos diferentes. Os recursos da Snipping Tool variam conforme a versão do Windows e, em alguns casos, o hardware. A página oficial da Microsoft documenta os recursos e essa disponibilidade: [Usar a Snipping Tool para capturar screenshots](https://support.microsoft.com/en-us/windows/open-snipping-tool-and-take-a-screenshot-00246869-1843-655f-f220-97299b865f6b).

| Necessidade | Aprint | Snipping Tool do Windows |
| --- | --- | --- |
| Tipos de captura | Todos os monitores, monitor sob o cursor ou região retangular | Região retangular, janela, tela inteira e, nas versões atuais, captura de vídeo |
| Anotar | Ferramentas de desenho, formas, realce, texto formatado, contador numerado e máscaras | Caneta, realce, formas, borracha, recorte e desfazer/refazer |
| Ocultar informação | Máscara sólida ou pixelada, manual ou sugerida sobre regiões de texto pelo OCR local | Ações de texto para copiar conteúdo e censura rápida de e-mails e telefones reconhecidos |
| Fluxo recorrente | Aplicativo na bandeja, atalho configurável, editor dedicado, preferências lembradas e destino de captura configurável | Integração nativa do Windows, atalho do sistema e fluxo de captura/salvamento da própria ferramenta |
| Outros recursos | Contador numerado, anotação antes de concluir a seleção e preferências de ferramentas persistentes | Recorte livre, captura de janela, gravação de tela e recursos adicionais que dependem do Windows e do dispositivo |

O Aprint é especialmente útil quando você captura imagens com frequência e precisa anotá-las, numerar etapas, ocultar áreas e encaminhar cada resultado sempre do mesmo modo. A Snipping Tool é a opção adequada quando você precisa gravar vídeo, capturar uma janela ou forma livre, extrair texto ou usar recursos exclusivos da versão do Windows. O Aprint não grava vídeo, não captura janelas individuais nem oferece seleção de forma livre ou extração de texto.

## Privacidade e processamento

Capturas, edição e detecção de texto são feitas localmente no computador; o Aprint não envia imagens ou texto reconhecido a serviços externos. Conforme as ações escolhidas, a imagem pode ser copiada para a área de transferência ou salva em disco. A máscara automática é uma assistência: ela não substitui a revisão manual, e a revelação temporária com `Espaço` mostra o conteúdo original na tela.

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

As configurações do usuário são carregadas e salvas pelo `SettingsStore`. O aplicativo é exclusivo para Windows e usa APIs do Windows para a bandeja, atalhos globais, área de transferência, captura de tela e OCR.
