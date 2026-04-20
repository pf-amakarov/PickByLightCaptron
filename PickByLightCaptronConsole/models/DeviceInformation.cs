namespace CaptronCommunicationModels
{
    /// <summary>
    /// /SEH100/nd/{device-id}/Pub/MAM
    /// /SEH100/nd/{device-id}/Get/MAM
    /// {
    ///   "Content": "{Content definition}",
    ///   "BoardName": "lucky_python",
    ///   "Manufacturer": "CAPTRON",
    ///   "Model": "PYTHON-Head",
    ///   "ProductCode": "123456789",
    ///   "SoftwareVersion": "v0.0.1"
    /// }
    /// </summary>
    public class DeviceInformation
    {
        public string Content { get; set; }
        public string BoardName { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string ProductCode { get; set; }
        public string SoftwareVersion { get; set; }
    }
}