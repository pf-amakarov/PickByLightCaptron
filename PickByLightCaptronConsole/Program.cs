using CaptronCommunicationModels;
using MQTTnet;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;
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

    public static async Task SetLedStripAsync(ActivateLEDStrip ledStrip)
    {
        var topic = $"captron.com/{Product}/nd/{DeviceId}/Set/Data/LedStrip";

        //var responseTopic = $"captron.com/{Product}/nd/{DeviceId}/Pub/MAM";
        //var requestTopic = $"captron.com/{Product}/nd/{DeviceId}/Get/MAM";

        var payload = JsonSerializer.Serialize(ledStrip);

        //var payload = "{\r\n  \"Content\": \"{Content definition}\",\r\n  \"LED_STRIP_1\": {\r\n    \"Active\": true,\r\n    \"Segments\": [\r\n      {\r\n        \"StartLED\": 0,\r\n        \"StopLED\": 30,\r\n        \"Speed\": 190,\r\n        \"Effect\": 1,\r\n        \"Colors\": [\r\n          {\r\n            \"R\": 0,\r\n            \"G\": 150,\r\n            \"B\": 0\r\n          },\r\n          {\r\n            \"R\": 0,\r\n            \"G\": 150,\r\n            \"B\": 0\r\n          }\r\n        ]\r\n      }\r\n    ]\r\n  }\r\n}";

        await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build());

        Console.WriteLine($"LED-Strip Konfiguration gesendet an {topic}");
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

        Console.WriteLine($"Content			    :	{deviceInformation.Content}");
        Console.WriteLine($"BoardName		    :	{deviceInformation.BoardName}");
        Console.WriteLine($"Manufacturer	    :	{deviceInformation.Manufacturer}");
        Console.WriteLine($"Model			    :	{deviceInformation.Model}");
        Console.WriteLine($"ProductCode		    :	{deviceInformation.ProductCode}");
        Console.WriteLine($"Version			    :	{deviceInformation.Version}");
        Console.WriteLine($"Uptime			    :	{deviceInformation.Uptime}");
        Console.WriteLine($"Conn			    :	{deviceInformation.Conn}");
        Console.WriteLine($"SegmentsActivated   :	{deviceInformation.SegmentsActivated}");
        Console.WriteLine($"LightUp			    :	{deviceInformation.LightUp}");

        ActivateLEDStrip activateLEDStrip = new ActivateLEDStrip
        {
            Content = "LED_STRIP_1",
            LedStrips = new Dictionary<string, object>
            {
                { "SegmentIndex", 0 },
                { "Red", 255 },
                { "Green", 0 },
                { "Blue", 0 },
                { "Brightness", 128 }
            }
        };

        await SetLedStripAsync(activateLEDStrip);

        await DisconnectToLocalServerAsync();
        await StopMqttServerAsync();
    }
}