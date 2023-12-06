namespace XMasDev.SleighTelemetryApp.Shared.Models;

using System.Text.Json.Serialization;

public class PersistedTelemetryData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    public DateOnly Date { get; set; }
    public DateTime DateAndTime { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double GyroX { get; set; }
    public double GyroY { get; set; }
    public double GyroZ { get; set; }
    public int GiftsDelivered { get; set; }
}