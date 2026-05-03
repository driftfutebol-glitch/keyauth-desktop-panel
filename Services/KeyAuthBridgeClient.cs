using System.Text.Json;

namespace KeyAuthDesktopPanel.Services;

public sealed class KeyAuthBridgeClient
{
    private readonly HttpClient _httpClient = new();

    public async Task<KeyAuthBridgeGenerateResult> GenerateAsync(KeyAuthBridgeGenerateRequest request, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, string>
        {
            ["action"] = "generate",
            ["ownerid"] = request.OwnerId,
            ["name"] = request.AppName,
            ["sellerkey"] = request.SellerKey,
            ["amount"] = request.Amount.ToString(),
            ["mask"] = request.Mask,
            ["duration"] = request.Duration.ToString(),
            ["expiry"] = request.Expiry.ToString(),
            ["level"] = request.Level.ToString(),
            ["note"] = request.Note,
            ["character"] = request.CharacterMode.ToString()
        };

        using var content = new FormUrlEncodedContent(payload);
        using var response = await _httpClient.PostAsync(request.BridgeUrl, content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var success = root.TryGetProperty("success", out var s) && s.GetBoolean();
        var message = root.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : "No message.";

        var keys = new List<string>();
        if (root.TryGetProperty("keys", out var keysProp) && keysProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in keysProp.EnumerateArray())
            {
                var key = item.GetString();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    keys.Add(key);
                }
            }
        }

        return new KeyAuthBridgeGenerateResult(success, message, keys);
    }
}

public sealed record KeyAuthBridgeGenerateRequest(
    string BridgeUrl,
    string OwnerId,
    string AppName,
    string SellerKey,
    int Amount,
    string Mask,
    int Duration,
    int Expiry,
    int Level,
    string Note,
    int CharacterMode
);

public sealed record KeyAuthBridgeGenerateResult(
    bool Success,
    string Message,
    IReadOnlyList<string> Keys
);
