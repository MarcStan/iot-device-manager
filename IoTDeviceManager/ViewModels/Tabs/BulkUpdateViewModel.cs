using IoTDeviceManager.Config;
using IoTDeviceManager.Framework;
using IoTDeviceManager.ViewModels.Controls;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IoTDeviceManager.ViewModels.Tabs
{
    public class BulkUpdateViewModel : ViewModelBase
    {
        private readonly RegistryManager _registryManager;
        private string _overheatThreshold;
        private string _errorMessage = "";
        private readonly FilteredDeviceListViewModel _filteredDeviceListViewModel;

        public BulkUpdateViewModel(
            IOptions<ConnectionStrings> connectionStrings,
            FilteredDeviceListViewModel filteredDeviceListViewModel)
        {
            _registryManager = RegistryManager.CreateFromConnectionString(connectionStrings.Value.IoTHub);

            _overheatThreshold = "60";
            OverheatThresholdCommand = new RelayCommand(async () => await UpdateThresholdAsync());
            _filteredDeviceListViewModel = filteredDeviceListViewModel;
        }

        public string OverheatThreshold
        {
            get => _overheatThreshold;
            set { SetProperty(ref _overheatThreshold, value); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { SetProperty(ref _errorMessage, value); }
        }

        public ICommand OverheatThresholdCommand { get; }

        private async Task UpdateThresholdAsync()
        {
            try
            {
                ErrorMessage = "Processing..";
                if (!int.TryParse(OverheatThreshold, out int threshold))
                    throw new ArgumentException("Threshold is not a valid integer!");

                var desiredProperties = JsonConvert.SerializeObject(new
                {
                    overheatThreshold = threshold
                });
                var twins = _filteredDeviceListViewModel.Devices
                    .Select(d => new Twin(d.Id)
                    {
                        ETag = d.ETag,
                        Properties = new TwinProperties
                        {
                            Desired = new TwinCollection(desiredProperties)
                        }
                    })
                    .ToList();
                await _registryManager.UpdateTwins2Async(twins);
                ErrorMessage = $"Desired properties updated on all {twins.Count} devices!";
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
            }
        }
    }
}
