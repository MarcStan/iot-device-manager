using IoTDeviceManager.Framework;
using IoTDeviceManager.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace IoTDeviceManager
{
    public partial class App
    {
        private readonly IHost _host;

        public App()
        {
            var hostBuilder = Host.CreateDefaultBuilder()
                // always use development as this is a sample client
                .UseEnvironment("Development")
                .ConfigureAppConfiguration((context, builder) =>
                {
                    if (context.HostingEnvironment.IsDevelopment())
                        builder.AddUserSecrets<App>();
                })
                .ConfigureServices((_, services) =>
                {
                    services.Scan(scan =>
                    {
                        scan.FromAssemblyOf<App>()
                            // views
                            .AddClasses(x => x.AssignableTo<Window>())
                            .AsSelf()
                            .WithTransientLifetime()
                            // viewmodels
                            .AddClasses(x => x.AssignableTo<ViewModelBase>())
                            .AsSelf()
                            .WithTransientLifetime()
                            .AddClasses()
                            .AsMatchingInterface()
                            .WithTransientLifetime();
                    });
                });

            _host = hostBuilder.Build();
        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var navigator = _host.Services.GetRequiredService<INavigator>();
            await navigator.ShowAsync<MainWindowViewModel>();
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            using (_host)
                await _host.StopAsync();
        }
    }
}
