# UltraBar

UltraBar é uma barra de ferramentas para Windows feita em C#/WinForms. Ela fica dockada em uma borda da tela usando a API de AppBar do Windows e permite guardar atalhos para aplicativos, arquivos e atalhos `.lnk`.

## Como usar

- Clique com o botão direito na barra para adicionar atalhos, mudar a borda ou sair.
- Use `Adicionar separador` para inserir uma divisória visual entre grupos de atalhos.
- Use `Aparência...` no menu da barra para ajustar tamanho do botão, margem interna do botão, margem entre botões, exibição padrão, cor de fundo e transparência.
- Use `Exibição` no menu de cada atalho para mostrar aquele botão como ícone, texto ou ícone + texto.
- Arraste arquivos executáveis ou atalhos para dentro da barra para adicioná-los.
- Arraste atalhos ou separadores dentro da barra para reordenar.
- Clique com o botão direito em um atalho para remover.
- A configuração fica em `%APPDATA%\UltraBar\settings.json`.

## Desenvolvimento

```powershell
dotnet build .\UltraBar.csproj
dotnet run --project .\UltraBar.csproj
```

## Próximos incrementos naturais

- Editar nome e ordem dos atalhos pela interface.
- Opções de tema e auto-iniciar com o Windows.
- Suporte a múltiplos monitores e escolha de monitor.
- Empacotamento em instalador ou executável self-contained.
