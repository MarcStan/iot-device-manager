namespace BulkDeviceCreator
{
    public class DeviceInfo
    {
        public string Id { get; set; } = "";

        public bool Enabed { get; set; }

        public string Country { get; set; } = "";

        public string Building { get; set; } = "";

        public string Floor { get; set; } = "";

        public string SensorType { get; set; } = "";

        public string[] Tags { get; set; } = new string[0];
    }
}
