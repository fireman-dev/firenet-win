using System.Windows.Controls;
using System.Windows;
using FireNet.UI.ViewModels;

namespace FireNet.UI.Views
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();

            if (DataContext is LoginViewModel vm)
            {
                vm.GetPassword = () => PasswordInput.Password;
            }
        }
    }
}
