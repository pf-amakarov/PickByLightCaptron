using System.Text.Json;
using System.Text.Json.Serialization;

public class Program
{
    public static void Main()
    {
        var ledMessage = new LedControlMessage
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
                            new ColorRgb { R = 0, G = 150, B = 0 },
                            new ColorRgb { R = 0, G = 150, B = 0 }
                        }
                    }
                }
            }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonOutput = JsonSerializer.Serialize(ledMessage, options);

        Console.WriteLine(jsonOutput);

        var config = new LedStripConfig
        {
            Content = "{Content definition}",
            Demo = false
        };

        for (int i = 1; i <= 5; i++)
        {
            config.LedStrips.Add($"LED_STRIP_{i}", new StripDetails { Length = "42" });
        }

        string jsonString = JsonSerializer.Serialize(config, options);

        Console.WriteLine("Generiertes JSON für LED Stripe.png:");
        Console.WriteLine(jsonString);
    }
}

public class LedStripConfig
{
    public string Content { get; set; }
    public bool Demo { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> LedStrips { get; set; } = new Dictionary<string, object>();
}

public class StripDetails
{
    public string Length { get; set; }
}

public class LedControlMessage
{
    public string Content { get; set; }
    public LedStrip LED_STRIP_1 { get; set; }
}

public class LedStrip
{
    public bool Active { get; set; }
    public List<Segment> Segments { get; set; }
}

public class Segment
{
    public int StartLED { get; set; }
    public int StopLED { get; set; }
    public int Speed { get; set; }
    public int Effect { get; set; }
    public List<ColorRgb> Colors { get; set; }
}

public class ColorRgb
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
}