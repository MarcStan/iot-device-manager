using IoTDeviceManager.Framework;
using IoTDeviceManager.Models;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IoTDeviceManager.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ObservableCollection<DeviceModel> _devices;
        private readonly RegistryManager _registryManager;
        private string? _deviceQuery;
        private string? _previousDeviceQuery;

        public MainWindowViewModel(
            IConfiguration configuration)
        {
            const string key = "ConnectionStrings:IoTHub";
            var connectionString = configuration[key];
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException($"Missing required connectionstring @{key}");

            _registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            _devices = new ObservableCollection<DeviceModel>(Enumerable.Range(0, 5).Select(i => new DeviceModel { DeviceName = $"Test #{i}" }));

            ExecuteDeviceQueryCommand = new RelayCommand(async () => await QueryDevicesAsync());
            ClearQueryCommand = new RelayCommand(() => DeviceQuery = null);
        }

        public ICommand ExecuteDeviceQueryCommand { get; }

        public ICommand ClearQueryCommand { get; }

        public string? DeviceQuery
        {
            get => _deviceQuery;
            set
            {
                if (SetProperty(ref _deviceQuery, value))
                {
                    OnPropertyChanged(nameof(HasDeviceQuery));
                    ExecuteDeviceQueryCommand.Execute(null);
                }
            }
        }

        public bool HasDeviceQuery => !string.IsNullOrEmpty(DeviceQuery);

        public ObservableCollection<DeviceModel> Devices
        {
            get => _devices;
            set { SetProperty(ref _devices, value); }
        }

        public async Task QueryDevicesAsync()
        {
            var queryString = DeviceQuery ?? "";
            if (queryString == _previousDeviceQuery)
                return;

            _previousDeviceQuery = queryString;
            queryString = "SELECT * FROM devices" + (string.IsNullOrEmpty(queryString) ? "" : $" {queryString}");
            try
            {
                var query = _registryManager.CreateQuery(queryString);
                var twins = new List<Twin>();
                while (query.HasMoreResults)
                {
                    var results = await query.GetNextAsTwinAsync();
                    twins.AddRange(results);
                }
                Devices = new ObservableCollection<DeviceModel>(twins.Select(t =>
                {
                    return new DeviceModel
                    {
                        DeviceName = t.DeviceId
                    };
                }));
            }
            catch (Exception)
            {
                //throw;
            }
        }
    }
}
