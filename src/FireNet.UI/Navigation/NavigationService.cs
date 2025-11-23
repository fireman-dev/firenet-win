using System.Windows.Controls;
using FireNet.UI.Views;

namespace FireNet.UI.Navigation
{
    public static class NavigationService
    {
        private static Frame _frame;

        private static HomePage _homePage = new HomePage();       // ← ثابت
        private static SettingsPage _settingsPage = new SettingsPage(); // ← ثابت
        private static LoginPage _loginPage = new LoginPage();    // ← ثابت

        public static void SetFrame(Frame frame)
        {
            _frame = frame;
        }

        public static void NavigateToHome()
        {
            _frame.Navigate(_homePage); // ← new نمی‌سازیم
        }

        public static void NavigateToSettings()
        {
            _frame.Navigate(_settingsPage);
        }

        public static void NavigateToLogin()
        {
            _frame.Navigate(_loginPage);
        }
    }
}
