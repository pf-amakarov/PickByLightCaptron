namespace CaptronCommunicationModels
{
    public class DeviceInformation
    {
        public string Content { get; set; }
        public string BoardName { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string ProductCode { get; set; }
        public string Version { get; set; }
        public int Uptime { get; set; }
        public int Conn { get; set; }
        public int SegmentsActivated { get; set; }
        public int LightUp { get; set; }
    }
}