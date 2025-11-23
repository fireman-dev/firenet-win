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

        private readonly PanelApiClient _api;
        private readonly SessionManager _session;

        public LoginViewModel()
        {
            _session = SessionManager.Instance;
            _api = new PanelApiClient(_session, "https://report.soft99.sbs:2053");

            LoginCommand = new RelayCommand(async _ => await Login(), _ => CanLogin);
        }

        private string _username = "";
        public string Username
        {
            get => _username;
            set
            {
                if (_username == value) return;
                _username = value;
                Set(nameof(Username));
                Set(nameof(CanLogin));
            }
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set
            {
                if (_password == value) return;
                _password = value;
                Set(nameof(Password));
                Set(nameof(CanLogin));
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                Set(nameof(IsBusy));
                Set(nameof(CanLogin));
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

        public bool CanLogin =>
            !IsBusy &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password);

        public ICommand LoginCommand { get; }

        private async Task Login()
        {
            if (!CanLogin)
                return;

            IsBusy = true;
            ErrorMessage = "";

            try
            {
                var req = new LoginRequest
                {
                    username = Username.Trim(),
                    password = Password,
                    device_id = MachineId()
                };

                await _api.LoginAsync(req);

                // اگر لاگین موفق شد، میریم صفحه Home
                NavigationService.NavigateToHome();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private string MachineId()
        {
            // دستگاه باید یک ID ثابت داشته باشد
            return Environment.MachineName + "_" + Environment.UserName;
        }
    }
}
