using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace IoTClient.Cli
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
                })
                .ConfigureServices((_, services) =>
                {
                    services.AddHostedService<SimulateManyDevicesToCloudWorker>();
                });

            using var host = hostBuilder.Build();
            await host.RunAsync();
        }
    }
}
