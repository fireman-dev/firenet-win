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

            // تنظیم فریم ناوبری
            NavigationService.SetFrame(MainFrame);

            // جایگزین: حالا SessionManager Singleton است
            if (SessionManager.Instance.IsLoggedIn)
                NavigationService.NavigateToHome();
            else
                NavigationService.NavigateToLogin();
        }

        // جلوگیری از برگشت با Backspace
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
