using System;
using System.Collections.Generic;
using System.Text.Json;

public class Program
{
    public static void Main()
    {
        // Objekt erstellen und exakt mit den Bild-Daten füllen
        var ledMessage = new LedControlMessage
        {
            // "{Content definition}" wird hier meist durch "set" ersetzt,
            // um die LEDs tatsächlich zu steuern.
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

        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonOutput = JsonSerializer.Serialize(ledMessage, options);

        Console.WriteLine(jsonOutput);
    }
}

// --- Klassenstruktur ---

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