namespace XMasDev.SleighTelemetryApp.Web.Services;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using System.Diagnostics;
using System.Text.Json;
using XMasDev.SleighTelemetryApp.Shared.Dtos;
using XMasDev.SleighTelemetryApp.Shared.Models;

public class SantaSleighTelemetryStateService : IAsyncDisposable
{
    private const double RomeLatitude  = 41.902782;
    private const double RomeLongitude = 12.496366;

    public event EventHandler? StateChanged;

    private readonly ServiceBusClient    _client;
    private readonly ServiceBusProcessor _processor;

    private readonly CosmosClient        _cosmosClient;
    private readonly Container           _cosmosContainer;

    public DateTime Date { get; private set; }
    public double   Latitude { get; private set; }
    public double   Longitude { get; private set; }
    public double   GyroX { get; private set; }
    public double   GyroY { get; private set; }
    public double   GyroZ { get; private set; }
    public int      GiftsDelivered { get; private set; }


    public SantaSleighTelemetryStateService(IConfiguration configuration)
    {
        var csCosmos         = configuration.GetConnectionString("CosmosDbConnection");
        var csServiceBus     = configuration.GetConnectionString("ServiceBusConnection");
        var subscriptionName = configuration.GetValue<string>("ServiceBus:SubscriptionName");
        _cosmosClient        = new CosmosClient(csCosmos);
        _cosmosContainer     = _cosmosClient.GetContainer("SantaSleighTelemetry", "Items");

        // Get the last position for set the initial state
        var lastPosition = _cosmosContainer
                                .GetItemLinqQueryable<PersistedTelemetryData>(allowSynchronousQueryExecution: true)
                                .OrderByDescending(x => x.DateAndTime)
                                .Take(1)
                                .ToList();

        if (lastPosition is not null)
        {
            UpdateState(lastPosition.First());
        }
        else
        {
            Latitude  = RomeLatitude;
            Longitude = RomeLongitude;
        }
        
        _client    = new ServiceBusClient(csServiceBus);
        _processor = _client.CreateProcessor("SantaSleighTelemetry", subscriptionName, new ServiceBusProcessorOptions());
        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync   += ErrorHandler;
    }

    public async Task StartMonitoringAsync()
    {
        // start processing realtime data
        if(!_processor.IsProcessing)
            await _processor.StartProcessingAsync();
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var body      = args.Message.Body.ToString();
        var telemetry = JsonSerializer.Deserialize<SleighTelemetryData>(body);

        // complete the message. messages is deleted from the subscription. 
        await args.CompleteMessageAsync(args.Message);

        UpdateState(telemetry!);
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Debug.WriteLine(args.Exception.ToString());
        return Task.CompletedTask;
    }

    private void UpdateState(SleighTelemetryData telemetryData)
    {
        Date      = telemetryData.Date;
        Latitude  = telemetryData.Latitude;
        Longitude = telemetryData.Longitude;
        GyroX     = telemetryData.GyroX;
        GyroY     = telemetryData.GyroY;
        GyroZ     = telemetryData.GyroZ;
        GiftsDelivered = telemetryData.GiftsDelivered;

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateState(PersistedTelemetryData telemetryData)
    {
        Date      = telemetryData.DateAndTime;
        Latitude  = telemetryData.Latitude;
        Longitude = telemetryData.Longitude;
        GyroX     = telemetryData.GyroX;
        GyroY     = telemetryData.GyroY;
        GyroZ     = telemetryData.GyroZ;
        GiftsDelivered = telemetryData.GiftsDelivered;

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async ValueTask DisposeAsync()
    {
        await _processor.DisposeAsync();
        await _client.DisposeAsync();
    }
}
