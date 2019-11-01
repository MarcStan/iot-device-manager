using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IoTDeviceManager.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
        {
            if ((backingField == null && value == null) ||
                (backingField != null && backingField.Equals(value)))
                return false;

            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
