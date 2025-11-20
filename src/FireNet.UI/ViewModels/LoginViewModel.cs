using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using FireNet.Core.Api;
using FireNet.Core.Api.Dto;
using FireNet.Core.Session;
using FireNet.UI.Navigation;

namespace FireNet.UI.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Set(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // -------------------------------
        // Bindings
        // -------------------------------
        public string Username { get; set; }
        public Func<string> GetPassword { get; set; }

        public string ErrorMessage { get; set; }
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        // -------------------------------
        // Command
        // -------------------------------
        public ICommand LoginCommand { get; }

        // -------------------------------
        // Services
        // -------------------------------
        private readonly SessionManager _session;
        private readonly PanelApiClient _api;

        public LoginViewModel()
        {
            _session = new SessionManager();
            _api = new PanelApiClient(_session, "https://report.soft99.sbs:2053");

            LoginCommand = new RelayCommand(async (_) => await LoginAsync());
        }

        // -------------------------------
        // Login Process
        // -------------------------------
        private async Task LoginAsync()
        {
            ErrorMessage = "";
            Set(nameof(HasError));
            Set(nameof(ErrorMessage));

            try
            {
                var req = new LoginRequest
                {
                    username = Username,
                    password = GetPassword(),
                    device_id = MachineId(),
                    app_version = "1.0.0"
                };

                await _api.LoginAsync(req);

                // بعد از لاگین → برو home
                NavigationService.NavigateToHome();

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Set(nameof(ErrorMessage));
                Set(nameof(HasError));
            }
        }

        private string MachineId()
        {
            // دستگاه باید یک ID ثابت داشته باشد
            return Environment.MachineName + "_" + Environment.UserName;
        }
    }
}
