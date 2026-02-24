using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProcessManagerWPF.Models;

namespace ProcessManagerWPF.Services
{
    public class ProcessService
    {
        public List<ProcessInfo> GetAllProcesses()
        {
            var processList = new List<ProcessInfo>();
            var processes = Process.GetProcesses();

            foreach (var p in processes)
            {
                try
                {
                    processList.Add(new ProcessInfo
                    {
                        Id = p.Id,
                        Name = p.ProcessName,
                        Priority = p.PriorityClass.ToString(),
                        MemoryUsage = p.WorkingSet64,
                        ThreadCount = p.Threads.Count,
                        CpuTime = p.TotalProcessorTime
                    });
                }
                catch
                {
                    // Некоторые системные процессы недоступны
                }
            }

            return processList;
        }
    }
}