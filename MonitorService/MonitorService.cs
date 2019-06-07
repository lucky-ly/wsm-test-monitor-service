using System.ServiceModel;
using System.ServiceProcess;
using Core;
using Core.Contracts;
using Core.Interfaces;

namespace MonitorService
{
    public partial class MonitorService : ServiceBase
    {
        private ProcessInfoBackgroundService _processInfoBackgroundService;
        private IProcessInfoProvider _processInfoProvider;
        private readonly ILogger _logger;
        private ServiceHost _serviceHost;

        public MonitorService()
        {
            InitializeComponent();
            _logger = new ServiceLogger(ServiceName);
        }

        protected override void OnStart(string[] args)
        {
            _processInfoProvider = new ProcessInfoProviderService(_logger);
            _serviceHost = new ServiceHost(_processInfoProvider);
            _logger.Info("Service started");
            _processInfoBackgroundService = ProcessInfoBackgroundService.GetInstance(_logger);
            _processInfoBackgroundService.SetCheckPeriod(1000);
            _processInfoBackgroundService.StartWatch();
            _serviceHost.Open();
            _logger.Info("Watch started");
        }

        protected override void OnPause()
        {
            base.OnPause();
            _serviceHost.Close();
            _processInfoBackgroundService.StopWatch();
            _logger.Info("Service paused");
        }

        protected override void OnContinue()
        {
            base.OnContinue();
            _processInfoBackgroundService.StartWatch();
            _serviceHost.Open();
            _logger.Info("Service continued");
        }

        protected override void OnStop()
        {
            _logger.Info("Service stopping");
            _processInfoBackgroundService.StopWatch();
            _serviceHost.Close();
            _logger.Info("Service stopped");
        }
    }
}
