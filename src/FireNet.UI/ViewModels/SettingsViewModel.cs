using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using FireNet.Core.Api;
using FireNet.Core.Api.Dto;
using FireNet.Core.Session;
using FireNet.UI.Navigation;
using FireNet.UI.Theme;

namespace FireNet.UI.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Set(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // -----------------------------
        // Bindings
        // -----------------------------
        public string VersionInfo { get; set; } =
            $"Version {Assembly.GetExecutingAssembly().GetName().Version}";

        private string _fcmToken;
        public string FcmToken
        {
            get => _fcmToken;
            set
            {
                _fcmToken = value;
                Set(nameof(FcmToken));
            }
        }

        // -------- THEME ----------
        public string[] ThemeOptions { get; } = { "Light", "Dark" };

        private string _selectedTheme = "Light";
        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                _selectedTheme = value;
                Set(nameof(SelectedTheme));
            }
        }

        // -----------------------------
        // Commands
        // -----------------------------
        public ICommand SaveFcmCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ApplyThemeCommand { get; }

        // -----------------------------
        // Services
        // -----------------------------
        private readonly PanelApiClient _api;
        private readonly SessionManager _session;

        public SettingsViewModel()
        {
            _session = SessionManager.Instance;
            _api = new PanelApiClient(_session, "https://report.soft99.sbs:2053");

            SaveFcmCommand = new RelayCommand(async _ => await SaveFcm());
            LogoutCommand = new RelayCommand(async _ => await Logout());
            ApplyThemeCommand = new RelayCommand(_ => ApplyTheme());
        }

        // --------------------------------------------------
        // ذخیره FCM Token
        // --------------------------------------------------
        private async Task SaveFcm()
        {
            if (string.IsNullOrWhiteSpace(FcmToken))
                return;

            var req = new UpdateFcmTokenRequest { fcm_token = FcmToken };
            await _api.UpdateFcmTokenAsync(req);
        }

        // --------------------------------------------------
        // Theme switching
        // --------------------------------------------------
        private void ApplyTheme()
        {
            ThemeManager.Apply(SelectedTheme);
        }

        // --------------------------------------------------
        // Logout
        // --------------------------------------------------
        private async Task Logout()
        {
            await _api.LogoutAsync();
            NavigationService.NavigateToLogin();
        }
    }
}
