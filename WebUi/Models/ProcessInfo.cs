﻿using System;

namespace WebUi.Models
{
    public class ProcessInfo
    {
        public string Name { get; set; }
        public int ProcessId { get; set; }
        public double CpuUsage { get; set; }
        public long RamUsageBytes { get; set; }
        public int ThreadsCount { get; set; }
        public DateTime StartTime { get; set; }
    }
}