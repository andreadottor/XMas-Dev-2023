namespace XMasDev.SleighTelemetryApp.Functions;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using XMasDev.SleighTelemetryApp.Shared.Dtos;
using XMasDev.SleighTelemetryApp.Shared.Models;

public class PersistDataToCosmosDB
{
    private readonly ILogger<PersistDataToCosmosDB> _logger;

    public PersistDataToCosmosDB(ILogger<PersistDataToCosmosDB> logger)
    {
        _logger = logger;
    }

    [Function(nameof(PersistDataToCosmosDB))]
    [CosmosDBOutput("SantaSleighTelemetry", "Items", Connection = "CosmosDbConnectionString", CreateIfNotExists = true, PartitionKey = "/Date")]

    public PersistedTelemetryData? Run([ServiceBusTrigger("SantaSleighTelemetry", "ToCosmosDB", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        var telemetry = System.Text.Json.JsonSerializer.Deserialize<SleighTelemetryData>(message.Body.ToString());
        if(telemetry is null)
            return null;

        return new()
        {
            Id = Guid.NewGuid().ToString(),
            Date = DateOnly.FromDateTime(telemetry.Date),
            DateAndTime = telemetry.Date,
            Latitude = telemetry.Latitude,
            Longitude = telemetry.Longitude,
            GyroX = telemetry.GyroX,
            GyroY = telemetry.GyroY,
            GyroZ = telemetry.GyroZ,
            GiftsDelivered = telemetry.GiftsDelivered
        };
    }
}
