namespace XMasDev.SleighTelemetryApp.Simulator
{
    using Microsoft.Azure.Devices.Client;
    using System.Text;
    using System.Text.Json;
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

                var json = JsonSerializer.Serialize(data);
                using var message = new Message(Encoding.ASCII.GetBytes(json))
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8",
                };
                await _deviceClient.SendEventAsync(message, stoppingToken);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Message sent: {data}", data);
                }
                await Task.Delay(5000, stoppingToken);

                latitude += (random.NextDouble() * 0.01);
                longitude += (random.NextDouble() * 0.01);
                gifts += random.Next(1, 10);
            }
        }
    }
}
