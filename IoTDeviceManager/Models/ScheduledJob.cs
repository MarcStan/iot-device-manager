using System;

namespace IoTDeviceManager.Models
{
    public class ScheduledJob
    {
        public ScheduledJob(string jobId, DateTime scheduledTime, int affectedDeviceCount)
        {
            JobId = jobId;
            ScheduledTime = scheduledTime;
            AffectedDeviceCount = affectedDeviceCount;
        }

        public string JobId { get; set; }

        public DateTime ScheduledTime { get; set; }

        public int AffectedDeviceCount { get; set; }
    }
}
