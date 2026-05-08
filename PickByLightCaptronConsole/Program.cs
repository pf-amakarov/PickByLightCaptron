using CaptronCommunicationModels;
using MQTTnet;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;
using PickByLightCaptronConsole.models;
using System.Buffers;
using System.Net;
using System.Text;
using System.Text.Json;

internal class Program
{
    public static MqttServer mqttServer;
    public static MqttClientDisconnectOptions mqttClientDisconnectOptions;
    public static IMqttClient mqttClient;

    //private const string Product = "SEH201";
    //private const string DeviceId = "EU-WigglyCaringPie";
    private const string Product = "SEH101";

    private const string DeviceId = "EU-RudeLyingSlide";

    public static async Task StartMqttServerAsync()
    {
        var logger = new MqttNetEventLogger();

        logger.LogMessagePublished += (s, e) =>
        {
            var logMessage = e.LogMessage;

            var color = logMessage.Level switch
            {
                MqttNetLogLevel.Error => ConsoleColor.Red,
                MqttNetLogLevel.Warning => ConsoleColor.Yellow,
                MqttNetLogLevel.Info => ConsoleColor.White,
                _ => ConsoleColor.Gray
            };

            Console.ForegroundColor = color;
            Console.WriteLine($"[{logMessage.Timestamp:HH:mm:ss}] [{logMessage.Level}] [{logMessage.Source}]: {logMessage.Message}");

            if (logMessage.Exception != null)
            {
                Console.WriteLine(logMessage.Exception);
            }

            Console.ResetColor();
        };

        var mqttServerFactory = new MqttServerFactory(logger);

        var mqttServerOptions = mqttServerFactory.CreateServerOptionsBuilder()
        .WithDefaultEndpoint()
        .WithDefaultEndpointBoundIPAddress(IPAddress.Any)
        .WithDefaultEndpointPort(1883)
        .Build();

        mqttServer = mqttServerFactory.CreateMqttServer(mqttServerOptions);

        mqttServer.ClientConnectedAsync += e =>
        {
            Console.WriteLine($"Client verbunden: {e.ClientId}");
            return Task.CompletedTask;
        };

        await mqttServer.StartAsync();

        Console.WriteLine("MQTT Server gestartet auf localhost:1883.");
    }

    public static async Task StopMqttServerAsync()
    {
        if (mqttServer is not null)
        {
            await mqttServer.StopAsync();
            mqttServer.Dispose();
        }
    }

    public static async Task ConnectToLocalServerAsync()
    {
        var logger = new MqttNetEventLogger();

        logger.LogMessagePublished += (s, e) =>
        {
            var logMessage = e.LogMessage;

            var color = logMessage.Level switch
            {
                MqttNetLogLevel.Error => ConsoleColor.Red,
                MqttNetLogLevel.Warning => ConsoleColor.Yellow,
                MqttNetLogLevel.Info => ConsoleColor.White,
                _ => ConsoleColor.Gray
            };

            Console.ForegroundColor = color;
            Console.WriteLine($"[{logMessage.Timestamp:HH:mm:ss}] [{logMessage.Level}] [{logMessage.Source}]: {logMessage.Message}");

            if (logMessage.Exception != null)
            {
                Console.WriteLine(logMessage.Exception);
            }

            Console.ResetColor();
        };

        var mqttFactory = new MqttClientFactory(logger);

        mqttClient = mqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .Build();

        var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        Console.WriteLine($"Client verbunden. ResultCode: {response.ResultCode}");

        mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();
    }

    public static async Task DisconnectToLocalServerAsync()
    {
        await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
    }

    public static async Task<DeviceInformation> GetDeviceInformationAsync()
    {
        var requestTopic = $"captron.com/{Product}/nd/{DeviceId}/Get/MAM";
        var responseTopic = $"captron.com/{Product}/nd/{DeviceId}/Pub/MAM";

        var tcs = new TaskCompletionSource<DeviceInformation>();

        await mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(responseTopic)
            .Build());

        mqttClient.ApplicationMessageReceivedAsync += handler;

