using System;
using System.Diagnostics;

namespace MonitorService
{
    internal static class ProcessUtils
    {
        public static double GetCpuUsageFast(this Process process, DateTime lastCheckTime, TimeSpan lastTotalProcessorTime)
        {
            var checkPeriod = DateTime.Now.Subtract(lastCheckTime).TotalMilliseconds;
            if (Math.Abs(checkPeriod) < 1) return 0;
            
            var processorTimeDiff = process.TotalProcessorTime.TotalMilliseconds - lastTotalProcessorTime.TotalMilliseconds;
            var result = processorTimeDiff / checkPeriod / Convert.ToDouble(Environment.ProcessorCount);
            return result;
        }
    }
}
