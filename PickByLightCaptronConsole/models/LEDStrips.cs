namespace CaptronCommunicationModels
{
    /// <summary>
    /// [captron.com/](https://captron.com/){product}/nd/{device-id}/Set/Config/LedStrip
    ///
    /// {
    ///  "Content": "{Content definition}",
    ///  "Demo": false,
    ///  "LED_STRIP_1": {
    ///    "Length": "42"
    ///  },
    ///  "LED_STRIP_2": {
    ///    "Length": "42"
    ///  },
    ///  "etc..."
    /// }
    /// </summary>

    public class LedStripConfig
    {
        public string Content { get; set; }
        public bool Demo { get; set; }
        public Dictionary<string, object> LedStrips { get; set; }
    }
}