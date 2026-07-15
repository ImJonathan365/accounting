using System.Security.Cryptography;

namespace Accounting.Application;

public static class TokenHelper
{
    // For tokens embedded in query params — caller applies Uri.EscapeDataString
    public static string GenerateRaw()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    // For tokens embedded in URL path segments — no percent-encoding needed
    public static string GenerateUrlSafeRaw()
    {
        var bytes = new byte[48];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
