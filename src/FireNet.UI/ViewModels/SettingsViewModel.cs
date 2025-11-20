using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using FireNet.Core.Api;
using FireNet.Core.Api.Dto;
using FireNet.Core.Session;
using FireNet.Core.Xray;
using FireNet.UI.Navigation;

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
        public string VersionInfo { get; set; } = "Version 1.0.0";

        public string FcmToken { get; set; }

        public string LogsText { get; set; } = "";

        // -----------------------------
        // Commands
        // -----------------------------
        public ICommand SaveFcmCommand { get; }
        public ICommand LogoutCommand { get; }

        // -----------------------------
        // Services
        // -----------------------------
        private readonly PanelApiClient _api;
        private readonly SessionManager _session;
        private readonly XrayProcessManager _xray;

        public SettingsViewModel()
        {
            _session = new SessionManager();
            _api = new PanelApiClient(_session, "https://your-panel.com");
            _xray = new XrayProcessManager();

            SaveFcmCommand = new RelayCommand(async (_) => await SaveFcm());
            LogoutCommand = new RelayCommand(async (_) => await Logout());

            HookLogs();
        }

        // --------------------------------------------------
        // نمایش لاگ‌های Xray پس‌زمینه
        // --------------------------------------------------
        private void HookLogs()
        {
            _xray.OnLog += (line) =>
            {
                LogsText += line + "\n";
                Set(nameof(LogsText));
            };
        }

        // --------------------------------------------------
        // ذخیره FCM Token
        // --------------------------------------------------
        private async Task SaveFcm()
        {
            if (string.IsNullOrWhiteSpace(FcmToken))
                return;

            var req = new UpdateFcmTokenRequest
            {
                fcm_token = FcmToken
            };

            await _api.UpdateFcmTokenAsync(req);
        }

        // --------------------------------------------------
        // Logout کامل
        // --------------------------------------------------
        private async Task Logout()
        {
            await _api.LogoutAsync();

            NavigationService.NavigateToLogin();
        }
    }
}
