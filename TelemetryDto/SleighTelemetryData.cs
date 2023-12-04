public record SleighTelemetryData
{
    public DateTime Date { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double GyroX { get; set; }
    public double GyroY { get; set; }
    public double GyroZ { get; set; }
    public int GiftsDelivered { get; set; }
}