using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Core.Entities
{
    public class ProcessUtils
    {
        public static string GetInstanceNameForProcessId(int processId)
        {
            var process = Process.GetProcessById(processId);
            var processName = Path.GetFileNameWithoutExtension(process.ProcessName);

            var cat = new PerformanceCounterCategory("Process");
            var instances = cat.GetInstanceNames()
                .Where(inst => inst.StartsWith(processName))
                .ToArray();

            foreach (var instance in instances)
            {
                using (var cnt = new PerformanceCounter("Process",
                    "ID Process", instance, true))
                {
                    var val = (int)cnt.RawValue;
                    if (val == processId)
                    {
                        return instance;
                    }
                }
            }

            return null;
        }

        public static double GetCpuUsageFast(Process process, DateTime lastCheckTime, TimeSpan lastTotalProcessorTime)
        {
            var checkPeriod = DateTime.Now.Subtract(lastCheckTime).TotalMilliseconds;
            if (Math.Abs(checkPeriod) < 1) return 0;
            
            var processorTimeDiff = process.TotalProcessorTime.TotalMilliseconds - lastTotalProcessorTime.TotalMilliseconds;
            var result = processorTimeDiff / checkPeriod / Convert.ToDouble(Environment.ProcessorCount);
            return result;
        }
    }
}
