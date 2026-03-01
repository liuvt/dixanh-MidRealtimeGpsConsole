using MidRealtimeGpsConsole.Models;
using System.Text.Json.Serialization;

namespace MidRealtimeGpsConsole.Responses;

public sealed class RefreshTokenResponse
{
    [JsonPropertyName("result")] public bool Result { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("status")] public int? Status { get; set; }
    [JsonPropertyName("data")] public List<RefreshTokenData>? Data { get; set; }
}