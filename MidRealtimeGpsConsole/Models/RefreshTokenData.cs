using System.Text.Json.Serialization;

namespace MidRealtimeGpsConsole.Models;

public sealed class RefreshTokenData
{
    [JsonPropertyName("token")] public string Token { get; set; } = string.Empty;
}

public sealed record RefreshTokenRequest(
    [property: JsonPropertyName("refresh_token")] string RefreshToken);