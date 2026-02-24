using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProcessManagerWPF.Models;
using System.ComponentModel;

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
                catch (Win32Exception)
                {
                    // Нет доступа
                }
                catch (InvalidOperationException)
                {
                    // Процесс завершился
                }
                catch
                {
                }
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
                errorMessage = "Недостаточно прав для изменения приоритета.";
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
                    error = "Процесс уже завершён.";
                    return false;
                }

                process.ProcessorAffinity = mask;

                error = null;
                return true;
            }
            catch (Win32Exception)
            {
                error = "Недостаточно прав для изменения affinity.";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}