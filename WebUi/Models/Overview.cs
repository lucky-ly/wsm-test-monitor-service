using System;
using System.Collections.Generic;

namespace WebUi.Models
{
    public class Overview
    {
        public DateTime UpdateTime { get; set; }
        public IEnumerable<ProcessInfo> Processes { get; set; }
        public IEnumerable<Notification> Notifications { get; set; }
    }
}