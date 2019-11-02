using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IoTClient.Cli
{
    public partial class SimulateManyDevicesToCloudWorker : IHostedService
    {
        private readonly ConnectionStrings _connectionStrings;
        private bool _sendData;

        public SimulateManyDevicesToCloudWorker(
            IConfiguration configuration)
        {
            _connectionStrings = new ConnectionStrings();
            configuration.GetSection("ConnectionStrings").Bind(_connectionStrings);
            if (_connectionStrings.DeviceConnections.Length == 0)
                throw new ArgumentException($"Missing required device connectionstrings!");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Beginning device simulation..");
            _sendData = true;
            foreach (var c in _connectionStrings.DeviceConnections)
                _ = SimulateAsync(c);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _sendData = false;
            Console.WriteLine("Stopping device simulation..");
            return Task.CompletedTask;
        }

        private async Task SimulateAsync(string connectionString)
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
            deviceClient.OperationTimeoutInMilliseconds = 2000;
            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdatedAsync, null);
            var client = connectionString.Split(";")[1].Split('=')[1];

            void WriteLine(string message)
                => Console.WriteLine($"{client}: {message}");

            const int seconds = 10;
            WriteLine($"Client set up to send temperature data every {seconds} seconds");

            var twin = await deviceClient.GetTwinAsync();
            int? overheatThreshold = null;
            if (twin.Properties.Desired.Contains("overheatThreshold"))
            {
                overheatThreshold = twin.Properties.Desired["overheatThreshold"];
            }
            var rng = new Random();
            var sensorBaseTemp = rng.Next(0, 50);
            while (_sendData)
            {
                try
                {
                    var data = new
                    {
                        temperature = sensorBaseTemp + rng.Next(0, 50)
                    };
                    var json = JsonSerializer.Serialize(data);
                    var message = new Message(Encoding.UTF8.GetBytes(json));
                    var overheated = overheatThreshold.HasValue && data.temperature > overheatThreshold;
                    message.Properties.Add("overheated", overheated.ToString());
                    await deviceClient.SendEventAsync(message);
                    WriteLine($"Sent: temperature={data.temperature}" + (overheated ? ";overheated=true" : ""));
                }
                catch (Exception e)
                {
                    WriteLine($"Failure: {e.Message}");
                }
                await Task.Delay(seconds * 1000);
            }
        }

        private Task DesiredPropertyUpdatedAsync(TwinCollection desiredProperties, object userContext)
        {
            throw new NotImplementedException();
        }
    }
}
