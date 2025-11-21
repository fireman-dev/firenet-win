using System.Windows;
using System.Windows.Input;
using FireNet.UI.Navigation;
using FireNet.Core.Session;

namespace FireNet.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            NavigationService.SetFrame(MainFrame);

            // هنگام باز شدن برنامه، اگر لاگین است → Home
            if (SessionManager.Instance.IsLoggedIn)
                NavigationService.NavigateToHome();
            else
                NavigationService.NavigateToLogin();
        }

        // جلوگیری از Backspace Navigation
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
