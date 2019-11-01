using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IoTDeviceManager.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        protected void SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
        {
            if ((backingField == null && value == null) ||
                (backingField != null && backingField.Equals(value)))
                return;

            backingField = value;
            OnPropertyChanged(propertyName);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
