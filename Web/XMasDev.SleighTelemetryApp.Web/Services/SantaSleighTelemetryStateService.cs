namespace XMasDev.SleighTelemetryApp.Web.Services;

using Azure.Messaging.ServiceBus;
using System.Diagnostics;
using System.Text.Json;
using XMasDev.SleighTelemetryApp.Shared.Dtos;
using XMasDev.SleighTelemetryApp.Shared.Models;

public class SantaSleighTelemetryStateService : IAsyncDisposable
{
    public event EventHandler? StateChanged;

    private readonly ServiceBusClient _client;
    private readonly ServiceBusProcessor _processor;


    public DateTime Date { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double GyroX { get; private set; }
    public double GyroY { get; private set; }
    public double GyroZ { get; private set; }
    public int GiftsDelivered { get; private set; }


    public SantaSleighTelemetryStateService(IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("ServiceBusConnection");
        _client = new ServiceBusClient(cs);
        _processor = _client.CreateProcessor("SantaSleighTelemetry", "web", new ServiceBusProcessorOptions());
        // add handler to process messages
        _processor.ProcessMessageAsync += MessageHandler;

        // add handler to process any errors
        _processor.ProcessErrorAsync += ErrorHandler;
    }


    public async Task StartMonitoringAsync()
    {
        // start processing 
        await _processor.StartProcessingAsync();
    }

    // handle received messages
    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string body = args.Message.Body.ToString();
        var telemetry = JsonSerializer.Deserialize<SleighTelemetryData>(body);

        // complete the message. messages is deleted from the subscription. 
        await args.CompleteMessageAsync(args.Message);

        UpdateState(telemetry);
    }

    // handle any errors when receiving messages
    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Debug.WriteLine(args.Exception.ToString());
        return Task.CompletedTask;
    }


    private void UpdateState(SleighTelemetryData telemetryData)
    {
        Date = telemetryData.Date;
        Latitude = telemetryData.Latitude;
        Longitude = telemetryData.Longitude;
        GyroX = telemetryData.GyroX;
        GyroY = telemetryData.GyroY;
        GyroZ = telemetryData.GyroZ;
        GiftsDelivered = telemetryData.GiftsDelivered;

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateState(PersistedTelemetryData telemetryData)
    {
        Date = telemetryData.DateAndTime;
        Latitude = telemetryData.Latitude;
        Longitude = telemetryData.Longitude;
        GyroX = telemetryData.GyroX;
        GyroY = telemetryData.GyroY;
        GyroZ = telemetryData.GyroZ;
        GiftsDelivered = telemetryData.GiftsDelivered;

        StateChanged?.Invoke(this, EventArgs.Empty);
    }


    public async ValueTask DisposeAsync()
    {
        await _processor.DisposeAsync();
        await _client.DisposeAsync();
    }
}
