using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProcessManagerWPF.Models;
using System.ComponentModel;

namespace ProcessManagerWPF.Services
{
    public class ProcessService
    {
        /// <summary>
        /// Получение списка всех процессов
        /// </summary>
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
                    // Недостаточно прав доступа — пропускаем процесс
                }
                catch (InvalidOperationException)
                {
                    // Процесс завершился во время чтения
                }
                catch
                {
                    // Любые другие исключения — игнорируем
                }
            }

            return processList;
        }

        /// <summary>
        /// Изменение приоритета процесса
        /// </summary>
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
                errorMessage = "Процесс с указанным ID не найден.";
                return false;
            }
            catch (Win32Exception)
            {
                errorMessage = "Недостаточно прав для изменения приоритета.";
                return false;
            }
            catch (InvalidOperationException)
            {
                errorMessage = "Процесс завершился во время операции.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Неизвестная ошибка: {ex.Message}";
                return false;
            }
        }
    }
}