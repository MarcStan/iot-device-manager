namespace IoTDeviceManager.Models
{
    public class DeviceModel
    {
        public string DeviceName { get; set; } = "<unknown>";

        public string Id => DeviceName;

        public string ETag { get; set; } = "";
    }
}
