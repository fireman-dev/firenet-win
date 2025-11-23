using System.Windows.Controls;
using FireNet.UI.Views;

namespace FireNet.UI.Navigation
{
    public static class NavigationService
    {
        private static Frame _frame;

        private static HomePage? _homePage;
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

        // همیشه LoginPage جدید بساز تا باگِ لاگین بعد از TokenExpired حل شود
        public static void NavigateToLogin()
        {
            EnsureFrame();
            _frame.Navigate(new LoginPage());
        }

        // HomePage نباید دوباره ساخته شود
        public static void NavigateToHome()
        {
            EnsureFrame();

            if (_homePage == null)
                _homePage = new HomePage();

            _frame.Navigate(_homePage);
        }

        // SettingsPage فقط یک بار ساخته شود
        public static void NavigateToSettings()
        {
            EnsureFrame();

            if (_settingsPage == null)
                _settingsPage = new SettingsPage();

            _frame.Navigate(_settingsPage);
        }
    }
}
