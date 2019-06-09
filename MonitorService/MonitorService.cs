using System.Configuration;
using System.ServiceModel;
using System.ServiceProcess;
using MonitorService.Interfaces;

namespace MonitorService
{
    public partial class MonitorService : ServiceBase
    {
        private ProcessInfoBackgroundService _processInfoBackgroundService;
        private readonly ILogger _logger;
        private WsBroadcaster _wsServer;
        private ServiceConfiguration _config;

        public MonitorService()
        {
            InitializeComponent();
            _logger = new ServiceLogger(ServiceName);
            _config = ReadConfig();
        }

        private ServiceConfiguration ReadConfig()
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var appSettings = configuration.AppSettings;
            var config = new ServiceConfiguration();

            config.WsUrl = appSettings.Settings["WsUrl"].Value;
            config.CpuUsageNotificationPercent = int.Parse(appSettings.Settings["CpuUsageNotificationPercent"].Value ?? "0");
            config.RamUsageNotificationPercent = int.Parse(appSettings.Settings["RamUsageNotificationPercent"].Value ?? "0");
            config.RamUsageNotificationBytes = int.Parse(appSettings.Settings["RamUsageNotificationBytes"].Value ?? "0");
            config.UpdateFrequency = int.Parse(appSettings.Settings["UpdateFrequency"].Value ?? "1000");

            return config;
        }

        protected override void OnStart(string[] args)
        {
            _processInfoBackgroundService = new ProcessInfoBackgroundService(_logger, _config);
            _processInfoBackgroundService.StartWatch();
            _logger.Info("Watch started");


            _wsServer = new WsBroadcaster(_logger, _config);
            _wsServer.Start();
            _processInfoBackgroundService.OnInfoUpdated += _wsServer.SendUpdatedList;
            _logger.Info("Ws Server started");
        }

        protected override void OnPause()
        {
            base.OnPause();
            _processInfoBackgroundService.StopWatch();
            _logger.Info("Service paused");
        }

        protected override void OnContinue()
        {
            base.OnContinue();
            _processInfoBackgroundService.StartWatch();
            _logger.Info("Service continued");
        }

        protected override void OnStop()
        {
            _logger.Info("Service stopping");
            _processInfoBackgroundService.StopWatch();
            _logger.Info("Service stopped");
        }
    }
}
