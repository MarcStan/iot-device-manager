using IoTDeviceManager.Models;
using IoTDeviceManager.ViewModels.Controls;
using IoTDeviceManager.ViewModels.Tabs;
using System.Collections.Generic;

namespace IoTDeviceManager.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string? _error;

        public MainWindowViewModel(
            QueryInputViewModel queryInputViewModel,
            FilteredDeviceListViewModel filteredDeviceListViewModel,
            BulkUpdateViewModel bulkUpdateViewModel,
            ScheduleJobsViewModel scheduleJobsViewModel,
            CloudToDeviceViewModel cloudToDeviceViewModel)
        {
            QueryInputViewModel = queryInputViewModel;
            FilteredDeviceListViewModel = filteredDeviceListViewModel;
            BulkUpdateViewModel = bulkUpdateViewModel;
            ScheduleJobsViewModel = scheduleJobsViewModel;
            CloudToDeviceViewModel = cloudToDeviceViewModel;
            queryInputViewModel.QueryResultUpdated += OnQueryUpdated;
            // initial query
            queryInputViewModel.ExecuteDeviceQueryCommand.Execute(null);
            queryInputViewModel.QueryError += OnQueryError;
        }

        public QueryInputViewModel QueryInputViewModel { get; }

        public FilteredDeviceListViewModel FilteredDeviceListViewModel { get; }

        public BulkUpdateViewModel BulkUpdateViewModel { get; }

        public ScheduleJobsViewModel ScheduleJobsViewModel { get; }

        public CloudToDeviceViewModel CloudToDeviceViewModel { get; }

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
