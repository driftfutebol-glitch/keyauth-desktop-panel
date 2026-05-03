# Integracao do App Cliente

Use o `KeyAuthDesktopPanel.exe` apenas como painel admin para gerar keys.

## Dados que ficam no painel admin

- `Bridge URL`
- `API URL`
- `OwnerID`
- `SellerKey`
- `Versao cliente`

## Dados que entram no app do cliente

- `API URL`
- `AppName`
- `OwnerID`
- `Versao cliente`

## Regra importante

Nunca coloque `SellerKey` dentro do app do cliente.

## Fluxo

1. No painel admin, configure `AppName`, `OwnerID`, `SellerKey`, `Bridge URL` e `API URL`.
2. Gere a key com `7`, `10`, `20` ou `lifetime`.
3. Clique em `Copiar Config Cliente`.
4. Cole o trecho no seu app C#.
5. No seu app, leia a key digitada pelo usuario.
6. Valide a key via `KeyAuthPublicApiClient.ValidateLicenseAsync(...)`.
7. Se `Success == true`, libera a area protegida do seu software.

## Exemplo de uso no app cliente

```csharp
private const string ApiUrl = "https://SEU_DOMINIO/keyauth-source/api/1.2/";
private const string AppName = "SEU_APP";
private const string OwnerId = "SEU_OWNER_ID";
private const string Version = "1.0";

private async Task<bool> ValidarKeyAsync(string keyDigitada)
{
    var hwid = $"{Environment.MachineName}-{Environment.UserName}";
    var keyAuth = new KeyAuthPublicApiClient();
    var result = await keyAuth.ValidateLicenseAsync(ApiUrl, AppName, OwnerId, keyDigitada, hwid, Version);
    return result.Success;
}
```

## Duracoes

No painel admin, o valor padrao fica em `7` dias.

Voce tambem pode usar:
- `10`
- `20`
- `30`
- `90`
- `365`
- `lifetime`

No modo `lifetime`, o painel usa uma expiracao longa para representar acesso permanente.
