using MQTTnet;
using MQTTnet.Server;
using System.Net;

Console.WriteLine("=== MQTT Server Startsequenz ===");

var mqttFactory = new MqttServerFactory();

var mqttServerOptions = mqttFactory.CreateServerOptionsBuilder()
    .WithDefaultEndpoint()
    .WithDefaultEndpointBoundIPAddress(IPAddress.Any)
    .WithDefaultEndpointPort(1883)
    .Build();

// WICHTIG: Das 'using' sorgt dafür, dass der Server sauber gestoppt wird
using var mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);

// Handler für anonyme Verbindungen
mqttServer.ValidatingConnectionAsync += e =>
{
    e.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.Success;
    return Task.CompletedTask;
};

// Startet den Server in einem eigenen Task, damit der Hauptthread frei bleibt
try
{
    // Wir warten hier auf den Start, nicht auf das Ende des Servers
    await mqttServer.StartAsync();
    Console.WriteLine("Server ist ONLINE (Port 1883).");
}
catch (Exception ex)
{
    Console.WriteLine($"Fehler beim Start: {ex.Message}");
}

Console.WriteLine("Programm läuft. Setze hier deinen Breakpoint.");
// Dieser Befehl verhindert, dass die Konsolen-App sich sofort schließt:
await Task.Delay(-1);