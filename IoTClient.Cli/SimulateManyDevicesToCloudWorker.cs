using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IoTClient.Cli
{
    public partial class SimulateManyDevicesToCloudWorker : IHostedService
    {
        private readonly ConnectionStrings _connectionStrings;
        private readonly List<Task> _backgroundWorkers = new List<Task>();
        private bool _sendData;

        public SimulateManyDevicesToCloudWorker(
            IConfiguration configuration)
        {
            _connectionStrings = new ConnectionStrings();
            configuration.GetSection("ConnectionStrings").Bind(_connectionStrings);
            if (_connectionStrings.DeviceConnections.Length == 0)
                throw new ArgumentException("Missing required device connectionstrings!");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Beginning device simulation..");
            _sendData = true;
            for (int i = 0; i < _connectionStrings.DeviceConnections.Length; i++)
            {
                Console.WriteLine($"Preparing client {i + 1}/{_connectionStrings.DeviceConnections.Length}");
                var instance = new DeviceInstance(_connectionStrings.DeviceConnections[i]);
                _backgroundWorkers.Add(SimulateAsync(instance));
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _sendData = false;
            Console.WriteLine("Stopping device simulation..");
            try
            {
                await Task.WhenAll(_backgroundWorkers);
            }
            catch
            {
                Console.WriteLine("Shutdown aborted (likely took too long)");
            }
        }

        private async Task SimulateAsync(DeviceInstance instance)
        {
            try
            {
                var deviceClient = instance.DeviceClient;

                // listen for cloud to device methods and desired property updates
                await deviceClient.SetMethodDefaultHandlerAsync(OnUnknownMethodAsync, instance);
                await deviceClient.SetMethodHandlerAsync("UpdateFirmwareNow", OnUpdateAvailableAsync, instance);
                await deviceClient.SetMethodHandlerAsync("UploadLog", OnUploadLogAsync, instance);
                await deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdatedAsync, instance);

                const int seconds = 10;
                instance.Log($"Client set up to send temperature data every {seconds} seconds");

                var twin = await deviceClient.GetTwinAsync();
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
                        var overheatThreshold = instance.OverheatThreshold;
                        var overheated = overheatThreshold.HasValue && data.temperature > overheatThreshold;
                        message.Properties.Add("overheated", overheated.ToString());
                        await deviceClient.SendEventAsync(message);
                        instance.Log($"Sent: temperature={data.temperature}" + (overheated ? ";overheated=true" : ""));
                    }
                    catch (Exception e)
                    {
                        instance.Log($"Failure: {e.Message}");
                    }
                    await Task.Delay(seconds * 1000);
                }
            }
            catch (Exception e)
            {
                instance.Log($"Fatal startup error: {e.Message}");
            }
            finally
            {
                instance.Log("Shutting down client..");
                await instance.DeviceClient.CloseAsync();
            }
        }

        private Task<MethodResponse> OnUnknownMethodAsync(MethodRequest methodRequest, object userContext)
        {
            var di = (DeviceInstance)userContext;
            di.Log($"Unknown cloud to device call. Method '{methodRequest.Name}' is not defined on client. Don't know what to do with payload {methodRequest.DataAsJson}");
            return Task.FromResult(new MethodResponse(404));
        }

        private async Task<MethodResponse> OnUploadLogAsync(MethodRequest methodRequest, object userContext)
        {
            var di = (DeviceInstance)userContext;
            if (int.TryParse(di.Name.Substring(di.Name.Length - 2), out int id) &&
                id % 2 == 0)
            {
                di.Log("Uploading log to cloud..");
                var logBytes = Encoding.UTF8.GetBytes($"This is fake data for device {di.Name}. In the realworld this could have been logs from last 24h of runtime.");
                using var logData = new MemoryStream(logBytes);
                logData.Position = 0;
                await di.DeviceClient.UploadToBlobAsync($"devicelog.{Guid.NewGuid()}.log", logData);

                di.Log("Upload done!");
                return new MethodResponse(200);
            }

            di.Log("No log available to be uploaded!");
            return new MethodResponse((int)HttpStatusCode.NoContent);
        }

        private async Task<MethodResponse> OnUpdateAvailableAsync(MethodRequest methodRequest, object userContext)
        {
            var di = (DeviceInstance)userContext;
            var data = methodRequest.DataAsJson;
            di.Log($"Updating firware. Payload was: {data}");
            await Task.Delay(3_000);
            di.Log("Firware update successful!");
            return new MethodResponse(200);
        }

        private async Task DesiredPropertyUpdatedAsync(TwinCollection desiredProperties, object userContext)
        {
            var di = (DeviceInstance)userContext;
            if (desiredProperties.Contains("overheatThreshold"))
            {
                var value = desiredProperties["overheatThreshold"];
                int threshold = -1;
                if (value != null &&
                    !int.TryParse(value?.ToString(), out threshold))
                {
                    return;
                }
                var desiredThreshold = value != null ? threshold : (int?)null;
                // devices end in double digit name, abuse to fake limit of certain devices
                if (int.TryParse(di.Name.Substring(di.Name.Length - 2), out int id) &&
                    id > 15)
                {
                    // pretend a threshold limit exists for certain devices
                    const int limit = 50;
                    if (desiredThreshold.HasValue && desiredThreshold > limit)
                    {
                        di.Log($"Error: Maximum threshold of {limit} exceeded");
                        return;
                    }
                }

                di.Log($"Updating threshold to {desiredThreshold}");
                di.OverheatThreshold = desiredThreshold;
                await di.DeviceClient.UpdateReportedPropertiesAsync(new TwinCollection(JsonSerializer.Serialize(new
                {
                    overheatThreshold = desiredThreshold
                })));
            }
        }
    }
}
