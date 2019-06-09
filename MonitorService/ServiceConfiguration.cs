namespace MonitorService
{
    public class ServiceConfiguration
    {
        public string WsUrl { get; set; }
        public int CpuUsageNotificationPercent { get; set; }
        public int RamUsageNotificationPercent { get; set; }
        public int RamUsageNotificationBytes { get; set; }
        public int UpdateFrequency { get; set; }
    }
}
