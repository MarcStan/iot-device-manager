using IoTDeviceManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace IoTDeviceManager
{
    public partial class App
    {
        private readonly IServiceProvider _serviceProvider;
        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
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
                    .WithTransientLifetime();
            });
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var window = _serviceProvider.GetRequiredService<MainWindow>();
            window.Show();
        }
    }
}
