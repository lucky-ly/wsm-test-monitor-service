using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Core.Contracts;
using WebUi.Models;

namespace WebUi
{
    public static class ProcessInfoCache
    {
        private static Timer _timer;
        private static readonly object Locker = new object();
        private static readonly List<ProcessInfoData> Cache = new List<ProcessInfoData>();
        private static ProcessInfoProviderClient _client;

        public static void Start()
        {
            _timer = new Timer(UpdateCache, null, 0, 1000);
        }

        public static void Stop()
        {
            if (_timer != null)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _timer.Dispose();
                _timer = null;
            }

            _client?.Close();
        }

        public delegate void CacheUpdatetEventHandler(Overview overview);
        public static event CacheUpdatetEventHandler OnCacheUpdated;

        private static void UpdateCache(object state)
        {
            // just wait until connection will be established
            if (_client == null || _client.State == CommunicationState.Faulted)
                _client = new ProcessInfoProviderClient();

            IEnumerable<ProcessInfoData> cache;
            try
            {
                cache = _client.GetProcesses();
            }
            catch (Exception e)
            {
                cache = new List<ProcessInfoData>();
            }

            var overview = new Overview
            {
                UpdateTime = DateTime.Now,
                Processes = cache.Select(x => new ProcessInfo
                {
                    Name = x.Name,
                    ProcessId = x.ProcessId,
                    CpuUsage = x.CpuUsage,
                    RamUsageBytes = x.RamUsageBytes,
                    ThreadsCount = x.ThreadsCount,
                    StartTime = x.StartTime
                }),
            };

            OnCacheUpdated?.Invoke(overview);
        }
    }
}