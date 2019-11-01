using IoTDeviceManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace IoTDeviceManager.Framework
{
    public class Navigator : INavigator
    {
        private readonly IServiceProvider _serviceProvider;

        public Navigator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task ShowAsync<TViewModel>() where TViewModel : ViewModelBase
        {
            return Task.Run(() =>
            {
                // replaces both in namespace as well as class name
                var windowName = typeof(TViewModel).FullName?.Replace("ViewModel", "View") ?? throw new ArgumentException($"Type has no name: {typeof(TViewModel)}");
                var windowType = Type.GetType(windowName, false);
                if (windowType == null && windowName.EndsWith("View"))
                {
                    // try without suffix
                    windowName = windowName.Substring(0, windowName.Length - "View".Length);
                    windowType = Type.GetType(windowName, false);
                }
                if (windowType == null)
                    throw new EntryPointNotFoundException($"Could not find view for viewmodel {typeof(TViewModel).Name}. Last probed: {windowName}");
                if (!typeof(Window).IsAssignableFrom(windowType))
                    throw new NotSupportedException($"Only types implementing window are supported. {windowName} does not work.");

                var vm = _serviceProvider.GetRequiredService<TViewModel>();
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    var instance = (Window)_serviceProvider.GetRequiredService(windowType);

                    instance.DataContext = vm;
                    instance.Show();
                }));
            });
        }
    }
}