        await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(requestTopic)
            .Build());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            return await tcs.Task;
        }
        finally
        {
            mqttClient.ApplicationMessageReceivedAsync -= handler;
            await mqttClient.UnsubscribeAsync(responseTopic);
        }

        Task handler(MqttApplicationMessageReceivedEventArgs e)
        {
            if (e.ApplicationMessage.Topic == responseTopic)
            {
                var payload = e.ApplicationMessage.Payload;

                if (!payload.IsEmpty)
                {
                    try
                    {
                        var jsonPayload = Encoding.UTF8.GetString(payload.ToArray());
                        var info = JsonSerializer.Deserialize<DeviceInformation>(jsonPayload);

                        if (info is not null)
                        {
                            tcs.TrySetResult(info);
                        }
                    }
                    catch (JsonException ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }
            }
            return Task.CompletedTask;
        }
    }

    public static async Task ActiveLEDStripe(LedControlMessage ledControlMessage)
    {
        var topic = $"captron.com/{Product}/nd/{DeviceId}/Set/Data/LedStrip";

        var options = new JsonSerializerOptions { WriteIndented = true };
        string payload = JsonSerializer.Serialize(ledControlMessage, options);
        Console.WriteLine(payload);

        await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build());

        Console.WriteLine($"LED-Strip Konfiguration gesendet an {topic}");
    }

    public static async Task SetLEDStripeConfig(LedStripConfig ledStripConfig)
    {
        var topic = $"captron.com/{Product}/nd/{DeviceId}/Set/Config/LedStrip";

        var options = new JsonSerializerOptions { WriteIndented = true };
        string payload = JsonSerializer.Serialize(ledStripConfig, options);
        Console.WriteLine(payload);

        await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build());

        Console.WriteLine($"LedStripConfig Konfiguration gesendet an {topic}");
    }

    private static async Task Main(string[] args)
    {
        await StartMqttServerAsync();

        Console.WriteLine("Warte bis LED am HUB grün leuchtet.");
        Console.ReadLine();

        await ConnectToLocalServerAsync();
        DeviceInformation deviceInformation = await GetDeviceInformationAsync();

        var separator = new string('─', 90);

        Console.WriteLine();
        Console.WriteLine("┌" + separator + "┐");
        Console.WriteLine($"│{"  Device Information",-90}│");
        Console.WriteLine("├" + separator + "┤");
        Console.WriteLine($"│  {"Content",-40} {deviceInformation.Content,-47}│");
        Console.WriteLine($"│  {"Board Name",-40} {deviceInformation.BoardName,-47}│");
        Console.WriteLine($"│  {"Manufacturer",-40} {deviceInformation.Manufacturer,-47}│");
        Console.WriteLine($"│  {"Model",-40} {deviceInformation.Model,-47}│");
        Console.WriteLine($"│  {"Product Code",-40} {deviceInformation.ProductCode,-47}│");
        Console.WriteLine($"│  {"Version",-40} {deviceInformation.Version,-47}│");
        Console.WriteLine($"│  {"Uptime",-40} {deviceInformation.Uptime,-47}│");
        Console.WriteLine($"│  {"Connection",-40} {deviceInformation.Conn,-47}│");
        Console.WriteLine($"│  {"Segments Activated",-40} {deviceInformation.SegmentsActivated,-47}│");
        Console.WriteLine($"│  {"Light Up",-40} {deviceInformation.LightUp,-47}│");
        Console.WriteLine("└" + separator + "┘");
        Console.WriteLine();

        var ledControlMessage = new LedControlMessage
        {
            Content = "/Set/Data/LedStrip",
            LED_STRIP_1 = new LedStrip
            {
                Active = true,
                Segments = new List<Segment>
                {
                    new Segment
                    {
                        StartLED = 0,
                        StopLED = 30,
                        Speed = 190,
                        Effect = 200,
                        Colors = new List<ColorRgb>
                        {
                            new ColorRgb { R = 0, G = 0, B = 255 },
                        }
                    }
                }
            }
        };
        await ActiveLEDStripe(ledControlMessage);

        await DisconnectToLocalServerAsync();

        Console.ReadLine();

        await StopMqttServerAsync();
    }
}