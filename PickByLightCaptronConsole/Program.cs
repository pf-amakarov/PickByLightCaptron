using CaptronCommunicationModels;
using MQTTnet;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;
using System.Drawing;
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
///
///
///
/// </summary>

internal class Program
{
    public static MqttServer mqttServer;
    public static MqttClientDisconnectOptions mqttClientDisconnectOptions;
    public static IMqttClient mqttClient;

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
        var mqttFactory = new MqttServerFactory(logger);

        // 2. Optionen konfigurieren (Port, Endpunkt, etc.)
        var mqttServerOptions = mqttFactory.CreateServerOptionsBuilder()
            .WithDefaultEndpoint() // Nutzt standardmäßig Port 1883
            .Build();

        // 3. Server-Instanz erstellen und als statisches Feld speichern
        mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);

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
        var mqttFactory = new MqttClientFactory();

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

    public static async Task<DeviceInformation> GetDeviceInformationAsync(string deviceId)
    {
        var responseTopic = $"/SEH100/nd/{deviceId}/Pub/MAM";
        var requestTopic = $"/SEH100/nd/{deviceId}/Get/MAM";

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
                var payload = e.ApplicationMessage.ConvertPayloadToString();
                var info = JsonSerializer.Deserialize<DeviceInformation>(payload);
                tcs.TrySetResult(info);
            }
            return Task.CompletedTask;
        }
    }

    public static async Task SetLedStripAsync(string deviceId, ActivateLEDStrip ledStrip)
    {
        var topic = $"/SEH100/nd/{deviceId}/Set/Data/LedStrip";

        var payload = JsonSerializer.Serialize(ledStrip);

        await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build());

        Console.WriteLine($"LED-Strip Konfiguration gesendet an {topic}");
    }

    private static async Task Main(string[] args)
    {
        await StartMqttServerAsync();
        await ConnectToLocalServerAsync();

        await GetDeviceInformationAsync("123456789");

        //var ledStrip = new ActivateLEDStrip
        //{
        //    Content = "LedStrip",
        //    LedStrips =
        //    {
        //        ["LED_STRIP_1"] = new LED_STRIP
        //        {
        //            Active = true,
        //            Segments =
        //            [
        //                new Segment
        //                {
        //                    StartLED = 0,
        //                    StopLED = 30,
        //                    Speed = 190,
        //                    Effect = 1,
        //                    Colors = [ new System.Drawing.Color
        //                    {
        //                        R = 0,
        //                        G = 150,
        //                        B = 0
        //                    } ]
        //                }
        //            ]
        //        }
        //    }
        //};
        //await SetLedStripAsync("123456789", ledStrip);

        Console.ReadLine();

        await DisconnectToLocalServerAsync();
        await StopMqttServerAsync();
    }
}