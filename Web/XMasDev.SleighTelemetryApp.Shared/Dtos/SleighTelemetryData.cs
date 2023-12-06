namespace XMasDev.SleighTelemetryApp.Shared.Dtos;

public record SleighTelemetryData
{
    public DateTime Date { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double GyroX { get; init; }
    public double GyroY { get; init; }
    public double GyroZ { get; init; }
    public int GiftsDelivered { get; init; }
}