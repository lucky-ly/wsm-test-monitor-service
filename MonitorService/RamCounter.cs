using System;
using System.Linq;
using System.Management;

namespace MonitorService
{
    public class RamCounter
    {
        private readonly ManagementObjectSearcher _ramWmiSearcher;

        public RamCounter()
        {
            _ramWmiSearcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
        }

        private class MemoryValues
        {
            public double FreePhysicalMemory { get; set; }
            public double TotalVisibleMemorySize { get; set; }
        }

        private MemoryValues GetMemoryValues()
        {
            var memoryValues = _ramWmiSearcher.Get()
                .Cast<ManagementObject>()
                .Select(mo => new MemoryValues {
                    FreePhysicalMemory = double.Parse(mo["FreePhysicalMemory"].ToString()),
                    TotalVisibleMemorySize = double.Parse(mo["TotalVisibleMemorySize"].ToString())
                })
                .FirstOrDefault();

            return memoryValues;
        }

        public long GetRamUsageBytes()
        {
            var memoryValues = GetMemoryValues();

            if (memoryValues == null) return 0;

            var result = (long)Math.Round(memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory);
            return result;
        }

        public double GetRamUsagePercentage()
        {
            var memoryValues = GetMemoryValues();

            if (memoryValues == null) return 0;

            var percent = ((memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory) / memoryValues.TotalVisibleMemorySize) * 100;
            return percent;
        }
    }
}
