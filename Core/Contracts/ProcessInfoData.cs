using System;
using System.Runtime.Serialization;

namespace Core.Contracts
{
    [DataContract]
    public class ProcessInfoData
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public double CpuUsage { get; set; }
        [DataMember]
        public long RamUsageBytes { get; set; }
        [DataMember]
        public int ProcessId { get; set; }
        [DataMember]
        public int ThreadsCount { get; set; }
        [DataMember]
        public DateTime StartTime { get; set; }
    }
}
