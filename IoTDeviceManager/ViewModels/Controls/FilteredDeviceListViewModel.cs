using IoTDeviceManager.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace IoTDeviceManager.ViewModels.Controls
{
    public class FilteredDeviceListViewModel : ViewModelBase
    {
        private ObservableCollection<DeviceModel> _devices;

        public FilteredDeviceListViewModel(
            QueryInputViewModel queryInputViewModel)
        {
            queryInputViewModel.QueryResultUpdated += OnQueryUpdated;

            _devices = new ObservableCollection<DeviceModel>();
        }

        public string DeviceFilterHeader
            => _devices.Count != 1 ?
                $"Filtered devices ({_devices.Count} results)" :
                $"Filtered devices ({_devices.Count} result)";

        public ObservableCollection<DeviceModel> Devices
        {
            get => _devices;
            set
            {
                if (SetProperty(ref _devices, value))
                    OnPropertyChanged(nameof(DeviceFilterHeader));
            }
        }

        private void OnQueryUpdated(QueryInputViewModel sender, IReadOnlyList<DeviceModel> queryResult)
        {
            Devices = new ObservableCollection<DeviceModel>(queryResult.OrderBy(x => x.DeviceName));
        }
    }
}
