using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ProcessManagerWPF.Services;

namespace ProcessManagerWPF
{
    public partial class MainWindow : Window
    {
        private readonly ProcessService _processService;
        private readonly DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            _processService = new ProcessService();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(3);
            _timer.Tick += (s, e) => LoadProcesses();
            _timer.Start();

            LoadProcesses();
        }

        private void LoadProcesses()
        {
            ProcessDataGrid.ItemsSource = _processService.GetAllProcesses();
        }
    }
}