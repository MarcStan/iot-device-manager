using Microsoft.Azure.Devices.Client;
using System;

namespace IoTClient.Cli
{
    public class DeviceInstance
    {
        public DeviceInstance(string connectionString)
        {
            DeviceClient = DeviceClient.CreateFromConnectionString(connectionString);
            DeviceClient.OperationTimeoutInMilliseconds = 2000;
            Name = connectionString.Split(";")[1].Split('=')[1];
        }

        public string Name { get; }

        public int? OverheatThreshold { get; set; }

        public DeviceClient DeviceClient { get; }

        public void Log(string message)
            => Console.WriteLine($"{Name}: {message}");
    }
}
