using IoTDeviceManager.Config;
using IoTDeviceManager.Framework;
using IoTDeviceManager.Models;
using IoTDeviceManager.ViewModels.Controls;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IoTDeviceManager.ViewModels.Tabs
{
    public class CloudToDeviceViewModel : ViewModelBase
    {
        private readonly JobClient _jobClient;
        private readonly FilteredDeviceListViewModel _filteredDeviceListViewModel;
        private ScheduledJob? _scheduledJob;
        private string _methodName = "";
        private string _jobStatusMessage = "";
        private string _methodBody = "";

        public CloudToDeviceViewModel(
            IOptions<ConnectionStrings> connectionStrings,
            FilteredDeviceListViewModel filteredDeviceListViewModel)
        {
            _filteredDeviceListViewModel = filteredDeviceListViewModel;
            _jobClient = JobClient.CreateFromConnectionString(connectionStrings.Value.IoTHub);
            ExecuteCommand = new RelayCommand(async () => await ExecuteCommandAsync());
        }

        public ICommand ExecuteCommand { get; }

        public string JobStatusMessage
        {
            get => _jobStatusMessage;
            set { SetProperty(ref _jobStatusMessage, value); }
        }

        public ScheduledJob? ScheduledJob
        {
            get => _scheduledJob;
            set
            {
                if (SetProperty(ref _scheduledJob, value))
                {
                    OnPropertyChanged(nameof(IsJobRunning));
                    OnPropertyChanged(nameof(CanExecute));
                }
            }
        }

        public string MethodName
        {
            get => _methodName;
            set
            {
                if (SetProperty(ref _methodName, value))
                    OnPropertyChanged(nameof(CanExecute));
            }
        }

        public string MethodBody
        {
            get => _methodBody;
            set { SetProperty(ref _methodBody, value); }
        }

        public bool CanExecute
            => !string.IsNullOrEmpty(MethodName) && !IsJobRunning;

        public bool IsJobRunning
            => ScheduledJob != null;

        private async Task ExecuteCommandAsync()
        {
            if (ScheduledJob != null)
                return;

            var name = MethodName;
            var body = MethodBody;
            // must be valid json
            if (string.IsNullOrEmpty(body))
                body = "{}";
            try
            {
                _ = JsonSerializer.Deserialize<object>(body);
            }
            catch (Exception)
            {
                JobStatusMessage = "Invalid body. Must be valid json!";
                return;
            }

            try
            {
                var twinsToUpdate = _filteredDeviceListViewModel.Devices
                    .Select(device => new Twin(device.Id)
                    {
                        ETag = device.ETag
                    })
                    .ToList();

                if (twinsToUpdate.Count == 0)
                {
                    JobStatusMessage = "No devices would be affected by this job!";
                    return;
                }

                var query = _filteredDeviceListViewModel.ExecutedQuery;

                if (string.IsNullOrWhiteSpace(query) &&
                    MessageBox.Show("Since you have not entered a query all devices will be updated at once. Are you sure?", "Update all devices?", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return;
                }
                if (string.IsNullOrWhiteSpace(query))
                {
                    // must use * for all devices instead of empty query
                    query = "*";
                }
                else if (query.StartsWith("where", StringComparison.OrdinalIgnoreCase))
                {
                    // job system does not like where as part of the query
                    query = query.Substring("where".Length).TrimStart();
                }

                var uid = Guid.NewGuid().ToString().Substring(0, 5);
                var jobId = $"{name}-{uid}";

                var scheduledTime = DateTime.UtcNow;
                // execution limit of job itself
                // if a device is not online within that window it will simply not be called
                const int jobTtlInSeconds = 120;
                var firmwareUpdateMethod = new CloudToDeviceMethod(name)
                    .SetPayloadJson(body);
                JobStatusMessage = "Executing job..";
                ScheduledJob = new ScheduledJob(jobId, scheduledTime, _filteredDeviceListViewModel.Devices.Count);
                var response = await _jobClient.ScheduleDeviceMethodAsync(jobId, query, firmwareUpdateMethod, scheduledTime, jobTtlInSeconds);
                await WaitForJobCompletionAsync(response.JobId);
                ScheduledJob = null;
                JobStatusMessage = "Job has executed. Check individual devices now!";
            }
            catch (Exception e)
            {
                ScheduledJob = null;
                JobStatusMessage = e.Message;
            }
        }

        private async Task WaitForJobCompletionAsync(string jobId)
        {
            JobResponse result;
            do
            {
                await Task.Delay(1000);
                result = await _jobClient.GetJobAsync(jobId);
            } while (result.Status != JobStatus.Completed && result.Status != JobStatus.Failed);
        }
    }
}
