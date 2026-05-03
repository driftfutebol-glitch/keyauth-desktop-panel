# KeyAuthDesktopPanel

Painel desktop (`.exe`) para gerenciamento local de licencas, sem dependencia do site.

## Funcionalidades
- Gerar key por app/cliente com expiracao.
- Validar key (ativa, expirada, revogada).
- Revogar e excluir key.
- Exportar todas as keys em JSON.
- Persistencia local em SQLite.
- Gerar key direto no servidor KeyAuth via bridge (`api/desktop`).
- Validar key online no endpoint KeyAuth `api/1.2` (init + license).

## Banco local
- Caminho do banco: `%LOCALAPPDATA%\KeyAuthDesktopPanel\licenses.db`

## Executavel gerado
- `C:\Users\User\Documents\keyauth\KeyAuthDesktopPanel\bin\Release\net10.0-windows\win-x64\publish\KeyAuthDesktopPanel.exe`

## Ligacao com a source KeyAuth
- Endpoint bridge criado em:
  - `C:\Users\User\Documents\keyauth\keyauth-source\api\desktop\index.php`
- Esse endpoint gera key na mesma tabela de licencas da source, entao a key criada no `.exe` tambem funciona no site/API.

## Build manual
```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

## Sugestao de repositorio GitHub
- Nome sugerido: `keyauth-desktop-panel`

## Publicar no GitHub
```powershell
.\tools\publish-github.ps1 -RepoUrl "https://github.com/SEU_USUARIO/keyauth-desktop-panel.git"
```
