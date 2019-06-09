using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using MonitorService.Entities;
using MonitorService.Interfaces;

namespace MonitorService
{
    internal class ProcessInfoBackgroundService
    {
        private readonly ILogger _logger;
        private Timer _timer;
        private int _timerPeriodMs;
        private DateTime _lastTickTime;

        private ConcurrentDictionary<int, Process> _processes;
        private ConcurrentDictionary<int, ProcessInfo> _processInfoCache;

        private readonly ManagementEventWatcher _processStartEventWatcher =
            new ManagementEventWatcher("SELECT * FROM Win32_ProcessStartTrace");

        private readonly ManagementEventWatcher _processStopEventWatcher =
            new ManagementEventWatcher("SELECT * FROM Win32_ProcessStopTrace");

        private PerformanceCounter _cpuCounter;
        private RamCounter _ramCounter;
        private readonly int _cpuUsageNotificationPercent;
        private readonly int _ramUsageNotificationPercent;
        private readonly int _ramUsageNotificationBytes;

        public ProcessInfoBackgroundService(ILogger logger, ServiceConfiguration config)
        {
            logger.Info("Back constructor called");
            _timerPeriodMs = config.UpdateFrequency;
            _cpuUsageNotificationPercent = config.CpuUsageNotificationPercent;
            _ramUsageNotificationPercent = config.RamUsageNotificationPercent;
            _ramUsageNotificationBytes = config.RamUsageNotificationBytes;

            _logger = logger;
        }

        public void StartWatch()
        {
            _processes = new ConcurrentDictionary<int, Process>();
            _processInfoCache = new ConcurrentDictionary<int, ProcessInfo>();

            _processStartEventWatcher.EventArrived += ProcessStartedHandler;
            _processStartEventWatcher.Start();
            _processStopEventWatcher.EventArrived += ProcessStoppedHandler;
            _processStopEventWatcher.Start();

            InitCaches();
            InitOverallCounters();

            if (_timer == null)
                _timer = new Timer(TimerTick, null, _timerPeriodMs, _timerPeriodMs);
            else
                _timer.Change(0, _timerPeriodMs);

            _logger.Info($"watch started");
        }

        private void ProcessStoppedHandler(object sender, EventArrivedEventArgs e)
        {
            var processId = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
            var processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            _logger.Info($"process {processName} ({processId}) exited");
            if (_processes.TryGetValue(processId, out var process))
            {
                process.Dispose();
            }
            _processes.TryRemove(processId, out _);
            _processInfoCache.TryRemove(processId, out _);
        }

        private void ProcessStartedHandler(object sender, EventArrivedEventArgs e)
        {
            var processId = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
            var processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            
            _logger.Info($"process {processName} ({processId}) started");

            try
            {
                var process = Process.GetProcessById(processId);
                if (process.HasExited) return;

                _processes.TryAdd(processId, process);
                _processInfoCache.TryAdd(processId, ProcessInfo.Create(process, _logger));
            }
            catch (ArgumentException)
            {
            }
        }

        public void StopWatch()
        {
            _timer?.Change(Timeout.Infinite, _timerPeriodMs);
            _logger.Info($"watch stopped");
        }

        public void SetCheckPeriod(int periodMs)
        {
            _timerPeriodMs = periodMs;
            _timer?.Change(periodMs, periodMs);
        }

        public IEnumerable<ProcessInfo> GetProcessInfo()
        {
            return _processInfoCache.Values;
        }

        private void InitCaches()
        {
            var processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                _processes.TryAdd(process.Id, process);
                var processInfo = ProcessInfo.Create(process, _logger);
                if (processInfo != null) _processInfoCache.TryAdd(process.Id, processInfo);
            }
        }

        private void InitOverallCounters()
        {
            _cpuCounter = new PerformanceCounter
            {
                CategoryName = "Processor",
                CounterName = "% Processor Time",
                InstanceName = "_Total"
            };

            _ramCounter = new RamCounter();
        }

        private void UpdateCaches()
        {
            // update cached processes
            foreach (var process in _processes.Values)
            {
                try
                {
                    process.Refresh();
                }
                catch (Exception e)
                {
                    _logger.Error($"process.Refresh failed for {process.Id}", e);
                }

                try
                {
                    if (_processInfoCache.TryGetValue(process.Id, out var processInfo))
                    {
                        processInfo.Update(process, _lastTickTime);
                    }
                }
                catch (Win32Exception)
                {
                    // do nothing, it will be removed from cache when event handler will be called
                }
                catch (Exception e)
                {
                    _logger.Error($"cache update failed for {process.Id}", e);
                }
            }
        }

        private Overview CreateOverview()
        {
            var cpuUsage = _cpuCounter.NextValue() / 100;
            var ramUsage = _ramCounter.GetRamUsageBytes();
            var ramUsagePercent = _ramCounter.GetRamUsagePercentage();
            var notifications = new List<Notification>();

            if (_cpuUsageNotificationPercent > 0 && cpuUsage > _cpuUsageNotificationPercent)
            {
                notifications.Add(new Notification
                {
                    Type = NotificationType.CpuUsage,
                    Message = $"Cpu usage ({cpuUsage}%) exceeded {_cpuUsageNotificationPercent}%."
                });
            }

            if (_ramUsageNotificationPercent > 0 && ramUsagePercent > _ramUsageNotificationPercent)
            {
                notifications.Add(new Notification
                {
                    Type = NotificationType.RamUsage,
                    Message = $"Ram usage ({ramUsagePercent}%) exceeded {_ramUsageNotificationPercent}%."
                });
            }

            if (_ramUsageNotificationBytes > 0 && ramUsage > _ramUsageNotificationBytes)
            {
                notifications.Add(new Notification
                {
                    Type = NotificationType.RamUsage,
                    Message = $"Ram usage ({ramUsage} bytes) exceeded {_ramUsageNotificationBytes} bytes."
                });
            }

            var overview = new Overview
            {
                Notifications = notifications,
                CpuUsage = cpuUsage,
                RamUsageBytes = ramUsage,
                Processes = _processInfoCache.Select(x => new ProcessInfoData
                {
                    Name = x.Value.Name,
                    ProcessId = x.Key,
                    CpuUsage = x.Value.CpuUsage,
                    RamUsageBytes = x.Value.RamUsageBytes,
                    ThreadsCount = x.Value.ThreadsCount,
                    StartTime = x.Value.StartTime,
                }),
                UpdateTime = DateTime.Now
            };

            return overview;
        }

        private void TimerTick(object state)
        {
            try
            {
                UpdateCaches();
                var overview = CreateOverview();
                OnInfoUpdated?.Invoke(overview);

                _lastTickTime = DateTime.Now;
            }
            catch (Exception e)
            {
                _logger.Error("Unexpected exception in main timer", e);
            }
        }

        public event Func<Overview, Task> OnInfoUpdated;
    }
}
