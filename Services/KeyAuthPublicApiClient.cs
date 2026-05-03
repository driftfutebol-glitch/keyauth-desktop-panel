using System.Text.Json;

namespace KeyAuthDesktopPanel.Services;

public sealed class KeyAuthPublicApiClient
{
    private readonly HttpClient _httpClient = new();

    public async Task<KeyAuthLicenseValidationResult> ValidateLicenseAsync(
        string apiUrl,
        string appName,
        string ownerId,
        string key,
        string hwid,
        string version = "1.0",
        CancellationToken ct = default
    )
    {
        var initPayload = new Dictionary<string, string>
        {
            ["type"] = "init",
            ["name"] = appName,
            ["ownerid"] = ownerId,
            ["ver"] = version,
            ["enckey"] = Guid.NewGuid().ToString("N")
        };

        var initResponse = await PostAsync(apiUrl, initPayload, ct);
        if (!initResponse.Success)
        {
            return new KeyAuthLicenseValidationResult(false, $"Init falhou: {initResponse.Message}");
        }

        if (string.IsNullOrWhiteSpace(initResponse.SessionId))
        {
            return new KeyAuthLicenseValidationResult(false, "Init sem sessionid.");
        }

        var licensePayload = new Dictionary<string, string>
        {
            ["type"] = "license",
            ["name"] = appName,
            ["ownerid"] = ownerId,
            ["sessionid"] = initResponse.SessionId,
            ["key"] = key,
            ["hwid"] = hwid
        };

        var licenseResponse = await PostAsync(apiUrl, licensePayload, ct);
        return new KeyAuthLicenseValidationResult(licenseResponse.Success, licenseResponse.Message);
    }

    private async Task<KeyAuthApiRawResponse> PostAsync(
        string apiUrl,
        Dictionary<string, string> payload,
        CancellationToken ct
    )
    {
        using var content = new FormUrlEncodedContent(payload);
        using var response = await _httpClient.PostAsync(apiUrl, content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (string.Equals(body.Trim(), "KeyAuth_Invalid", StringComparison.OrdinalIgnoreCase))
        {
            return new KeyAuthApiRawResponse(false, "Aplicacao invalida no KeyAuth.", null);
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var success = root.TryGetProperty("success", out var s) && s.GetBoolean();
        var message = root.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : "Sem mensagem";
        var sessionId = root.TryGetProperty("sessionid", out var sid) ? sid.GetString() : null;

        return new KeyAuthApiRawResponse(success, message, sessionId);
    }

    private sealed record KeyAuthApiRawResponse(bool Success, string Message, string? SessionId);
}

public sealed record KeyAuthLicenseValidationResult(bool Success, string Message);
