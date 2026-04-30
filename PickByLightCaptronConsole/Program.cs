using CaptronCommunicationModels;
using MQTTnet;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;
using PickByLightCaptronConsole.models;
using System.Buffers;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

/// <summary>
/// EU-WigglyCaringPie f8:b3:b7:c3:52:0f
///
/// Zur Offline-Konfiguration kann die integrierte Setup-Seite genutzt werden. Sie können darauf zugreifen,
/// indem Sie in einem beliebigen Browser http://169.254.8.238 gefolgt von der IP-Adresse des Geräts eingeben. Standardmäßig versucht das Gerät,
/// seine IP-Adresse über einen DHCP-Server zu beziehen. Auf diesem Server/Router können Sie die
/// zugewiesene Adresse abrufen. Die Web-Setup-Seite ist ab Firmware-Version V0.227-29 verfügbar
///
/// http://169.254.8.238
/// http://192.168.240.226
/// http://169.254.101.201
/// 169.254.101.2
/// http://169.254.101.2
///
///
/// </summary>

/// <summary>
///
/// Beispiel Payload für die Device Information:
///
/// {
///   "Content": "{Content definition}",
///   "BoardName": "lucky_python",
///   "Manufacturer": "CAPTRON",
///   "Model": "PYTHON-Head",
///   "ProductCode": "123456789",
///   "SoftwareVersion": "v0.0.1"
/// }
///
/// </summary>
//public class DeviceInformation
//{
//    public string Content { get; set; }
//    public string BoardName { get; set; }
//    public string Manufacturer { get; set; }
//    public string Model { get; set; }
//    public string ProductCode { get; set; }
//    public string SoftwareVersion { get; set; }
//}

internal class Program
{
    public static MqttServer mqttServer;
    public static MqttClientDisconnectOptions mqttClientDisconnectOptions;
    public static IMqttClient mqttClient;
    private const string Product = "SEH201";
    private const string DeviceId = "EU-WigglyCaringPie";

    public static async Task StartMqttServerAsync()
    {
        // 1. Erstelle einen Logger
        var logger = new MqttNetEventLogger();

        // 2. Abonniere das LogMessagePublished Event
        logger.LogMessagePublished += (s, e) =>
        {
            var logMessage = e.LogMessage;

            // Formatierung der Ausgabe
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

        // 1. Instanz der Factory erstellen
        var mqttServerFactory = new MqttServerFactory(logger);

        var mqttServerOptions = mqttServerFactory.CreateServerOptionsBuilder()
        .WithDefaultEndpoint()
        .WithDefaultEndpointBoundIPAddress(IPAddress.Any)
        .WithDefaultEndpointPort(1883)
        .Build();

        // 3. Server-Instanz erstellen und als statisches Feld speichern
        mqttServer = mqttServerFactory.CreateMqttServer(mqttServerOptions);

        // Event: Wenn ein Client sich verbindet
        mqttServer.ClientConnectedAsync += e =>
        {
            Console.WriteLine($"Client verbunden: {e.ClientId}");
            return Task.CompletedTask;
        };

        // Server starten
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
        // 1. Logger erstellen
        var logger = new MqttNetEventLogger();

        // 2. LogMessagePublished abonnieren
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

        // 3. Client mit Logger erstellen
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

        // Antwort abonnieren
        await mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(responseTopic)
            .Build());

        mqttClient.ApplicationMessageReceivedAsync += handler;

        // Anfrage senden
        await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(requestTopic)
            .Build());

        // Auf Antwort warten (max. 5 Sekunden)
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

        await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build());

        Console.WriteLine($"LED-Strip Konfiguration gesendet an {topic}");
        Console.WriteLine(payload);
    }

    public static async Task SetLEDStripeConfig(LedStripConfig ledStripConfig)
    {
        var topic = $"captron.com/{Product}/nd/{DeviceId}/Set/Data/LedStrip";

        var options = new JsonSerializerOptions { WriteIndented = true };
        string payload = JsonSerializer.Serialize(ledStripConfig, options);

        await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build());

        Console.WriteLine($"LedStripConfig Konfiguration gesendet an {topic}");
        Console.WriteLine(payload);
    }

    //public static async Task<ActivateLEDStrip> GetLedStripAsync()

    //{
    //    var topic = $"/SEH100/nd/{DeviceId}/Set/Data/LedStrip";
    //}

    //public static async Task<List<string>> GetAllUniqueIDs()
    //{
    //    var list = new List<string>();
    //}

    // 1. Datenmodell für die Device Information (Message Payload Data)

    //public static async Task<DeviceInformation> GetDeviceInformation()
    //{
    //    string getTopic = $"captron.com/{Product}/nd/{DeviceId}/Get/MAM";
    //    string pubTopic = $"captron.com/{Product}/nd/{DeviceId}/Pub/MAM";
    //}

    private static async Task Main(string[] args)
    {
        await StartMqttServerAsync();

        // Warten bis sich das Captron-Gerät verbunden hat (max. 60 Sekunden)
        Console.WriteLine("Warte auf Verbindung des Captron-Geräts...");
        var deviceConnected = new TaskCompletionSource<bool>();

        mqttServer.ClientConnectedAsync += e =>
        {
            Console.WriteLine($"Captron-Gerät verbunden: {e.ClientId}");
            deviceConnected.TrySetResult(true);
            return Task.CompletedTask;
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        cts.Token.Register(() => deviceConnected.TrySetCanceled());

        try
        {
            await deviceConnected.Task;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Timeout: Kein Captron-Gerät hat sich innerhalb von 60 Sekunden verbunden.");
            return;
        }

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

        var ledStripConfig = new LedStripConfig
        {
            Content = "/Set/Config/LedStrip",
            Demo = false
        };

        for (int i = 1; i <= 5; i++)
        {
            ledStripConfig.LedStrips.Add($"LED_STRIP_{i}", new StripDetails { Length = "42" });
        }

        await SetLEDStripeConfig(ledStripConfig);

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
                        Effect = 1,
                        Colors = new List<ColorRgb>
                        {
                            new ColorRgb { R = 0, G = 150, B = 0 }, // Erstes Grün
                            new ColorRgb { R = 0, G = 150, B = 0 }  // Zweites Grün
                        }
                    }
                }
            }
        };
        await ActiveLEDStripe(ledControlMessage);

        await DisconnectToLocalServerAsync();
        await StopMqttServerAsync();
    }
}