using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
        private string _searchText;

        private ObservableCollection<ProcessInfo> _allProcesses;

        public ObservableCollection<ProcessInfo> Processes { get; }
        public ObservableCollection<ThreadInfo> Threads { get; }
        public ObservableCollection<CoreItem> Cores { get; }
        public ObservableCollection<ProcessTreeNode> ProcessTree { get; }

        public ObservableCollection<ProcessPriorityClass> PriorityLevels { get; }

        public ICommand RefreshCommand { get; }
        public ICommand ChangePriorityCommand { get; }
        public ICommand ApplyAffinityCommand { get; }
        public ICommand KillProcessCommand { get; }

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

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public MainViewModel()
        {
            _service = new ProcessService();

            Processes = new ObservableCollection<ProcessInfo>();
            Threads = new ObservableCollection<ThreadInfo>();
            ProcessTree = new ObservableCollection<ProcessTreeNode>();
            _allProcesses = new ObservableCollection<ProcessInfo>();

            Cores = new ObservableCollection<CoreItem>();

            for (int i = 0; i < Environment.ProcessorCount; i++)
                Cores.Add(new CoreItem { CoreIndex = i, IsSelected = true });

            PriorityLevels = new ObservableCollection<ProcessPriorityClass>
            {
                ProcessPriorityClass.Idle,
                ProcessPriorityClass.BelowNormal,
                ProcessPriorityClass.Normal,
                ProcessPriorityClass.AboveNormal,
                ProcessPriorityClass.High,
                ProcessPriorityClass.RealTime
            };

            RefreshCommand = new RelayCommand(_ => RefreshAll());
            ChangePriorityCommand =
                new RelayCommand(ChangePriority, _ => SelectedProcess != null);
            ApplyAffinityCommand =
                new RelayCommand(_ => ApplyAffinity(), _ => SelectedProcess != null);
            KillProcessCommand =
                new RelayCommand(_ => KillProcess(), _ => SelectedProcess != null);

            RefreshAll();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };

            _timer.Tick += (s, e) => RefreshAll();
            _timer.Start();
        }

        private void RefreshAll()
        {
            LoadProcesses();
            LoadProcessTree();
        }

        private void LoadProcesses()
        {
            var list = _service.GetAllProcesses();

            _allProcesses.Clear();
            foreach (var p in list)
                _allProcesses.Add(p);

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            Processes.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allProcesses
                : new ObservableCollection<ProcessInfo>(
                    _allProcesses.Where(p =>
                        p.Name.ToLower().Contains(SearchText.ToLower())));

            foreach (var p in filtered)
                Processes.Add(p);
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

            RefreshAll();
        }

        private void LoadThreads()
        {
            Threads.Clear();

            foreach (var t in
                     _service.GetProcessThreads(SelectedProcess.Id))
                Threads.Add(t);
        }

        private void LoadAffinity()
        {
            try
            {
                long mask =
                    _service.GetProcessorAffinity(
                        SelectedProcess.Id).ToInt64();

                foreach (var core in Cores)
                    core.IsSelected =
                        (mask & (1L << core.CoreIndex)) != 0;
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
            var result = MessageBox.Show(
                $"Terminate {SelectedProcess.Name} ?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            _service.KillProcess(
                SelectedProcess.Id,
                out string error);

            if (error != null)
                MessageBox.Show(error);

            RefreshAll();
        }

        private void LoadProcessTree()
        {
            ProcessTree.Clear();

            foreach (var node in _service.GetProcessTree())
                ProcessTree.Add(node);
        }
    }
}