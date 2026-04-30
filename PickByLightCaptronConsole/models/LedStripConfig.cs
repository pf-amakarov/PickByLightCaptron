using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PickByLightCaptronConsole.models
{
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
}