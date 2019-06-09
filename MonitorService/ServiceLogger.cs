using System;
using System.ComponentModel;
using System.Diagnostics;
using MonitorService.Entities;
using MonitorService.Interfaces;

namespace MonitorService
{
    public class ServiceLogger: ILogger
    {
        private readonly LogLevelEnum _logLevel;
        private EventLog EventLog { get; set; }

        public ServiceLogger(string serviceName, LogLevelEnum logLevel)
        {
            _logLevel = logLevel;
            EventLog = new EventLog
            {
                Source = serviceName,
                Log = "Application"
            };

            ((ISupportInitialize)EventLog).BeginInit();
            if (!EventLog.SourceExists(EventLog.Source))
            {
                EventLog.CreateEventSource(EventLog.Source, EventLog.Log);
            }

            ((ISupportInitialize)(EventLog)).EndInit();
        }


        public void Info(string message)
        {
            if (_logLevel < LogLevelEnum.Info) return;
            EventLog.WriteEntry(message, EventLogEntryType.Information);
        }

        public void Warn(string message)
        {
            if (_logLevel < LogLevelEnum.Warning) return;
            EventLog.WriteEntry(message, EventLogEntryType.Warning);
        }

        public void Error(string message, Exception e = null)
        {
            if (_logLevel < LogLevelEnum.Error) return;
            EventLog.WriteEntry($"{message}\n\n{e.Message}\n\n{e.StackTrace}", EventLogEntryType.Error);
        }
    }
}
