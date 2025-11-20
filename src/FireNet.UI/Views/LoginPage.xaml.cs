using System.Windows.Controls;
using FireNet.UI.ViewModels;

namespace FireNet.UI.Views
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();

            var vm = DataContext as LoginViewModel;
            if (vm != null)
            {
                vm.GetPassword = () => PasswordInput.Password;
            }
        }
    }
}
