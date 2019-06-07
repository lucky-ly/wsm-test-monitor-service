using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces;

namespace MonitorService
{
    public class ServiceLogger: ILogger
    {
        private EventLog EventLog { get; set; }

        public ServiceLogger(string serviceName)
        {
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
            EventLog.WriteEntry(message, EventLogEntryType.Information);
        }

        public void Warn(string message)
        {
            EventLog.WriteEntry(message, EventLogEntryType.Warning);
        }

        public void Error(string message, Exception e = null)
        {
            EventLog.WriteEntry($"{message}\n\n{e.Message}\n\n{e.StackTrace}", EventLogEntryType.Error);
        }
    }
}
