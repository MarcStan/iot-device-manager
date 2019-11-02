using IoTDeviceManager.Config;
using IoTDeviceManager.Framework;
using IoTDeviceManager.Models;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IoTDeviceManager.ViewModels.Controls
{
    public class QueryInputViewModel : ViewModelBase
    {
        private readonly RegistryManager _registryManager;
        private string? _deviceQuery;
        private string? _previousDeviceQuery;
        private bool _isQueryRunning;

        public QueryInputViewModel(
            IOptions<ConnectionStrings> connectionStrings)
        {
            _registryManager = RegistryManager.CreateFromConnectionString(connectionStrings.Value.IoTHub);

            ExecuteDeviceQueryCommand = new RelayCommand(async () => await QueryDevicesAsync(false));
            ExecuteDeviceQueryOnEnterCommand = new RelayCommand(async () => await QueryDevicesAsync(true));
            ClearQueryCommand = new RelayCommand(() => DeviceQuery = null);
        }

        public event GenericEventHandler<QueryInputViewModel, IReadOnlyList<DeviceModel>>? QueryResultUpdated;

        public event GenericEventHandler<QueryInputViewModel, string>? QueryError;

        public ICommand ExecuteDeviceQueryCommand { get; }

        public ICommand ExecuteDeviceQueryOnEnterCommand { get; }

        public ICommand ClearQueryCommand { get; }

        public string QueryPrefix => "SELECT * FROM devices";

        /// <summary>
        /// The user provided query without the query prefix.
        /// </summary>
        public string? DeviceQuery
        {
            get => _deviceQuery;
            set
            {
                if (SetProperty(ref _deviceQuery, value))
                {
                    OnPropertyChanged(nameof(HasDeviceQuery));
                    ExecuteDeviceQueryCommand.Execute(null);
                }
            }
        }

        public bool IsQueryRunning
        {
            get => _isQueryRunning;
            set { SetProperty(ref _isQueryRunning, value); }
        }

        public bool HasDeviceQuery => !string.IsNullOrEmpty(DeviceQuery);

        public async Task QueryDevicesAsync(bool force)
        {
            var queryString = DeviceQuery ?? "";
            if (!force)
            {
                // abort if nobody is listening or already running
                if (QueryResultUpdated == null || IsQueryRunning)
                    return;

                if (queryString == _previousDeviceQuery)
                    return;
            }

            _previousDeviceQuery = queryString;
            queryString = QueryPrefix + (string.IsNullOrEmpty(queryString) ? "" : $" {queryString}");
            try
            {
                IsQueryRunning = true;
                var query = _registryManager.CreateQuery(queryString);
                var twins = new List<Twin>();
                while (query.HasMoreResults)
                {
                    var results = await query.GetNextAsTwinAsync();
                    twins.AddRange(results);
                }
                QueryResultUpdated?.Invoke(this, twins.Select(t =>
                {
                    return new DeviceModel
                    {
                        DeviceName = t.DeviceId,
                        ETag = t.ETag
                    };
                }).ToList());
            }
            catch (Exception e)
            {
                QueryError?.Invoke(this, e.Message);
            }
            finally
            {
                IsQueryRunning = false;
            }
        }
    }
}
