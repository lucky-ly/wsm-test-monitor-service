using System;
using System.ComponentModel;
using System.Diagnostics;
using Core.Interfaces;

namespace MonitorService
{
    internal class ProcessInfo
    {
        private TimeSpan _lastTotalProcessorTime;
        private readonly bool _cpuTimeReadAllowed = true;
        private readonly PerformanceCounter _cpuCounter;

        public ProcessInfo(DateTime startTime, TimeSpan lastTotalProcessorTime)
        {
            StartTime = startTime;
            _lastTotalProcessorTime = lastTotalProcessorTime;
        }

        private ProcessInfo(DateTime startTime, PerformanceCounter perfCounter)
        {
            StartTime = startTime;
            _cpuCounter = perfCounter;
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
                // !! some processes don't allow to get this
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
                var name = ProcessUtils.GetInstanceNameForProcessId(process.Id);
                var cpuCounter = new PerformanceCounter("Process", "% Processor Time", name, true);
                logger.Warn($"Process {process.Id} doesn't allow accessing its info, trying to get it via counter");

                var uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
                var startTime = DateTime.Now.Subtract(uptime);

                instance = new ProcessInfo(startTime, cpuCounter);
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
                _lastTotalProcessorTime = process.TotalProcessorTime;
                CpuUsage = ProcessUtils.GetCpuUsageFast(process, lastCheckTime, _lastTotalProcessorTime);
            }
            else
            {
                CpuUsage = _cpuCounter.NextValue() / 100 / Environment.ProcessorCount;
            }

            ThreadsCount = process.Threads.Count;
            RamUsageBytes = process.WorkingSet64; // there are locked pages which aren't counted for some reason
        }
    }
}
