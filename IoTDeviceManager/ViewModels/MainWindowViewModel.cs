using IoTDeviceManager.Models;
using IoTDeviceManager.ViewModels.Controls;
using System.Collections.Generic;

namespace IoTDeviceManager.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private FilteredDeviceListViewModel _filteredDeviceListViewModel;
        private QueryInputViewModel _queryInputViewModel;
        private string? _error;

        public MainWindowViewModel(
            QueryInputViewModel queryInputViewModel,
            FilteredDeviceListViewModel filteredDeviceListViewModel)
        {
            _queryInputViewModel = queryInputViewModel;
            _filteredDeviceListViewModel = filteredDeviceListViewModel;

            queryInputViewModel.QueryResultUpdated += OnQueryUpdated;
            // initial query
            queryInputViewModel.ExecuteDeviceQueryCommand.Execute(null);
            queryInputViewModel.QueryError += OnQueryError;
        }

        public QueryInputViewModel QueryInputViewModel
        {
            get => _queryInputViewModel;
            set { SetProperty(ref _queryInputViewModel, value); }
        }

        public FilteredDeviceListViewModel FilteredDeviceListViewModel
        {
            get => _filteredDeviceListViewModel;
            set { SetProperty(ref _filteredDeviceListViewModel, value); }
        }

        public string? Error
        {
            get => _error;
            set
            {
                if (SetProperty(ref _error, value) && !string.IsNullOrEmpty(Error))
                    OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(Error);

        private void OnQueryUpdated(QueryInputViewModel sender, IReadOnlyList<DeviceModel> args)
        {
            Error = null;
        }

        private void OnQueryError(QueryInputViewModel sender, string errorMessage)
        {
            Error = "Query error: " + errorMessage;
        }
    }
}
