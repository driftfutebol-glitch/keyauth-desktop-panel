namespace KeyAuthDesktopPanel.Services;

public static class LicenseKeyGenerator
{
    private const string Charset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Create(string appName)
    {
        var appCode = new string(
            appName
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .Take(4)
                .ToArray()
        );

        if (appCode.Length < 4)
        {
            appCode = appCode.PadRight(4, 'X');
        }

        return $"KAUTH-{appCode}-{Segment(4)}-{Segment(4)}-{Segment(4)}";
    }

    private static string Segment(int length)
    {
        Span<byte> bytes = stackalloc byte[length];
        Random.Shared.NextBytes(bytes);

        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Charset[bytes[i] % Charset.Length];
        }

        return new string(chars);
    }
}
