using IoTDeviceManager.ViewModels;
using System.Threading.Tasks;

namespace IoTDeviceManager.Framework
{
    public interface INavigator
    {
        Task ShowAsync<TViewModel>() where TViewModel : ViewModelBase;
    }
}
