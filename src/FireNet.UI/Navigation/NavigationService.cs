using System.Windows.Controls;
using FireNet.UI.Views;

namespace FireNet.UI.Navigation
{
    public static class NavigationService
    {
        public static Frame MainFrame;

        public static void NavigateToLogin() => MainFrame.Navigate(new LoginPage());
        public static void NavigateToHome() => MainFrame.Navigate(new HomePage());
    }
}
