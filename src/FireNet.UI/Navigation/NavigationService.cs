using System.Windows.Controls;
using FireNet.UI.Views;

namespace FireNet.UI.Navigation
{
    public static class NavigationService
    {
        private static Frame _frame;

        private static HomePage? _homePage;
        private static LoginPage? _loginPage;
        private static SettingsPage? _settingsPage;

        public static void SetFrame(Frame frame)
        {
            _frame = frame;
        }

        private static void EnsureFrame()
        {
            if (_frame == null)
                throw new System.InvalidOperationException("Navigation frame is not set.");
        }

        public static void NavigateToLogin()
        {
            EnsureFrame();

            _loginPage ??= new LoginPage();
            _frame.Navigate(_loginPage);
        }

        public static void NavigateToHome()
        {
            EnsureFrame();

            _homePage ??= new HomePage();
            _frame.Navigate(_homePage);
        }

        public static void NavigateToSettings()
        {
            EnsureFrame();

            _settingsPage ??= new SettingsPage();
            _frame.Navigate(_settingsPage);
        }
    }
}
