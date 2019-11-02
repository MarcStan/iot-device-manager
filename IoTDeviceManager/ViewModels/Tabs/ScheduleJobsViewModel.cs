using IoTDeviceManager.Config;
using IoTDeviceManager.Framework;
using IoTDeviceManager.Models;
using IoTDeviceManager.ViewModels.Controls;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IoTDeviceManager.ViewModels.Tabs
{
    public class ScheduleJobsViewModel : ViewModelBase
    {
        private readonly JobClient _jobClient;
        private readonly FilteredDeviceListViewModel _filteredDeviceListViewModel;
        private string _targetVersion;
        private string _selectedupdateTime;
        private ObservableCollection<string> _updateTimes;
        private ScheduledJob? _scheduledJob;
        private Timer _timer;
        private string _jobStatusMessage = "";

        public ScheduleJobsViewModel(
            IOptions<ConnectionStrings> connectionStrings,
            FilteredDeviceListViewModel filteredDeviceListViewModel)
        {
            _filteredDeviceListViewModel = filteredDeviceListViewModel;
            _jobClient = JobClient.CreateFromConnectionString(connectionStrings.Value.IoTHub);
            _targetVersion = "1.1";
            _updateTimes = new ObservableCollection<string>(new[]
            {
                "immediately",
                "5 seconds from now",
                "10 seconds from now",
                "30 seconds from now",
                "1 minute from now"
            });
            _selectedupdateTime = _updateTimes[1];
            UpdateFirmwareCommand = new RelayCommand(async () => await UpdateFirmwareAsync());
            _timer = new Timer(OnTick, null, Timeout.Infinite, 1000);
        }

        public ICommand UpdateFirmwareCommand { get; }

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
                    OnPropertyChanged(nameof(CanForceUpdate));
                }
            }
        }

        public string TargetVersion
        {
            get => _targetVersion;
            set
            {
                if (SetProperty(ref _targetVersion, value))
                    OnPropertyChanged(nameof(CanForceUpdate));
            }
        }

        public string SelectedUpdateTime
        {
            get => _selectedupdateTime;
            set { SetProperty(ref _selectedupdateTime, value); }
        }

        public ObservableCollection<string> UpdateTimes
        {
            get => _updateTimes;
            set { SetProperty(ref _updateTimes, value); }
        }

        public bool CanForceUpdate
            => !string.IsNullOrEmpty(TargetVersion) && !IsJobRunning;

        public bool IsJobRunning
            => ScheduledJob != null;

        private async Task UpdateFirmwareAsync()
        {
            if (ScheduledJob != null)
                return;

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
                var jobId = $"firmware-update-{uid}";

                var desiredDelayInSeconds = GetDelayInSecondsFromUserSelection(SelectedUpdateTime);
                var scheduledTime = DateTime.UtcNow.AddSeconds(desiredDelayInSeconds);
                // execution limit of job itself
                // if a device is not online within that window it will simply not be called
                const int jobTtlInSeconds = 120;
                var firmwareUpdateMethod = new CloudToDeviceMethod("UpdateFirmwareNow")
                    .SetPayloadJson(JsonSerializer.Serialize(new
                    {
                        targetVersion = TargetVersion
                    }));
                ScheduledJob = new ScheduledJob(jobId, scheduledTime, _filteredDeviceListViewModel.Devices.Count);
                _timer.Change(0, 1000);
                var response = await _jobClient.ScheduleDeviceMethodAsync(jobId, query, firmwareUpdateMethod, scheduledTime, jobTtlInSeconds);
                await WaitForJobCompletionAsync(response.JobId);
                ScheduledJob = null;
                JobStatusMessage = "Job has executed. Check individual devices now!";
            }
            catch (Exception e)
            {
                ScheduledJob = null;
                _timer.Change(Timeout.Infinite, 1000);
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

        private int GetDelayInSecondsFromUserSelection(string selected)
        {
            if (selected.StartsWith("immediate"))
                return 0;

            // assume value + time in format "60 seconds", "1 minute", ..
            // -> can parse number and use first letter as multiplier.. good enough

            var separatorIndex = selected.IndexOf(' ');
            var time = selected.Substring(0, separatorIndex);
            return int.Parse(time) * (selected[separatorIndex + 1] == 'm' ? 60 : 1);
        }

        private void OnTick(object? state)
        {
            var job = ScheduledJob;
            if (job == null)
                return;

            var delta = (job.ScheduledTime - DateTime.UtcNow).Seconds;
            if (delta <= 0)
            {
                JobStatusMessage = "Executing job..";
                // run one more time with delay to set fake "completed" message above
                ScheduledJob = null;
                _timer.Change(Timeout.Infinite, 1000);
                return;
            }

            JobStatusMessage = $"Job {job.JobId} is scheduled to run in {delta} seconds and should affect {job.AffectedDeviceCount} devices..";
        }
    }
}
