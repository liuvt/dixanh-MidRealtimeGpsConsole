using MidRealtimeGpsConsole.Models;
using System.Text.Json.Serialization;

namespace MidRealtimeGpsConsole.Responses;

public sealed class LoginResponse
{
    [JsonPropertyName("result")] public bool Result { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("status")] public int? Status { get; set; }
    [JsonPropertyName("data")] public List<LoginData>? Data { get; set; }
}

public sealed record LoginRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password);