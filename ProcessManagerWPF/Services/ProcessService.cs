using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using ProcessManagerWPF.Models;

namespace ProcessManagerWPF.Services
{
    public class ProcessService
    {
        private readonly PerformanceCounter _cpuCounter =
            new PerformanceCounter("Processor", "% Processor Time", "_Total");

        private readonly PerformanceCounter _ramCounter =
            new PerformanceCounter("Memory", "Available MBytes");


        public float GetCpuUsage()
        {
            return _cpuCounter.NextValue();
        }

        public float GetUsedRam()
        {
            var available = _ramCounter.NextValue();

            var total = GetTotalPhysicalMemory();

            return total - available;
        }

        private float GetTotalPhysicalMemory()
        {
            var searcher = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");

            foreach (ManagementObject obj in searcher.Get())
            {
                float totalKb = Convert.ToSingle(obj["TotalVisibleMemorySize"]);
                return totalKb / 1024f;
            }

            return 0;
        }

        public List<ProcessInfo> GetAllProcesses()
        {
            var list = new List<ProcessInfo>();

            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    list.Add(new ProcessInfo
                    {
                        Id = p.Id,
                        Name = p.ProcessName,
                        Priority = p.PriorityClass,
                        MemoryUsage = p.WorkingSet64,
                        ThreadCount = p.Threads.Count,
                        CpuTime = p.TotalProcessorTime
                    });
                }
                catch { }
            }

            return list;
        }

        public bool SetProcessPriority(int pid,
                                       ProcessPriorityClass priority,
                                       out string error)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                process.PriorityClass = priority;
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool KillProcess(int pid, out string error)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                process.Kill();
                process.WaitForExit(3000);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public IntPtr GetProcessorAffinity(int pid)
        {
            return Process.GetProcessById(pid).ProcessorAffinity;
        }

        public bool SetProcessorAffinity(int pid,
                                         IntPtr mask,
                                         out string error)
        {
            try
            {
                Process.GetProcessById(pid).ProcessorAffinity = mask;
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public List<ThreadInfo> GetProcessThreads(int pid)
        {
            var list = new List<ThreadInfo>();

            try
            {
                var process = Process.GetProcessById(pid);

                foreach (ProcessThread t in process.Threads)
                {
                    try
                    {
                        list.Add(new ThreadInfo
                        {
                            Id = t.Id,
                            Priority = t.PriorityLevel.ToString(),
                            State = t.ThreadState.ToString(),
                            ProcessorTime = t.TotalProcessorTime
                        });
                    }
                    catch { }
                }
            }
            catch { }

            return list;
        }

        public List<ProcessTreeNode> GetProcessTree()
        {
            var nodes = new Dictionary<int, ProcessTreeNode>();
            var parentMap = new Dictionary<int, int>();

            foreach (var p in Process.GetProcesses())
            {
                nodes[p.Id] = new ProcessTreeNode
                {
                    Id = p.Id,
                    Name = p.ProcessName
                };
            }

            var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, ParentProcessId FROM Win32_Process");

            foreach (ManagementObject obj in searcher.Get())
            {
                int pid = Convert.ToInt32(obj["ProcessId"]);
                int parent = Convert.ToInt32(obj["ParentProcessId"]);
                parentMap[pid] = parent;
            }

            var roots = new List<ProcessTreeNode>();

            foreach (var pair in nodes)
            {
                int pid = pair.Key;
                var node = pair.Value;

                if (parentMap.ContainsKey(pid) &&
                    nodes.ContainsKey(parentMap[pid]))
                {
                    nodes[parentMap[pid]].Children.Add(node);
                }
                else
                {
                    roots.Add(node);
                }
            }

            return roots;
        }
    }
}