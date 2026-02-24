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
        private readonly ProcessService _processService;
        private readonly DispatcherTimer _timer;

        private string _searchText;
        private ProcessInfo _selectedProcess;

        private ObservableCollection<ProcessInfo> _allProcesses;

        public ObservableCollection<ProcessInfo> Processes { get; set; }
        public ObservableCollection<ProcessPriorityClass> PriorityLevels { get; }
        public ObservableCollection<CoreItem> Cores { get; set; }

        public ICommand RefreshCommand { get; }
        public ICommand ChangePriorityCommand { get; }
        public ICommand ApplyAffinityCommand { get; }

        public ProcessInfo SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                _selectedProcess = value;
                OnPropertyChanged();

                if (_selectedProcess != null)
                    LoadAffinity();
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
            _processService = new ProcessService();

            Processes = new ObservableCollection<ProcessInfo>();
            _allProcesses = new ObservableCollection<ProcessInfo>();

            PriorityLevels = new ObservableCollection<ProcessPriorityClass>
            {
                ProcessPriorityClass.Idle,
                ProcessPriorityClass.BelowNormal,
                ProcessPriorityClass.Normal,
                ProcessPriorityClass.AboveNormal,
                ProcessPriorityClass.High,
                ProcessPriorityClass.RealTime
            };

            Cores = new ObservableCollection<CoreItem>();
            int coreCount = Environment.ProcessorCount;

            for (int i = 0; i < coreCount; i++)
            {
                Cores.Add(new CoreItem
                {
                    CoreIndex = i,
                    IsSelected = true
                });
            }

            RefreshCommand = new RelayCommand(_ => LoadProcesses());
            ChangePriorityCommand = new RelayCommand(ChangePriority, CanChangePriority);
            ApplyAffinityCommand = new RelayCommand(_ => ApplyAffinity(), _ => SelectedProcess != null);

            LoadProcesses();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(3);
            _timer.Tick += (s, e) => LoadProcesses();
            _timer.Start();
        }

        private void LoadProcesses()
        {
            var processList = _processService.GetAllProcesses();

            _allProcesses.Clear();
            foreach (var process in processList)
                _allProcesses.Add(process);

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

            foreach (var process in filtered)
                Processes.Add(process);
        }

        private bool CanChangePriority(object parameter)
        {
            return SelectedProcess != null && parameter is ProcessPriorityClass;
        }

        private void ChangePriority(object parameter)
        {
            if (SelectedProcess == null)
                return;

            if (!(parameter is ProcessPriorityClass))
                return;

            var newPriority = (ProcessPriorityClass)parameter;

            if (newPriority == ProcessPriorityClass.RealTime)
            {
                var result = MessageBox.Show(
                    "Приоритет RealTime может нарушить работу системы. Продолжить?",
                    "Внимание",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;
            }

            var success = _processService.SetProcessPriority(
                SelectedProcess.Id,
                newPriority,
                out string errorMessage);

            if (!success)
            {
                MessageBox.Show(
                    "Ошибка изменения приоритета:\n" + errorMessage,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            LoadProcesses();
        }

        private void LoadAffinity()
        {
            try
            {
                var mask = _processService.GetProcessorAffinity(SelectedProcess.Id);
                long value = mask.ToInt64();

                foreach (var core in Cores)
                {
                    core.IsSelected = (value & (1L << core.CoreIndex)) != 0;
                }
            }
            catch
            {
            }
        }

        private void ApplyAffinity()
        {
            if (SelectedProcess == null)
                return;

            long mask = 0;

            foreach (var core in Cores)
            {
                if (core.IsSelected)
                    mask |= (1L << core.CoreIndex);
            }

            if (mask == 0)
            {
                MessageBox.Show("Необходимо выбрать хотя бы одно ядро.");
                return;
            }

            var success = _processService.SetProcessorAffinity(
                SelectedProcess.Id,
                new IntPtr(mask),
                out string error);

            if (!success)
            {
                MessageBox.Show("Ошибка установки affinity:\n" + error);
            }
        }
    }
}