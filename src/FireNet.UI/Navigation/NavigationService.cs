using System.Windows.Controls;
using FireNet.UI.Views;
using FireNet.Core.Session;

namespace FireNet.UI.Navigation
{
    public static class NavigationService
    {
        private static Frame _frame;

        public static void SetFrame(Frame frame)
        {
            _frame = frame;
        }

        public static void NavigateToLogin()
        {
            // جلوگیری از ورود به صفحه لاگین اگر کاربر لاگین است
            if (SessionManager.Instance.IsLoggedIn)
                return;

            _frame.Navigate(new LoginPage());
        }

        public static void NavigateToHome()
        {
            _frame.Navigate(new HomePage());
        }

        public static void NavigateToSettings()
        {
            _frame.Navigate(new SettingsPage());
        }
    }
}
