using System;
using System.ComponentModel;
using System.Diagnostics;
using MonitorService.Interfaces;

namespace MonitorService
{
    internal class ProcessInfo
    {
        private TimeSpan _lastTotalProcessorTime;
        private readonly bool _cpuTimeReadAllowed = true;

        public ProcessInfo(DateTime startTime, TimeSpan lastTotalProcessorTime)
        {
            StartTime = startTime;
            _lastTotalProcessorTime = lastTotalProcessorTime;
        }

        private ProcessInfo(DateTime startTime)
        {
            StartTime = startTime;
            _cpuTimeReadAllowed = false;
        }

        public string Name { get; set; }
        public double CpuUsage { get; private set; }
        public long RamUsageBytes { get; private set; }
        public int ProcessId { get; set; }
        public int ThreadsCount { get; private set; }
        public DateTime StartTime { get; set; }

        public static ProcessInfo Create(Process process, ILogger logger)
        {
            ProcessInfo instance;

            try
            {
                var lastTotalProcessorTime = process.TotalProcessorTime;
                var startTime = process.StartTime;

                instance = new ProcessInfo(startTime, lastTotalProcessorTime);
            }
            catch (InvalidOperationException)
            {
                logger.Warn($"Process {process.Id} is already terminated");
                return null;
            }
            catch (Win32Exception)
            {
                var uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
                var startTime = DateTime.Now.Subtract(uptime);
                instance = new ProcessInfo(startTime);
            }

            instance.Name = process.ProcessName;
            instance.ProcessId = process.Id;
            instance.Update(process, DateTime.Now);

            return instance;
        }

        public void Update(Process process, DateTime lastCheckTime)
        {
            if (_cpuTimeReadAllowed)
            {
                CpuUsage = process.GetCpuUsageFast(lastCheckTime, _lastTotalProcessorTime);
                _lastTotalProcessorTime = process.TotalProcessorTime;
            }
            else
            {
                CpuUsage = 0;
            }

            ThreadsCount = process.Threads.Count;
            RamUsageBytes = process.WorkingSet64;
        }
    }
}
