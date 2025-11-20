using System.Windows.Controls;
using FireNet.UI.ViewModels;

namespace FireNet.UI.Views
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();

            var vm = new LoginViewModel();
            this.DataContext = vm;

            vm.GetPassword = () => PasswordBox.Password;
        }
    }
}
