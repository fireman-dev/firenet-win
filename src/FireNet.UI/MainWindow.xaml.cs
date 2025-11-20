using System.Windows;
using FireNet.UI.Navigation;
using FireNet.UI.Views;
using FireNet.Core.Session;

namespace FireNet.UI
{
    public partial class MainWindow : Window
    {
        private readonly SessionManager _session;

        public MainWindow()
        {
            InitializeComponent();

            NavigationService.MainFrame = RootFrame;
            _session = new SessionManager();

            // اگر از قبل توکن داشت → برو Home
            if (_session.IsLoggedIn())
                NavigationService.NavigateToHome();
            else
                NavigationService.NavigateToLogin();
        }
    }
}
