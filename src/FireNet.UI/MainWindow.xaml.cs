using System.Windows;
using System.Windows.Input;
using FireNet.UI.Navigation;
using FireNet.Core.Session;

namespace FireNet.UI
{
    public partial class MainWindow : Window
    {
        private readonly SessionManager _session = new SessionManager();

        public MainWindow()
        {
            InitializeComponent();

            NavigationService.SetFrame(MainFrame);

            if (_session.IsLoggedIn)
                NavigationService.NavigateToHome();
            else
                NavigationService.NavigateToLogin();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                e.Handled = true;
                return;
            }

            base.OnPreviewKeyDown(e);
        }
    }
}
