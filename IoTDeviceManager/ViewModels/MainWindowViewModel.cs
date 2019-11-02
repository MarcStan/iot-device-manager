using IoTDeviceManager.Models;
using IoTDeviceManager.ViewModels.Controls;
using IoTDeviceManager.ViewModels.Tabs;
using System.Collections.Generic;

namespace IoTDeviceManager.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private FilteredDeviceListViewModel _filteredDeviceListViewModel;
        private QueryInputViewModel _queryInputViewModel;
        private BulkUpdateViewModel _bulkUpdateViewModel;
        private ScheduleJobsViewModel _scheduleJobsViewModel;
        private string? _error;

        public MainWindowViewModel(
            QueryInputViewModel queryInputViewModel,
            FilteredDeviceListViewModel filteredDeviceListViewModel,
            BulkUpdateViewModel bulkUpdateViewModel,
            ScheduleJobsViewModel scheduleJobsViewModel)
        {
            _queryInputViewModel = queryInputViewModel;
            _filteredDeviceListViewModel = filteredDeviceListViewModel;
            _bulkUpdateViewModel = bulkUpdateViewModel;
            _scheduleJobsViewModel = scheduleJobsViewModel;

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

        public BulkUpdateViewModel BulkUpdateViewModel
        {
            get => _bulkUpdateViewModel;
            set { SetProperty(ref _bulkUpdateViewModel, value); }
        }

        public ScheduleJobsViewModel ScheduleJobsViewModel
        {
            get => _scheduleJobsViewModel;
            set { SetProperty(ref _scheduleJobsViewModel, value); }
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
