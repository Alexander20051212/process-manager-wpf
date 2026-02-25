using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ProcessManagerWPF.Models;
using ProcessManagerWPF.Services;
using ProcessManagerWPF.Utilities;

namespace ProcessManagerWPF.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ProcessService _service;
        private readonly DispatcherTimer _timer;

        private ProcessInfo _selectedProcess;

        public ObservableCollection<ProcessInfo> Processes { get; }
        public ObservableCollection<ThreadInfo> Threads { get; }
        public ObservableCollection<ProcessTreeNode> ProcessTree { get; }
        public ObservableCollection<CoreItem> Cores { get; }

        public ObservableCollection<ProcessPriorityClass> PriorityLevels { get; }

        public ICommand RefreshCommand { get; }
        public ICommand KillCommand { get; }
        public ICommand ChangePriorityCommand { get; }
        public ICommand ApplyAffinityCommand { get; }

        public ProcessInfo SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                _selectedProcess = value;
                OnPropertyChanged();

                if (value != null)
                {
                    LoadThreads();
                    LoadAffinity();
                }
            }
        }

        public double CurrentCpu { get; set; }
        public double CurrentRam { get; set; }

        public MainViewModel()
        {
            _service = new ProcessService();

            Processes = new ObservableCollection<ProcessInfo>();
            Threads = new ObservableCollection<ThreadInfo>();
            ProcessTree = new ObservableCollection<ProcessTreeNode>();
            Cores = new ObservableCollection<CoreItem>();

            PriorityLevels = new ObservableCollection<ProcessPriorityClass>
            {
                ProcessPriorityClass.Idle,
                ProcessPriorityClass.BelowNormal,
                ProcessPriorityClass.Normal,
                ProcessPriorityClass.AboveNormal,
                ProcessPriorityClass.High,
                ProcessPriorityClass.RealTime
            };

            for (int i = 0; i < Environment.ProcessorCount; i++)
                Cores.Add(new CoreItem { CoreIndex = i, IsSelected = true });

            RefreshCommand = new RelayCommand(_ => LoadProcesses());
            KillCommand = new RelayCommand(_ => KillProcess(), _ => SelectedProcess != null);
            ChangePriorityCommand =
                new RelayCommand(ChangePriority, _ => SelectedProcess != null);
            ApplyAffinityCommand =
                new RelayCommand(_ => ApplyAffinity(), _ => SelectedProcess != null);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            _timer.Tick += (s, e) =>
            {
                LoadProcesses();
                UpdateMonitor();
            };

            _timer.Start();

            LoadProcesses();
            LoadProcessTree();
            UpdateMonitor();
        }

        private void LoadProcesses()
        {
            Processes.Clear();

            foreach (var p in _service.GetAllProcesses())
                Processes.Add(p);
        }

        private void LoadThreads()
        {
            Threads.Clear();

            foreach (var t in _service.GetProcessThreads(SelectedProcess.Id))
                Threads.Add(t);
        }

        private void LoadProcessTree()
        {
            ProcessTree.Clear();

            foreach (var node in _service.GetProcessTree())
                ProcessTree.Add(node);
        }

        private void LoadAffinity()
        {
            try
            {
                long mask = _service
                    .GetProcessorAffinity(SelectedProcess.Id)
                    .ToInt64();

                foreach (var core in Cores)
                    core.IsSelected = (mask & (1L << core.CoreIndex)) != 0;
            }
            catch { }
        }

        private void ApplyAffinity()
        {
            long mask = 0;

            foreach (var core in Cores)
                if (core.IsSelected)
                    mask |= (1L << core.CoreIndex);

            if (mask == 0)
            {
                MessageBox.Show("Select at least one CPU core.");
                return;
            }

            _service.SetProcessorAffinity(
                SelectedProcess.Id,
                new IntPtr(mask),
                out string error);

            if (error != null)
                MessageBox.Show(error);
        }

        private void KillProcess()
        {
            if (MessageBox.Show("Terminate process?",
                "Confirm",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            _service.KillProcess(
                SelectedProcess.Id,
                out string error);

            if (error != null)
                MessageBox.Show(error);

            LoadProcesses();
        }

        private void ChangePriority(object parameter)
        {
            if (!(parameter is ProcessPriorityClass priority))
                return;

            _service.SetProcessPriority(
                SelectedProcess.Id,
                priority,
                out string error);

            if (error != null)
                MessageBox.Show(error);

            LoadProcesses();
        }

        private void UpdateMonitor()
        {
            CurrentCpu = _service.GetCpuUsage();
            CurrentRam = _service.GetUsedRam();
            OnPropertyChanged(nameof(CurrentCpu));
            OnPropertyChanged(nameof(CurrentRam));
        }
    }
}