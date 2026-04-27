using Avalonia.Controls;
using EarthBackground.ViewModels;

namespace EarthBackground.Views
{
    public partial class MainWindow : Window
    {
        public bool AllowClose { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (!AllowClose)
            {
                e.Cancel = true;
                Hide();
                Controls.ModernNotification.Show(
                    DataContext is MainWindowViewModel viewModel
                        ? viewModel.NotifyHiddenToTray
                        : "EarthBackground is still running in the system tray.",
                    Controls.ModernNotification.NotificationType.Info);
                return;
            }

            base.OnClosing(e);
        }
    }
}
