using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IoTClient.Cli
{
    public class SimulatedDeviceToCloudWorker : IHostedService
    {
        private readonly DeviceClient _deviceClient;
        private bool _sendData;

        public SimulatedDeviceToCloudWorker(
            IConfiguration configuration)
        {
            const string key = "ConnectionStrings:DeviceConnection";
            var connectionString = configuration[key];
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException($"Missing required connectionstring @{key}");

            _deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Beginning device simulation..");
            _sendData = true;
            _ = SimulateAsync();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _sendData = false;
            Console.WriteLine("Stopping device simulation..");
            return Task.CompletedTask;
        }

        private async Task SimulateAsync()
        {
            var rng = new Random();
            const int min = 0;
            while (_sendData)
            {
                try
                {
                    var data = new
                    {
                        temperature = min + rng.Next(0, 100)
                    };
                    var json = JsonSerializer.Serialize(data);
                    var message = new Message(Encoding.UTF8.GetBytes(json));
                    message.Properties.Add("overheated", (data.temperature > 60).ToString());
                    await _deviceClient.SendEventAsync(message);
                    Console.WriteLine($"Sent: temperature={data.temperature}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failure: {e.Message}");
                }
                await Task.Delay(1000);
            }
        }
    }
}
