using System.Text.Json.Serialization;

namespace MidRealtimeGpsConsole.Models;

public sealed class LoginData
{
    [JsonPropertyName("token")] public string Token { get; set; } = string.Empty;
    [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; } = string.Empty;
}
