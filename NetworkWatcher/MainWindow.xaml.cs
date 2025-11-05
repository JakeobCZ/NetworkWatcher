using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Forms; // NotifyIcon
using System.Drawing; // Icon
using WinFormsApp = System.Windows.Forms;
using Microsoft.Win32;

namespace NetworkWatcher
{
    public partial class MainWindow : Window
    {
        ObservableCollection<AdapterItem> adapters = new ObservableCollection<AdapterItem>();
        DispatcherTimer? refreshTimer;
        SettingsStore settings;
        NotifyIcon? notifyIcon;

        public MainWindow()
        {
            InitializeComponent();

            settings = SettingsStore.Load();
            adapters = new ObservableCollection<AdapterItem>();
            InitializeNotifyIcon();
            LoadAdapters();
            StartTimer();
            SetStartup(true);
            AdaptersList.DataContext = adapters;

            // Minimalizace do tray při startu
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Hide();
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Text = "NetworkWatcher";
            notifyIcon.Visible = true;
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "icon_green.ico");
            notifyIcon.Icon = new Icon(iconPath);

            var menu = new ContextMenuStrip();
            var open = new ToolStripMenuItem("Settings");
            open.Click += (s, e) => RestoreFromTray();
            var exit = new ToolStripMenuItem("Close");
            exit.Click += (s, e) =>
            {
                notifyIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            };
            menu.Items.Add(open);
            menu.Items.Add(exit);
            notifyIcon.ContextMenuStrip = menu;

            notifyIcon.DoubleClick += (s, e) => RestoreFromTray();
        }

        private void RestoreFromTray()
        {
            Dispatcher.Invoke(() =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = true;
                this.Activate();
            });
        }

        private void UpdateTrayIcon(bool everythingOk)
        {
            if (notifyIcon == null) return;
            string iconFile = everythingOk ? "icon_red.ico" : "icon_green.ico";
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", iconFile);
            notifyIcon.Icon = new Icon(iconPath);
        }

        void LoadAdapters()
        {
            adapters.Clear();
            var nics = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var nic in nics)
            {
                adapters.Add(new AdapterItem
                {
                    Id = nic.Id,
                    Name = string.IsNullOrWhiteSpace(nic.Name) ? nic.Description : nic.Name,
                    Type = nic.NetworkInterfaceType.ToString(),
                    IsUp = nic.OperationalStatus == OperationalStatus.Up,
                    IsIgnored = settings.IgnoredIds.Contains(nic.Id)
                });
            }
        }

        void StartTimer()
        {
            refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }

        void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            var current = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToDictionary(n => n.Id);

            foreach (var item in adapters.ToList())
            {
                if (current.TryGetValue(item.Id, out var nic))
                    item.IsUp = nic.OperationalStatus == OperationalStatus.Up;
                else
                    item.IsUp = false;
            }

            foreach (var kv in current)
            {
                if (!adapters.Any(a => a.Id == kv.Key))
                {
                    var nic = kv.Value;
                    adapters.Add(new AdapterItem
                    {
                        Id = nic.Id,
                        Name = string.IsNullOrWhiteSpace(nic.Name) ? nic.Description : nic.Name,
                        Type = nic.NetworkInterfaceType.ToString(),
                        IsUp = nic.OperationalStatus == OperationalStatus.Up,
                        IsIgnored = settings.IgnoredIds.Contains(nic.Id)
                    });
                }
            }

            bool allOk = adapters.Any(a => a.IsUp && !a.IsIgnored);
            UpdateTrayIcon(allOk);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            settings.IgnoredIds = adapters.Where(a => a.IsIgnored).Select(a => a.Id).ToList();
            settings.Save();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void SetStartup(bool enable)
        {
            string appName = "NetworkWatcher";
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
            using RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (enable)
                rk.SetValue(appName, $"\"{exePath}\"");
            else
                rk.DeleteValue(appName, false);
        }
    }
}
