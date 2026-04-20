using MQTTnet;
using MQTTnet.Server;
using MQTTnet.Diagnostics.Logger;
using CaptronCommunicationModels;

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

    private static async Task Main(string[] args)
    {
        await StartMqttServerAsync();
        await ConnectToLocalServerAsync();

        Console.WriteLine("Drücke Enter zum Beenden...");
        Console.ReadLine();

        await DisconnectToLocalServerAsync();
        await StopMqttServerAsync();
    }
}