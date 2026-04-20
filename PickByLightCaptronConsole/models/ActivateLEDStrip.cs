namespace CaptronCommunicationModels
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// [captron.com/](https://captron.com/){product}/nd/{device-id}/Set/Data/LedStrip
    ///
    ///{
    ///  "Content": "{Content definition}",
    ///  "LED_STRIP_1": {
    ///    "Active": true,
    ///    "Segments": [
    ///      {
    ///        "StartLED": 0,
    ///        "StopLED": 30,
    ///        "Speed": 190,
    ///        "Effect": 1,
    ///        "Colors": [
    ///          {
    ///            "R": 0,
    ///            "G": 150,
    ///            "B": 0
    ///          }
    ///        ]
    ///      }
    ///    ]
    ///  }
    ///}
    ///
    /// </summary>

    public class ActivateLEDStrip
    {
        [JsonPropertyName("Content")]
        public string Content { get; set; }

        /// <summary>
        /// Schlüssel: z.B. "LED_STRIP_1", "LED_STRIP_2"
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> LedStrips { get; set; } = new();
    }

    public class LED_STRIP
    {
        [JsonPropertyName("Active")]
        public bool Active { get; set; }

        [JsonPropertyName("Segments")]
        public Segment[] Segments { get; set; }
    }

    public class Segment
    {
        [JsonPropertyName("StartLED")]
        public int StartLED { get; set; }

        [JsonPropertyName("StopLED")]
        public int StopLED { get; set; }

        [JsonPropertyName("Speed")]
        public int Speed { get; set; }

        [JsonPropertyName("Effect")]
        public int Effect { get; set; }

        [JsonPropertyName("Colors")]
        public Color[] Colors { get; set; }
    }

    public class Color
    {
        [JsonPropertyName("R")]
        public int R { get; set; }

        [JsonPropertyName("G")]
        public int G { get; set; }

        [JsonPropertyName("B")]
        public int B { get; set; }
    }
}