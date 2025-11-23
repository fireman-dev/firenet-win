using System.Windows.Controls;
using FireNet.UI.ViewModels;

namespace FireNet.UI.Views
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
    }
}
