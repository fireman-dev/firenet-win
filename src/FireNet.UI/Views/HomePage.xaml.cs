using System.Windows.Controls;
using FireNet.UI.ViewModels;

namespace FireNet.UI.Views
{
    public partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();
            this.DataContext = new HomeViewModel();
        }
    }
}
