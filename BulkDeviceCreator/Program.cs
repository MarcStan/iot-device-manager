using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BulkDeviceCreator
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                // always use development as this is a sample client
                .UseEnvironment("Development")
                .ConfigureAppConfiguration((context, builder) =>
                {
                    if (context.HostingEnvironment.IsDevelopment())
                        builder.AddUserSecrets<Program>();
                });

            using var host = hostBuilder.Build();

            var configuration = host.Services.GetRequiredService<IConfiguration>();

            const string key = "ConnectionStrings:IoTHub";
            var connectionString = configuration[key];
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException($"Missing required connectionstring @{key}");

            var registryManager = RegistryManager.CreateFromConnectionString(connectionString);

            Console.WriteLine($"Beginning bulk device create..");
            await BulkCreateAsync(registryManager, CancellationToken.None);
            Console.WriteLine("Bulk upload done! Press enter to exit..");
#if !DEBUG
            Console.ReadLine();
#endif
        }

        private static async Task BulkCreateAsync(RegistryManager registryManager, CancellationToken cancellationToken)
        {
            var devices = ReadFromCsv("devices.csv");

            // devices must be unique, so get existing devices
            var existingTwin = devices
                .Select(x => registryManager.GetTwinAsync(x.Id))
                .ToArray();

            await Task.WhenAll(existingTwin);

            var existingDevices = existingTwin
                .Select(task => task.Result)
                .Where(x => x != null)
                .ToArray();

            if (existingDevices.Length > 0)
                Console.WriteLine($"{existingDevices.Length} devices already exist.");

            // only insert devices that don't exist yet
            var devicesToUpload = devices
                .Where(device => existingDevices.All(existing => existing.DeviceId != device.Id))
                .Select(deviceInfo => new Device(deviceInfo.Id)
                {
                    Status = deviceInfo.Enabed ? DeviceStatus.Enabled : DeviceStatus.Disabled
                })
                .ToList();
            if (devicesToUpload.Any())
            {
                Console.WriteLine($"Creating {devicesToUpload.Count} devices..");
                var result = await registryManager.AddDevices2Async(devicesToUpload, cancellationToken);

                if (!result.IsSuccessful)
                    throw new AggregateException("Device creation failed!",
                        result.Errors.Select(e => new Exception(JsonSerializer.Serialize(new
                        {
                            deviceId = e.DeviceId,
                            code = e.ErrorCode,
                            status = e.ErrorStatus,
                            moduleid = e.ModuleId
                        }))));
            }

            // need etag from cloud before updating, so fetch all devices again
            existingTwin = devices
                .Select(deviceInfo => registryManager.GetTwinAsync(deviceInfo.Id))
                .ToArray();

            await Task.WhenAll(existingTwin);

            var devicesInCloud = existingTwin
                .Select(task => task.Result)
                .ToArray();

            var twins = devices
                .Select(d => new Twin(d.Id)
                {
                    Tags = new TwinCollection(JsonSerializer.Serialize(new
                    {
                        country = d.Country,
                        building = d.Building,
                        floor = d.Floor,
                        sensorType = d.SensorType
                    })),
                    ETag = devicesInCloud.Single(cloudDevice => cloudDevice.DeviceId == d.Id).ETag
                })
                .ToList();
            Console.WriteLine($"Updating twin state (and setting tags) on all {twins.Count} devices..");
            await registryManager.UpdateTwins2Async(twins, cancellationToken);
        }

        private static IReadOnlyList<DeviceInfo> ReadFromCsv(string filePath)
        {
            var lines = File.ReadAllLines(filePath);

            var header = lines[0].Split(',');

            string ReadByName(string[] data, string name)
            {
                var idx = header
                    .Select((v, i) => v == name ? i : (int?)null)
                    .Where(x => x.HasValue)
                    .FirstOrDefault();
                if (!idx.HasValue)
                    throw new ArgumentException($"Header {name} not found");

                return data[idx.Value];
            }

            return lines
                .Skip(1)
                .Select(line =>
                {
                    var data = line.Split(',');

                    var enabed = "true".Equals(ReadByName(data, "enabled"), StringComparison.OrdinalIgnoreCase);
                    return new DeviceInfo
                    {
                        Id = ReadByName(data, "deviceId"),
                        Enabed = enabed,
                        Country = ReadByName(data, "country"),
                        Building = ReadByName(data, "building"),
                        Floor = ReadByName(data, "floor"),
                        SensorType = ReadByName(data, "sensorType")
                    };
                })
                .ToList();
        }
    }
}
