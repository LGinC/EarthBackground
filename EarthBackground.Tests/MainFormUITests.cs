using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using EarthBackground.Controls;
using EarthBackground.Views;
using Xunit;

namespace EarthBackground.Tests
{
    public class MainFormUITests
    {
        [AvaloniaFact]
        public void MainWindow_ShouldBindWindowTitleAndHeader()
        {
            var viewModel = new MainWindowTestViewModel
            {
                WindowTitle = "EarthBackground - Test",
                HeaderTitle = "Earth Background",
                StatusText = "Ready",
                ProgressText = "0/0",
                ProgressValue = 0,
                ProgressMax = 100,
                EarthRotationAngle = 0,
                BtnStart = "Start",
                BtnStop = "Stop",
                BtnSettings = "Settings",
                BtnExit = "Exit",
                CanStart = true,
                CanStop = false
            };

            var window = CreateWindow(viewModel);

            Assert.Equal(viewModel.WindowTitle, window.Title);

            var header = window
                .GetLogicalDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(x => string.Equals(x.Text, viewModel.HeaderTitle, StringComparison.Ordinal));

            Assert.NotNull(header);
        }

        [AvaloniaFact]
        public void MainWindow_ShouldReflectButtonBindings()
        {
            var viewModel = new MainWindowTestViewModel
            {
                WindowTitle = "EarthBackground - Test",
                HeaderTitle = "Earth Background",
                StatusText = "Running",
                ProgressText = "1/2",
                ProgressValue = 1,
                ProgressMax = 2,
                EarthRotationAngle = 10,
                BtnStart = "Start",
                BtnStop = "Stop",
                BtnSettings = "Settings",
                BtnExit = "Exit",
                CanStart = true,
                CanStop = false
            };

            var window = CreateWindow(viewModel);
            var buttons = window.GetLogicalDescendants().OfType<Button>().ToList();

            var startButton = FindButton(buttons, viewModel.BtnStart);
            var stopButton = FindButton(buttons, viewModel.BtnStop);
            var settingsButton = FindButton(buttons, viewModel.BtnSettings);
            var exitButton = FindButton(buttons, viewModel.BtnExit);

            Assert.NotNull(startButton);
            Assert.NotNull(stopButton);
            Assert.NotNull(settingsButton);
            Assert.NotNull(exitButton);

            Assert.True(startButton!.IsEnabled);
            Assert.False(stopButton!.IsEnabled);
            Assert.True(settingsButton!.IsEnabled);
            Assert.True(exitButton!.IsEnabled);
        }

        [AvaloniaFact]
        public void MainWindow_ShouldReflectStatusProgressAndEarthRotationBindings()
        {
            var viewModel = new MainWindowTestViewModel
            {
                WindowTitle = "EarthBackground - Test",
                HeaderTitle = "Earth Background",
                StatusText = "Downloading...",
                ProgressText = "3/5 (60%)",
                ProgressValue = 3,
                ProgressMax = 5,
                EarthRotationAngle = 42,
                BtnStart = "Start",
                BtnStop = "Stop",
                BtnSettings = "Settings",
                BtnExit = "Exit",
                CanStart = false,
                CanStop = true
            };

            var window = CreateWindow(viewModel);

            var textBlocks = window.GetLogicalDescendants().OfType<TextBlock>().ToList();
            var progressBar = window.GetLogicalDescendants().OfType<ProgressBar>().FirstOrDefault();
            var earthCanvas = window.GetLogicalDescendants().OfType<EarthCanvas>().FirstOrDefault();

            Assert.Contains(textBlocks, x => string.Equals(x.Text, viewModel.StatusText, StringComparison.Ordinal));
            Assert.Contains(textBlocks, x => string.Equals(x.Text, viewModel.ProgressText, StringComparison.Ordinal));
            Assert.NotNull(progressBar);
            Assert.Equal(viewModel.ProgressValue, progressBar!.Value);
            Assert.Equal(viewModel.ProgressMax, progressBar.Maximum);
            Assert.NotNull(earthCanvas);
            Assert.Equal(viewModel.EarthRotationAngle, earthCanvas!.RotationAngle);
        }

        private static MainWindow CreateWindow(MainWindowTestViewModel viewModel)
        {
            var window = new MainWindow
            {
                DataContext = viewModel,
                Width = 480,
                Height = 300
            };

            window.ApplyTemplate();
            window.Measure(new Size(window.Width, window.Height));
            window.Arrange(new Rect(0, 0, window.Width, window.Height));

            return window;
        }

        private static Button? FindButton(IEnumerable<Button> buttons, string content)
        {
            return buttons.FirstOrDefault(button => string.Equals(button.Content?.ToString(), content, StringComparison.Ordinal));
        }

        private sealed class MainWindowTestViewModel
        {
            public string WindowTitle { get; init; } = string.Empty;
            public string HeaderTitle { get; init; } = string.Empty;
            public string StatusText { get; init; } = string.Empty;
            public string ProgressText { get; init; } = string.Empty;
            public double ProgressValue { get; init; }
            public int ProgressMax { get; init; }
            public float EarthRotationAngle { get; init; }
            public string BtnStart { get; init; } = string.Empty;
            public string BtnStop { get; init; } = string.Empty;
            public string BtnSettings { get; init; } = string.Empty;
            public string BtnExit { get; init; } = string.Empty;
            public bool CanStart { get; init; }
            public bool CanStop { get; init; }
            public ICommand StartCommand { get; } = new NoOpCommand();
            public ICommand StopCommand { get; } = new NoOpCommand();
            public ICommand SettingsCommand { get; } = new NoOpCommand();
            public ICommand ExitCommand { get; } = new NoOpCommand();
        }

        private sealed class NoOpCommand : ICommand
        {
            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter)
            {
            }
        }
    }
}
