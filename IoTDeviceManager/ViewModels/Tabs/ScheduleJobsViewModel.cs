using IoTDeviceManager.Config;
using IoTDeviceManager.Framework;
using IoTDeviceManager.Models;
using IoTDeviceManager.ViewModels.Controls;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        private string _jobStatus = "";

        public ScheduleJobsViewModel(
            IOptions<ConnectionStrings> connectionStrings,
            FilteredDeviceListViewModel filteredDeviceListViewModel)
        {
            _filteredDeviceListViewModel = filteredDeviceListViewModel;
            _jobClient = JobClient.CreateFromConnectionString(connectionStrings.Value.IoTHub);
            _targetVersion = "1.1";
            _updateTimes = new ObservableCollection<string>(new[]
            {
                "5 seconds from now",
                "10 seconds from now",
                "30 seconds from now",
                "1 minute from now"
            });
            _selectedupdateTime = _updateTimes.First();
            UpdateFirmwareCommand = new RelayCommand(async () => await UpdateFirmwareAsync());
            _timer = new Timer(OnTick, null, Timeout.Infinite, 1000);
        }

        public ICommand UpdateFirmwareCommand { get; }

        public string JobStatus
        {
            get => _jobStatus;
            set { SetProperty(ref _jobStatus, value); }
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
                    JobStatus = "No devices would be affected by this job!";
                    return;
                }

                var query = _filteredDeviceListViewModel.ExecutedQuery;

                if (string.IsNullOrWhiteSpace(query) &&
                    MessageBox.Show("Since you have not entered a query all devices will be updated at once. Are you sure?", "Update all devices?", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return;
                }
                // must use * for all devices instead of empty query
                if (string.IsNullOrWhiteSpace(query))
                    query = "*";

                var uid = Guid.NewGuid().ToString().Substring(0, 5);
                var jobId = $"firmware-update-{uid}";

                var desiredDelayInSeconds = GetFromUserSelection(SelectedUpdateTime);
                var scheduledTime = DateTime.UtcNow.AddSeconds(desiredDelayInSeconds);
                const int ttl = 120;
                var firmwareUpdateMethod = new CloudToDeviceMethod("UpdateFirmwareNow")
                    .SetPayloadJson(JsonConvert.SerializeObject(new
                    {
                        targetVersion = TargetVersion
                    }));
                ScheduledJob = new ScheduledJob(jobId, scheduledTime, _filteredDeviceListViewModel.Devices.Count);
                _timer.Change(0, 1000);
                OnPropertyChanged(nameof(CanForceUpdate));
                OnPropertyChanged(nameof(IsJobRunning));
                await _jobClient.ScheduleDeviceMethodAsync(jobId, query, firmwareUpdateMethod, scheduledTime, ttl);
            }
            catch (Exception e)
            {
                ScheduledJob = null;
                _timer.Change(Timeout.Infinite, 1000);
                JobStatus = e.Message;
            }
        }

        private int GetFromUserSelection(string selected)
        {
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
            {
                JobStatus = "Job has executed. Check individual devices now!";
                _timer.Change(Timeout.Infinite, 1000);
                return;
            }

            var delta = (job.ScheduledTime - DateTime.UtcNow).Seconds;
            if (delta <= 0)
            {
                JobStatus = "Executing job..";
                // run one more time with delay to set fake "completed" message above
                ScheduledJob = null;
                _timer.Change(2000, 1000);
                return;
            }

            JobStatus = $"Job {job.JobId} is scheduled to run in {delta} seconds and should affect {job.AffectedDeviceCount} devices..";
        }
    }
}
