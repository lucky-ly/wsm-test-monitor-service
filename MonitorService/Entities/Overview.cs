using System;
using System.Collections.Generic;

namespace MonitorService.Entities
{
    public class Overview
    {
        public DateTime UpdateTime { get; set; }
        public double CpuUsage { get; set; }
        public long RamUsageBytes { get; set; }
        public IEnumerable<ProcessInfoData> Processes { get; set; }
        public IEnumerable<Notification> Notifications { get; set; }
    }
}