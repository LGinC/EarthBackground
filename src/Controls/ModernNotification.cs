using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace EarthBackground.Controls
{
    /// <summary>
    /// 现代化通知提示控件（Avalonia 版本）
    /// </summary>
    public class ModernNotification : Window
    {
        public enum NotificationType
        {
            Success,
            Warning,
            Error,
            Info
        }

        private ModernNotification(string message, NotificationType type, int duration)
        {
            SystemDecorations = SystemDecorations.None;
            ShowInTaskbar = false;
            Topmost = true;
            Width = 350;
            Height = 80;
            Background = Brushes.White;
            CanResize = false;

            // Position at bottom-right
            var screen = Screens.Primary;
            if (screen != null)
            {
                var workArea = screen.WorkingArea;
                Position = new PixelPoint(
                    workArea.Right - (int)Width - 20,
                    workArea.Bottom - (int)Height - 20);
            }

            var (accentColor, icon) = GetNotificationStyle(type);

            var content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("4,Auto,*"),
                Margin = new Thickness(8)
            };

            // Left accent bar
            var accentBar = new Border
            {
                Background = new SolidColorBrush(accentColor),
                CornerRadius = new CornerRadius(2),
                Width = 4
            };
            Grid.SetColumn(accentBar, 0);
            content.Children.Add(accentBar);

            // Icon
            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0)
            };
            Grid.SetColumn(iconText, 1);
            content.Children.Add(iconText);

            // Message
            var messageText = new TextBlock
            {
                Text = message,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94)),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(messageText, 2);
            content.Children.Add(messageText);

            Content = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Child = content,
                BoxShadow = new BoxShadows(new BoxShadow
                {
                    Blur = 8,
                    Color = Color.FromArgb(30, 0, 0, 0),
                    OffsetX = 0,
                    OffsetY = 2
                })
            };

            // Auto-close after duration
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(duration) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                Close();
            };

            Opened += (s, e) => timer.Start();
        }

        private static (Color color, string icon) GetNotificationStyle(NotificationType type) => type switch
        {
            NotificationType.Success => (Color.FromRgb(46, 204, 113), "✓"),
            NotificationType.Warning => (Color.FromRgb(241, 196, 15), "⚠"),
            NotificationType.Error => (Color.FromRgb(231, 76, 60), "✗"),
            NotificationType.Info => (Color.FromRgb(52, 152, 219), "ℹ"),
            _ => (Color.FromRgb(149, 165, 166), "•")
        };

        public static void Show(string message, NotificationType type = NotificationType.Info, int duration = 3000)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var notification = new ModernNotification(message, type, duration);
                notification.Show();
            });
        }
    }
}
