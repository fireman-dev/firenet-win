using System.Windows;
using System.Windows.Input;
using FireNet.UI.Navigation;

namespace FireNet.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            NavigationService.SetFrame(MainFrame);
            NavigationService.NavigateToHome();   // ← صفحه اول
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
