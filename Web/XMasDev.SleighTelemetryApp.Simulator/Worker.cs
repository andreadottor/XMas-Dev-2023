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

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _deviceClient = DeviceClient.CreateFromConnectionString(configuration.GetConnectionString("IoTHubDeviceConnection"));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var data = new SleighTelemetryData
                {
                    Date = DateTime.Now,
                    Latitude = 1,
                    Longitude = 2,
                    GyroX = 3,
                    GyroY = 4,
                    GyroZ = 5,
                    GiftsDelivered = 6
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
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
