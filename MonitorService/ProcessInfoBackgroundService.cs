using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Core.Interfaces;

namespace MonitorService
{
    internal class ProcessInfoBackgroundService
    {
        private readonly ILogger _logger;
        private Timer _timer;
        private int _timerPeriodMs;

        private static DateTime _lastTickTime;
        private static ConcurrentDictionary<int, Process> _processes;
        private static ConcurrentDictionary<int, ProcessInfo> _processInfoCache;

        private static ProcessInfoBackgroundService Instance { get; set; }
        public static ProcessInfoBackgroundService GetInstance(ILogger logger) =>
            Instance ?? (Instance = new ProcessInfoBackgroundService(logger));


        public ProcessInfoBackgroundService(ILogger logger)
        {
            logger.Info("Back constructor called");
            _timerPeriodMs = 1000;
            _logger = logger;
        }

        public void StartWatch()
        {
            Thread.Sleep(10000);

            InitCache();
            InitProcessInfoCache();

            /*
            if (_timer == null)
                _timer = new Timer(TimerTick, null, _timerPeriodMs, _timerPeriodMs);
            else
                _timer.Change(0, _timerPeriodMs);
            */

            var state = new object();
            var tempThread = new Thread(() =>
            {
                while(true) { TimerTick(state); Thread.Sleep(_timerPeriodMs); }
            })
            {
                IsBackground = true,
            };

            tempThread.Start();

            _lastTickTime = DateTime.Now;
            _logger.Info($"watch started");
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

        private void InitCache()
        {
            var currentProcesses = Process.GetProcesses();
            _processes = new ConcurrentDictionary<int, Process>(Environment.ProcessorCount * 2, currentProcesses.Length);

            _logger.Info("");

            foreach (var process in currentProcesses)
            {
                _processes[process.Id] = process;
            }
        }

        private void InitProcessInfoCache()
        {
            _processInfoCache = new ConcurrentDictionary<int, ProcessInfo>(Environment.ProcessorCount * 2, _processes.Count);

            foreach (var process in _processes)
            {
                var processInfo = ProcessInfo.Create(process.Value, _logger);
                if (processInfo != null) _processInfoCache[process.Key] = processInfo;
            }
        }

        private void UpdateProcessCache()
        {
            var rawProcesses = Process.GetProcesses();
            var currentProcesses = rawProcesses.ToDictionary(x => x.Id);
            _logger.Info($"{rawProcesses.Length} running, {_processes.Count} cached");

            var exitedPids = new List<int>();

            // update cached processes
            foreach (var process in _processes.Values)
            {
                process.Refresh();

                if (!currentProcesses.ContainsKey(process.Id))
                {
                    exitedPids.Add(process.Id);
                    // getting name of exited process leads to exception
                }
            }

            // remove exited processes from cache
            exitedPids.ForEach(x =>
                {
                    if (_processes.TryRemove(x, out _)) _logger.Info($"process (pid {x}) removed from cache");
                }
            );

            // add new processes
            _logger.Info($"{currentProcesses.Keys.Except(_processes.Keys).Count()} new processes");

            foreach (var key in currentProcesses.Keys.Except(_processes.Keys))
            {
                var process = currentProcesses[key];
                if (_processes.TryAdd(key, process))
                    _logger.Info($"process {process.ProcessName} (pid {process.Id}) added to cache");
            }
        }

        private void UpdateProcessInfoCache()
        {
            _logger.Info($"{_processes.Count} cached, {_processInfoCache.Count} info cached");

            var exited = _processInfoCache.Keys.Except(_processes.Keys).ToList();
            var newProcesses = _processes.Keys.Except(_processInfoCache.Keys).ToList();

            _logger.Info($"{exited.Count} exited, {newProcesses.Count} new");

            // remove exited processes from cache
            exited.ForEach(x => _processInfoCache.TryRemove(x, out _));

            // update cached info
            foreach (var processInfo in _processInfoCache.Values)
            {
                processInfo.Update(_processes[processInfo.ProcessId], _lastTickTime);
            }

            // add new processes
            newProcesses.ForEach(x =>
            {
                var processInfo = ProcessInfo.Create(_processes[x], _logger);
                _processInfoCache.TryAdd(x, processInfo);
            });
        }

        private void TimerTick(object state)
        {
            _logger.Info("Tick");
            try
            {
                UpdateProcessCache();
                UpdateProcessInfoCache();

                _lastTickTime = DateTime.Now;

            }
            catch (Exception e)
            {
                _logger.Error("Tick threw an exception", e);
            }
            _logger.Info("Tick done");
        }

        public IEnumerable<ProcessInfo> GetProcessInfo()
        {
            return _processInfoCache.Values;
        }
    }
}
