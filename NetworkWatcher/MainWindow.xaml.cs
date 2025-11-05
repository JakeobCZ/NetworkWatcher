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
        DispatcherTimer? refreshTimer; // nullable
        SettingsStore settings;
        NotifyIcon? notifyIcon;        // nullable

        public MainWindow()
        {
            InitializeComponent();

            SetStartup(true); // nastaví aplikaci, aby se spouštěla se systémem
            AdaptersList.DataContext = adapters;
            settings = SettingsStore.Load();
            InitializeNotifyIcon();
            LoadAdapters();
            StartTimer();
        }

        void InitializeNotifyIcon()
        {
            // vytvoření NotifyIcon
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Warning;
            notifyIcon.Text = "NetworkWatcher";

            // Zobrazit ikonu ihned po startu (větší šance, že Windows ji umístí přímo na hlavní panel)
            notifyIcon.Visible = true;

            // Kontextové menu
            var menu = new ContextMenuStrip();
            var open = new ToolStripMenuItem("Otevřít nastavení");
            open.Click += (s, e) => Dispatcher.Invoke(() => { this.Show(); this.WindowState = WindowState.Normal; this.Activate(); });
            var exit = new ToolStripMenuItem("Ukončit");
            exit.Click += (s, e) =>
            {
                // skrytí ikony a ukončení aplikace
                notifyIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            };

            menu.Items.Add(open);
            menu.Items.Add(exit);
            notifyIcon.ContextMenuStrip = menu;

            // dvojklik obnoví okno
            notifyIcon.DoubleClick += (s, e) => Dispatcher.Invoke(() => { this.Show(); this.WindowState = WindowState.Normal; this.Activate(); });

            // Volitelně: krátké oznámení, které má systém tendenci zobrazit a tím ikonu "zviditelní".
            // Pokud nechceš bublinu, smaž následující řádku.
            notifyIcon.ShowBalloonTip(3000, "NetworkWatcher", "Aplikace běží. Pokud chcete ikonu stále vidět: Nastavení → Hlavní panel → Vyber ikony.", ToolTipIcon.Info);
        }


        void LoadAdapters()
        {
            adapters.Clear();
            var nics = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        .ToArray();

            foreach (var nic in nics)
            {
                var item = new AdapterItem
                {
                    Id = nic.Id,
                    Name = string.IsNullOrWhiteSpace(nic.Name) ? nic.Description : nic.Name,
                    Type = nic.NetworkInterfaceType.ToString(),
                    IsUp = nic.OperationalStatus == OperationalStatus.Up,
                    IsIgnored = settings.IgnoredIds.Contains(nic.Id)
                };
                adapters.Add(item);
            }
        }

        void StartTimer()
        {
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromSeconds(2);
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
                {
                    item.IsUp = nic.OperationalStatus == OperationalStatus.Up;
                }
                else
                {
                    item.IsUp = false;
                }
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

            UpdateTrayIconVisibility();
        }

        void UpdateTrayIconVisibility()
        {
            var existsActiveNonIgnored = adapters.Any(a => a.IsUp && !a.IsIgnored);
            if (notifyIcon != null)
                notifyIcon.Visible = existsActiveNonIgnored;
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
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (enable)
                rk.SetValue(appName, $"\"{exePath}\"");
            else
                rk.DeleteValue(appName, false);
        }
    }
}
