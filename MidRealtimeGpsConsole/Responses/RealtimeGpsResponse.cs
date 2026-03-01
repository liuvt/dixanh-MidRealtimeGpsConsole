using MidRealtimeGpsConsole.Models;
using System.Text.Json.Serialization;

namespace MidRealtimeGpsConsole.Responses;

public sealed class RealtimeGpsResponse
{
    [JsonPropertyName("result")] public bool Result { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("status")] public int? Status { get; set; }
    [JsonPropertyName("totalPage")] public int? TotalPage { get; set; }
    [JsonPropertyName("data")] public Dictionary<string, RealtimeGpsItem>? Data { get; set; }
}
