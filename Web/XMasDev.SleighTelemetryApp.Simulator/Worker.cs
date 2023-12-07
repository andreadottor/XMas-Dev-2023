namespace XMasDev.SleighTelemetryApp.Simulator
{
    using Microsoft.Azure.Devices.Client;
    using System;
    using System.Globalization;
    using System.Text;
    using System.Text.Json;
    using System.Xml.Linq;
    using XMasDev.SleighTelemetryApp.Shared.Dtos;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly DeviceClient _deviceClient;

        private const double RomeLatitude = 41.902782;
        private const double RomeLongitude = 12.496366;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _deviceClient = DeviceClient.CreateFromConnectionString(configuration.GetConnectionString("IoTHubDeviceConnection"));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (File.Exists("Route.gpx"))
                await SimulateGpxRouteAsync(stoppingToken);
            else
                await SimulateRandomRouteAsync(stoppingToken);
        }

        private async Task SimulateGpxRouteAsync(CancellationToken stoppingToken)
        {
            var gpx = await File.ReadAllTextAsync("Route.gpx", stoppingToken);
            var gpxDoc = XDocument.Parse(gpx);
            var gpxNs = gpxDoc.Root.GetDefaultNamespace();
            var points = gpxDoc.Descendants(gpxNs + "trkpt").ToList();

            var random = new Random();
            var gifts = random.Next(1000, 10000);

            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var point in points)
                {
                    var lat = double.Parse(point.Attribute("lat").Value, CultureInfo.InvariantCulture);
                    var lon = double.Parse(point.Attribute("lon").Value, CultureInfo.InvariantCulture);

                    var data = new SleighTelemetryData
                    {
                        Date = DateTime.UtcNow,
                        Latitude = lat,
                        Longitude = lon,
                        GyroX = random.Next(-45, 45),
                        GyroY = random.Next(-30, 30),
                        GyroZ = random.Next(-90, 90),
                        GiftsDelivered = gifts
                    };
                    await SendMessageAsync(data, stoppingToken);
                    await Task.Delay(2000, stoppingToken);

                    gifts += random.Next(1, 10);

                    if (stoppingToken.IsCancellationRequested)
                        break;
                }
            }
        }

        private async Task SimulateRandomRouteAsync(CancellationToken stoppingToken)
        {
            var latitude = RomeLatitude;
            var longitude = RomeLongitude;
            var random = new Random();
            var gifts = random.Next(1000, 10000);

            while (!stoppingToken.IsCancellationRequested)
            {
                var data = new SleighTelemetryData
                {
                    Date = DateTime.UtcNow,
                    Latitude = latitude,
                    Longitude = longitude,
                    GyroX = random.Next(-45, 45),
                    GyroY = random.Next(-30, 30),
                    GyroZ = random.Next(-90, 90),
                    GiftsDelivered = gifts
                };
                await SendMessageAsync(data, stoppingToken);
                await Task.Delay(2000, stoppingToken);

                latitude += (random.Next(-100, 100) * 0.00001);
                longitude += (random.Next(-100, 100) * 0.00001);
                gifts += random.Next(1, 10);
            }
        }

        private async Task SendMessageAsync(SleighTelemetryData data, CancellationToken stoppingToken)
        {
            var json = JsonSerializer.Serialize(data);
            var message = new Message(Encoding.ASCII.GetBytes(json))
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8",
            };
            await _deviceClient.SendEventAsync(message, stoppingToken);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Message sent: {data}", data);
            }
        }
    }
}
