using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using FireNet.Core.Api;
using FireNet.Core.Api.Dto;
using FireNet.Core.Config;
using FireNet.Core.Session;
using FireNet.Core.Xray;

namespace FireNet.UI.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Set(string name) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // -------------------------------------------------
        // Bindings
        // -------------------------------------------------
        public string ConnectionStatus { get; set; } = "Disconnected";
        public string ConnectButtonText => IsConnected ? "Disconnect" : "Connect";
        public string TrafficInfo { get; set; }
        public string ExpireInfo { get; set; }

        public ObservableCollection<string> Profiles { get; set; }
            = new ObservableCollection<string>();

        public string SelectedProfile { get; set; }

        public string ErrorMessage { get; set; }
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        private bool IsConnected => _xray.IsRunning();

        // -------------------------------------------------
        // Commands
        // -------------------------------------------------
        public ICommand ConnectCommand { get; }

        // -------------------------------------------------
        // Services
        // -------------------------------------------------
        private readonly SessionManager _session;
        private readonly PanelApiClient _api;
        private readonly XrayConfigBuilder _configBuilder;
        private readonly XrayProcessManager _xray;

        private StatusResponse _status;

        public HomeViewModel()
        {
            _session = new SessionManager();
            _api = new PanelApiClient(_session, "https://your-panel.com");
            _configBuilder = new XrayConfigBuilder();
            _xray = new XrayProcessManager();

            ConnectCommand = new RelayCommand(async (_) => await ConnectOrDisconnect());

            LoadStatus();
        }

        // -------------------------------------------------
        // Load /api/status
        // -------------------------------------------------
        private async void LoadStatus()
        {
            try
            {
                _status = await _api.GetStatusAsync();

                TrafficInfo = $"Used: {FormatBytes(_status.used_traffic)} / {FormatBytes(_status.data_limit)}";
                ExpireInfo = $"Expire: {UnixToDate(_status.expire)}";

                Profiles.Clear();
                foreach (var l in _status.links)
                    Profiles.Add(l);

                if (Profiles.Count > 0)
                    SelectedProfile = Profiles[0];

                Set(nameof(TrafficInfo));
                Set(nameof(ExpireInfo));
                Set(nameof(Profiles));
                Set(nameof(SelectedProfile));
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Set(nameof(ErrorMessage));
                Set(nameof(HasError));
            }
        }

        // -------------------------------------------------
        // Connect / Disconnect
        // -------------------------------------------------
        private async Task ConnectOrDisconnect()
        {
            if (_xray.IsRunning())
            {
                _xray.Stop();
                ConnectionStatus = "Disconnected";
                Set(nameof(ConnectionStatus));
                Set(nameof(ConnectButtonText));
                return;
            }

            try
            {
                ErrorMessage = "";
                Set(nameof(HasError));
                Set(nameof(ErrorMessage));

                if (SelectedProfile == null)
                    throw new Exception("No server selected");

                var configPath = _configBuilder.BuildConfig(new() { SelectedProfile });

                bool ok = _xray.Start(configPath);
                if (!ok)
                    throw new Exception("Failed to start Xray");

                ConnectionStatus = "Connected";
                Set(nameof(ConnectionStatus));
                Set(nameof(ConnectButtonText));

                // KeepAlive background
                _ = Task.Run(async () =>
                {
                    while (_xray.IsRunning())
                    {
                        await _api.KeepAliveAsync();
                        await Task.Delay(30_000);
                    }
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Set(nameof(ErrorMessage));
                Set(nameof(HasError));
            }
        }

        // -------------------------------------------------
        // Helpers
        // -------------------------------------------------
        private string FormatBytes(long b)
        {
            double kb = b / 1024.0;
            double mb = kb / 1024.0;
            double gb = mb / 1024.0;

            if (gb >= 1) return $"{gb:F2} GB";
            if (mb >= 1) return $"{mb:F2} MB";
            if (kb >= 1) return $"{kb:F2} KB";
            return $"{b} B";
        }

        private string UnixToDate(long ts)
        {
            return DateTimeOffset.FromUnixTimeSeconds(ts)
                                 .ToLocalTime()
                                 .ToString("yyyy/MM/dd");
        }
    }
}
