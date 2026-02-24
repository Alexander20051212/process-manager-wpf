using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using ProcessManagerWPF.Models;
using ProcessManagerWPF.Services;
using System.Windows.Input;
using ProcessManagerWPF.Utilities;

namespace ProcessManagerWPF.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ProcessService _processService;
        private readonly DispatcherTimer _timer;

        private string _searchText;

        public ObservableCollection<ProcessInfo> Processes { get; set; }
        private ObservableCollection<ProcessInfo> _allProcesses;

        public ICommand RefreshCommand { get; }

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

            RefreshCommand = new RelayCommand(_ => LoadProcesses());

            LoadProcesses();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
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
    }
}