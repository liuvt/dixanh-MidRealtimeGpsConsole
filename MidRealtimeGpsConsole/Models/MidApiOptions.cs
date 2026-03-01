namespace MidRealtimeGpsConsole.Models;
public sealed class MidApiOptions
{
    public string BaseUrl { get; set; } = "https://api-gw.midvietnam.net";
    public string ApiKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SignatureMode { get; set; } = "HmacSha256";
    public string SecretKeyHash { get; set; } = string.Empty;
    public string? PrivateKeyPemPath { get; set; }
    public int RealtimeIntervalSeconds { get; set; } = 15;
    public int RefreshFallbackMinutes { get; set; } = 20;
    public int RefreshSkewSeconds { get; set; } = 300;
    public int RequestTimeoutSeconds { get; set; } = 30;
}
