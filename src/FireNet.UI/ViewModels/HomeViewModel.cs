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
        private void Set(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

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

        // ------------------------------
        // PING
        // ------------------------------

        private string _pingText = "Ping: ---";
        public string PingText
        {
            get => _pingText;
            set
            {
                _pingText = value;
                Set(nameof(PingText));
            }
        }

        public ICommand RefreshPingCommand { get; }

        // ------------------------------
        // Profiles
        // ------------------------------

        public class ProfileItem
        {
            public string Remark { get; set; }
            public string FullLink { get; set; }

            // انتخاب شده؟
            public bool IsSelected { get; set; }

            // سایز دایره
            public double Size { get; set; }
        }

        public ObservableCollection<ProfileItem> Profiles { get; } = new();

        private ProfileItem? _selectedProfile;
        public ProfileItem? SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (_selectedProfile == value) return;
                _selectedProfile = value;
                Set(nameof(SelectedProfile));
            }
        }

        // ------------------------------
        // Error
        // ------------------------------
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
        public ICommand SelectProfileCommand => new RelayCommand(p =>
        {
            if (p is ProfileItem item)
                SelectProfile(item);
        });

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

        // -------------------------------------------------
        // Constructor
        // -------------------------------------------------
        public HomeViewModel()
        {
            _session = SessionManager.Instance;
            _api = new PanelApiClient(_session, "https://report.soft99.sbs:2053");
            _configBuilder = new XrayConfigBuilder();
            _xray = new XrayProcessManager();

            IsConnected = _xray.IsRunning;
            ConnectionStatus = IsConnected ? "Connected" : "Disconnected";

            AppVersion = $"v{Assembly.GetExecutingAssembly().GetName().Version}";

            ConnectCommand = new RelayCommand(async _ => await ConnectOrDisconnect());
            LogoutCommand = new RelayCommand(async _ => await LogoutAsync());
            OpenSettingsCommand = new RelayCommand(_ => NavigationService.NavigateToSettings());
            RefreshPingCommand = new RelayCommand(async _ => await MeasurePing());

            _xray.OnCrashed += () =>
            {
                SystemProxyManager.DisableProxy();
                IsConnected = false;
                ConnectionStatus = "Disconnected";
            };

            _ = LoadStatus();
        }


        // -------------------------------------------------
        // Load Status (API)
        // -------------------------------------------------
        private async Task LoadStatus()
        {
            try
            {
                _status = await _api.GetStatusAsync();

                // نمایش حجم
                TrafficInfo = $"{FormatBytes(_status.used_traffic)} / {FormatBytes(_status.data_limit)}";

                // روز باقی‌مانده
                var days = (DateTimeOffset
                    .FromUnixTimeSeconds(_status.expire)
                    .ToLocalTime()
                    .Date - DateTime.Now.Date).TotalDays;

                ExpireInfo = $"{Math.Max(0, (int)days)} روز باقی مانده";

                Profiles.Clear();

                foreach (var link in _status.links)
                {
                    string remark = ExtractRemark(link);

                    Profiles.Add(new ProfileItem
                    {
                        Remark = remark,
                        FullLink = link,
                        IsSelected = false,
                        Size = 45
                    });
                }

                if (Profiles.Count > 0)
                    SelectProfile(Profiles[0]);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }


        private string ExtractRemark(string raw)
        {
            try
            {
                int i = raw.IndexOf('#');
                if (i != -1)
                    return raw[(i + 1)..].Trim();
            }
            catch { }

            return "Server";
        }


        // -------------------------------------------------
        // Select Profile
        // -------------------------------------------------
        private void SelectProfile(ProfileItem item)
        {
            foreach (var p in Profiles)
            {
                p.IsSelected = false;
                p.Size = 35;
            }

            item.IsSelected = true;
            item.Size = 60;

            SelectedProfile = item;

            Set(nameof(Profiles));
        }


        // -------------------------------------------------
        // Connect / Disconnect
        // -------------------------------------------------
        private async Task ConnectOrDisconnect()
        {
            try
            {
                ErrorMessage = "";

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
                        throw new Exception("Failed to load server status");
                }

                if (_status.status != "active")
                    throw new Exception("اکانت فعال نیست");

                if (SelectedProfile == null)
                    throw new Exception("هیچ سروری انتخاب نشده");

                string cfg = _configBuilder.BuildConfig(new() { SelectedProfile.FullLink });

                _xray.Start(cfg);

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
                    catch { }
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
                ErrorMessage = "";

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
        // Measure Ping
        // -------------------------------------------------
        private async Task MeasurePing()
        {
            try
            {
                if (SelectedProfile == null)
                {
                    PingText = "Ping: ---";
                    return;
                }

                PingText = "Ping: measuring...";

                long delay = await RealDelayTester.MeasureAsync(SelectedProfile.FullLink);

                PingText = delay <= 0 ? "Ping: timeout" : $"Ping: {delay}ms";
            }
            catch
            {
                PingText = "Ping: error";
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
    }
}
