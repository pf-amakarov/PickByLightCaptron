namespace CaptronCommunicationModels
{
    using System.Collections.Generic;

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
}