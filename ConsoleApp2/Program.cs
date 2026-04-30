using MQTTnet.Server;
using System.Net;

Console.WriteLine("=== MQTT Server Startsequenz ===");

var mqttFactory = new MqttServerFactory();

var mqttServerOptions = mqttFactory.CreateServerOptionsBuilder()
    .WithDefaultEndpoint()
    .WithDefaultEndpointBoundIPAddress(IPAddress.Any)
    .WithDefaultEndpointPort(1883)
    .Build();

using var mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);

mqttServer.ValidatingConnectionAsync += e =>
{
    e.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.Success;
    return Task.CompletedTask;
};

try
{
    await mqttServer.StartAsync();
    Console.WriteLine("Server ist ONLINE (Port 1883).");
}
catch (Exception ex)
{
    Console.WriteLine($"Fehler beim Start: {ex.Message}");
}

await Task.Delay(-1);