using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                        Priority = p.PriorityClass,
                        MemoryUsage = p.WorkingSet64,
                        ThreadCount = p.Threads.Count,
                        CpuTime = p.TotalProcessorTime
                    });
                }
                catch (Win32Exception) { }
                catch (InvalidOperationException) { }
                catch { }
            }

            return processList;
        }

        public bool SetProcessPriority(int processId,
                                       ProcessPriorityClass priority,
                                       out string errorMessage)
        {
            try
            {
                var process = Process.GetProcessById(processId);

                if (process.HasExited)
                {
                    errorMessage = "Процесс уже завершён.";
                    return false;
                }

                process.PriorityClass = priority;

                errorMessage = null;
                return true;
            }
            catch (ArgumentException)
            {
                errorMessage = "Процесс не найден.";
                return false;
            }
            catch (Win32Exception)
            {
                errorMessage = "Недостаточно прав.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public IntPtr GetProcessorAffinity(int processId)
        {
            var process = Process.GetProcessById(processId);
            return process.ProcessorAffinity;
        }

        public bool SetProcessorAffinity(int processId,
                                         IntPtr mask,
                                         out string error)
        {
            try
            {
                var process = Process.GetProcessById(processId);

                if (process.HasExited)
                {
                    error = "Процесс завершён.";
                    return false;
                }

                process.ProcessorAffinity = mask;

                error = null;
                return true;
            }
            catch (Win32Exception)
            {
                error = "Недостаточно прав.";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public List<ThreadInfo> GetProcessThreads(int processId)
        {
            var threadList = new List<ThreadInfo>();

            try
            {
                var process = Process.GetProcessById(processId);

                foreach (ProcessThread thread in process.Threads)
                {
                    try
                    {
                        threadList.Add(new ThreadInfo
                        {
                            Id = thread.Id,
                            Priority = thread.PriorityLevel.ToString(),
                            State = thread.ThreadState.ToString(),
                            ProcessorTime = thread.TotalProcessorTime
                        });
                    }
                    catch { }
                }
            }
            catch { }

            return threadList;
        }
    }
}