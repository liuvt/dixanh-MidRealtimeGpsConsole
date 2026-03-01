using System.Text.Json.Serialization;

namespace MidRealtimeGpsConsole.Models;

public sealed class RealtimeGpsItem
{
    [JsonPropertyName("vehicle_id")] public int? VehicleId { get; set; }
    [JsonPropertyName("device_id")] public int? DeviceId { get; set; }
    [JsonPropertyName("imei")] public string? Imei { get; set; }
    [JsonPropertyName("latitude")] public string? Latitude { get; set; }
    [JsonPropertyName("longitude")] public string? Longitude { get; set; }
    [JsonPropertyName("speed")] public decimal? Speed { get; set; }
    [JsonPropertyName("rotation")] public decimal? Rotation { get; set; }
    [JsonPropertyName("signal_quality")] public int? SignalQuality { get; set; }
    [JsonPropertyName("status_device")] public int? StatusDevice { get; set; }
    [JsonPropertyName("status")] public int? Status { get; set; }
    [JsonPropertyName("acc")] public int? Acc { get; set; }
    [JsonPropertyName("time")] public long? Time { get; set; }
    [JsonPropertyName("distance")] public long? Distance { get; set; }
    [JsonPropertyName("total_distance")] public long? TotalDistance { get; set; }
    [JsonPropertyName("max_speed")] public decimal? MaxSpeed { get; set; }
    [JsonPropertyName("vehicle_name")] public string? VehicleName { get; set; }
    [JsonPropertyName("vehicle_type_name")] public string? VehicleTypeName { get; set; }
    [JsonPropertyName("name_driver")] public string? NameDriver { get; set; }
    [JsonPropertyName("phone_driver")] public string? PhoneDriver { get; set; }
    [JsonPropertyName("time_record")] public long? TimeRecord { get; set; }
}
