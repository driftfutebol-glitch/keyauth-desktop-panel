namespace KeyAuthDesktopPanel.Models;

public sealed class LicenseRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Key { get; init; } = string.Empty;
    public string AppName { get; init; } = string.Empty;
    public string Buyer { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; init; } = DateTimeOffset.UtcNow.AddDays(30);
    public bool Active { get; set; } = true;
    public int Activations { get; set; }
    public DateTimeOffset? LastValidatedAtUtc { get; set; }
}
