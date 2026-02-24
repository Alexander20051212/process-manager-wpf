using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using ProcessManagerWPF.Models;
using ProcessManagerWPF.Services;

namespace ProcessManagerWPF.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ProcessService _processService;
        private readonly DispatcherTimer _timer;

        public ObservableCollection<ProcessInfo> Processes { get; set; }

        public MainViewModel()
        {
            _processService = new ProcessService();
            Processes = new ObservableCollection<ProcessInfo>();

            LoadProcesses();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(3);
            _timer.Tick += (s, e) => LoadProcesses();
            _timer.Start();
        }

        private void LoadProcesses()
        {
            var processList = _processService.GetAllProcesses();

            Processes.Clear();
            foreach (var process in processList)
            {
                Processes.Add(process);
            }
        }
    }
}