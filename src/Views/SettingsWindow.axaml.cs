using Avalonia.Controls;
using EarthBackground.ViewModels;

namespace EarthBackground.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        protected override void OnOpened(System.EventArgs e)
        {
            base.OnOpened(e);
            if (DataContext is SettingsWindowViewModel vm)
            {
                vm.OwnerWindow = this;
            }
        }
    }
}
