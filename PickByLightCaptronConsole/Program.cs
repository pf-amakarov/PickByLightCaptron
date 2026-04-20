using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;

internal class Program
{
    private static async Task Main(string[] args)
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

        // 3. Server-Instanz erstellen
        using (var mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions))
        {
            // Event: Wenn ein Client sich verbindet
            mqttServer.ClientConnectedAsync += e =>
            {
                Console.WriteLine($"Client verbunden: {e.ClientId}");
                return Task.CompletedTask;
            };

            // Server starten
            await mqttServer.StartAsync();

            Console.WriteLine("MQTT Server gestartet. Drücke Enter zum Beenden...");
            Console.ReadLine();

            // Server stoppen
            await mqttServer.StopAsync();
        }
    }
}