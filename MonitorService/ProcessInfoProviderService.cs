using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Core.Contracts;
using Core.Interfaces;

namespace MonitorService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal class ProcessInfoProviderService : IProcessInfoProvider
    {
        private readonly ILogger _logger;
        private readonly ProcessInfoBackgroundService _backgroundService;

        public ProcessInfoProviderService(ILogger logger)
        {
            _logger = logger;
            _backgroundService = ProcessInfoBackgroundService.GetInstance(_logger);
        }

        public IEnumerable<ProcessInfoData> GetProcesses()
        {
            var processInfo = _backgroundService.GetProcessInfo();
            var result = processInfo.Select(x => new ProcessInfoData
            {
                Name = x.Name,
                ProcessId = x.ProcessId,
                CpuUsage = x.CpuUsage,
                RamUsageBytes = x.RamUsageBytes,
                ThreadsCount = x.ThreadsCount,
                StartTime = x.StartTime,
            });
            return result;
        }
    }
}
