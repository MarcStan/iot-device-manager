using IoTDeviceManager.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace IoTDeviceManager.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ObservableCollection<Device> _devices;

        public MainWindowViewModel()
        {
            _devices = new ObservableCollection<Device>(Enumerable.Range(0, 5).Select(i => new Device { DeviceName = $"Test #{i}" }));
        }

        public ObservableCollection<Device> Devices
        {
            get => _devices;
            set { SetProperty(ref _devices, value); }
        }
    }
}
