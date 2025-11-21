using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using FireNet.Core.Api;
using FireNet.Core.Api.Dto;
using FireNet.Core.Config;
using FireNet.Core.Session;
using FireNet.Core.Xray;
using FireNet.UI.Navigation;

namespace FireNet.UI.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Set(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // -------------------------------------------------
        // Bindings
        // -------------------------------------------------

        private string _connectionStatus = "Disconnected";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            private set
            {
                if (_connectionStatus == value) return;
                _connectionStatus = value;
                Set(nameof(ConnectionStatus));
            }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected == value) return;
                _isConnected = value;
                Set(nameof(IsConnected));
                Set(nameof(ConnectButtonText));
            }
        }

        public string ConnectButtonText => IsConnected ? "Disconnect" : "Connect";

        private string? _trafficInfo;
        public string? TrafficInfo
        {
            get => _trafficInfo;
            private set
            {
                if (_trafficInfo == value) return;
                _trafficInfo = value;
                Set(nameof(TrafficInfo));
            }
        }

        private string? _expireInfo;
        public string? ExpireInfo
        {
            get => _expireInfo;
            private set
            {
                if (_expireInfo == value) return;
                _expireInfo = value;
                Set(nameof(ExpireInfo));
            }
        }

        public ObservableCollection<string> Profiles { get; } = new();

        private string? _selectedProfile;
        public string? SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (_selectedProfile == value) return;
                _selectedProfile = value;
                Set(nameof(SelectedProfile));
            }
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage == value) return;
                _errorMessage = value;
                Set(nameof(ErrorMessage));
                Set(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public string AppVersion { get; }

        // -------------------------------------------------
        // Commands
        // -------------------------------------------------
        public ICommand ConnectCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand OpenSettingsCommand { get; }

        // -------------------------------------------------
        // Services
        // -------------------------------------------------
        private readonly SessionManager _session;
        private readonly PanelApiClient _api;
        private readonly XrayConfigBuilder _configBuilder;
        private readonly XrayProcessManager _xray;

        private StatusResponse? _status;

        private const string SocksHost = "127.0.0.1";
        private const int SocksPort = 10808;

        public HomeViewModel()
        {
            _session = new SessionManager();

            _api = new PanelApiClient(_session, "https://report.soft99.sbs:2053");
            _configBuilder = new XrayConfigBuilder();
            _xray = new XrayProcessManager();

            IsConnected = _xray.IsRunning;
            ConnectionStatus = IsConnected ? "Connected" : "Disconnected";

            AppVersion = $"v{Assembly.GetExecutingAssembly().GetName().Version}";

            ConnectCommand = new RelayCommand(async _ => await ConnectOrDisconnect());
            LogoutCommand = new RelayCommand(async _ => await LogoutAsync());
            OpenSettingsCommand = new RelayCommand(_ => NavigationService.NavigateToSettings());

            _xray.OnCrashed += () =>
            {
                SystemProxyManager.DisableProxy();
                IsConnected = false;
                ConnectionStatus = "Disconnected";
            };

            // تغییر مهم: LoadStatus دیگر async void نیست → باید fire-and-forget صدا زده شود
            _ = LoadStatus();
        }

        // -------------------------------------------------
        // Load /api/status
        // -------------------------------------------------
        private async Task LoadStatus()
        {
            try
            {
                _status = await _api.GetStatusAsync();

                TrafficInfo =
                    $"Used: {FormatBytes(_status.used_traffic)} / {FormatBytes(_status.data_limit)}";
                ExpireInfo = $"Expire: {UnixToDate(_status.expire)}";

                Profiles.Clear();
                foreach (var link in _status.links)
                    Profiles.Add(link);

                if (Profiles.Count > 0)
                    SelectedProfile = Profiles[0];
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        // -------------------------------------------------
        // Connect / Disconnect + System Proxy
        // -------------------------------------------------
        private async Task ConnectOrDisconnect()
        {
            try
            {
                ErrorMessage = string.Empty;

                if (_xray.IsRunning)
                {
                    SystemProxyManager.DisableProxy();

                    _xray.Stop();
                    IsConnected = false;
                    ConnectionStatus = "Disconnected";
                    return;
                }

                if (_status == null)
                {
                    await LoadStatus();
                    if (_status == null)
                        throw new Exception("No status data");
                }

                if (_status.status != "active")
                    throw new Exception("Account is not active");

                if (string.IsNullOrWhiteSpace(SelectedProfile))
                    throw new Exception("No server selected");

                var configPath = _configBuilder.BuildConfig(new() { SelectedProfile! });

                _xray.Start(configPath);

                SystemProxyManager.EnableSocksProxy(SocksHost, SocksPort);

                IsConnected = true;
                ConnectionStatus = "Connected";

                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (_xray.IsRunning)
                        {
                            await _api.KeepAliveAsync();
                            await Task.Delay(30_000);
                        }
                    }
                    catch
                    {
                    }
                });
            }
            catch (Exception ex)
            {
                SystemProxyManager.DisableProxy();
                IsConnected = false;
                ConnectionStatus = "Disconnected";

                ErrorMessage = ex.Message;
            }
        }

        // -------------------------------------------------
        // Logout
        // -------------------------------------------------
        private async Task LogoutAsync()
        {
            try
            {
                ErrorMessage = string.Empty;

                if (_xray.IsRunning)
                {
                    SystemProxyManager.DisableProxy();
                    _xray.Stop();
                }

                await _api.LogoutAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return;
            }

            NavigationService.NavigateToLogin();
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