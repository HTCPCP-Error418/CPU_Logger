using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Collections.Generic;

namespace CpuLoggerService
{
    public partial class CpuLoggerService : ServiceBase
    {
        private Timer sampleTimer;
        private readonly string logDir = @"C:\Logs\CpuSpikes";
        private readonly TimeSpan interval = TimeSpan.FromSeconds(10);
        private readonly double cpuThreshold = 20.0; // %
        private readonly int retentionDays = 5;

        private readonly Dictionary<int, TimeSpan> lastCpuTimes = new();
        private readonly Dictionary<int, DateTime> lastSampleTimes = new();

        public CpuLoggerService()
        {
            InitializeComponent();
            ServiceName = "CpuLoggerService";
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Directory.CreateDirectory(logDir);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Failed to create log directory: {ex.Message}", EventLogEntryType.Error);
                Stop();
                return;
            }

            sampleTimer = new Timer(LogCpuUsage, null, TimeSpan.Zero, interval);
        }

        protected override void OnStop()
        {
            sampleTimer?.Dispose();
        }

        private void LogCpuUsage(object state)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string todayLog = Path.Combine(logDir, $"cpu_log_{DateTime.Now:yyyy-MM-dd}.csv");

            try
            {
                if (!File.Exists(todayLog))
                {
                    File.WriteAllText(todayLog, "Timestamp,ProcessName,Id,CPU(%),WorkingSet(MB),Path\n");
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Failed to write log header: {ex.Message}", EventLogEntryType.Warning);
                return;
            }

            var now = DateTime.UtcNow;
            var currentProcessIds = new HashSet<int>();

            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    var currentCpuTime = proc.TotalProcessorTime;
                    var currentPid = proc.Id;
                    var currentWorkingSet = proc.WorkingSet64 / 1_000_000.0;
                    string path = "N/A";
                    try { path = proc.MainModule?.FileName ?? "N/A"; } catch { }

                    currentProcessIds.Add(currentPid);

                    if (lastCpuTimes.TryGetValue(currentPid, out TimeSpan lastCpu) &&
                        lastSampleTimes.TryGetValue(currentPid, out DateTime lastTime))
                    {
                        var cpuDelta = (currentCpuTime - lastCpu).TotalMilliseconds;
                        var timeDelta = (now - lastTime).TotalMilliseconds;

                        if (timeDelta > 0)
                        {
                            var cpuUsage = (cpuDelta / (timeDelta * Environment.ProcessorCount)) * 100.0;
                            if (cpuUsage >= cpuThreshold)
                            {
                                var line = $"\"{timestamp}\",\"{proc.ProcessName}\",{currentPid},{cpuUsage:F2},{currentWorkingSet:F2},\"{path}\"\n";
                                try
                                {
                                    File.AppendAllText(todayLog, line);
                                }
                                catch (Exception ex)
                                {
                                    EventLog.WriteEntry($"Failed to append log line: {ex.Message}", EventLogEntryType.Warning);
                                }
                            }
                        }
                    }

                    lastCpuTimes[currentPid] = currentCpuTime;
                    lastSampleTimes[currentPid] = now;
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry($"Process sampling error: {ex.Message}", EventLogEntryType.Warning);
                    continue;
                }
            }

            // Clean up data for exited processes
            var exitedPids = lastCpuTimes.Keys.Except(currentProcessIds).ToList();
            foreach (var pid in exitedPids)
            {
                lastCpuTimes.Remove(pid);
                lastSampleTimes.Remove(pid);
            }

            CleanupOldLogs();
        }

        private void CleanupOldLogs()
        {
            try
            {
                foreach (var file in Directory.GetFiles(logDir, "cpu_log_*.csv"))
                {
                    if (File.GetCreationTime(file) < DateTime.Now.AddDays(-retentionDays))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Log cleanup error: {ex.Message}", EventLogEntryType.Warning);
            }
        }
    }
}