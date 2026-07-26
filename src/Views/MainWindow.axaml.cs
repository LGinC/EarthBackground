using System;
using Avalonia.Controls;
using EarthBackground.Localization;
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
                        : LocalizedStrings.Instance["Notify_HiddenToTray"],
                    Controls.ModernNotification.NotificationType.Info);
                return;
            }

            base.OnClosing(e);
        }
    }
}
