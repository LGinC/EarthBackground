using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using EarthBackground.Views;
using Xunit;

namespace EarthBackground.Tests
{
    public class SettingsWindowUITests
    {
        [AvaloniaFact]
        public void SettingsWindow_ShouldBindTitleAndHeader()
        {
            var viewModel = CreateViewModel();
            var window = CreateWindow(viewModel);

            Assert.Equal(viewModel.WindowTitle, window.Title);

            var headers = window.GetLogicalDescendants().OfType<TextBlock>().Select(x => x.Text).ToList();
            Assert.Contains(viewModel.HeaderTitle, headers);
            Assert.Contains(viewModel.Label_CaptureSection, headers);
            Assert.Contains(viewModel.Label_DownloadSection, headers);
        }

        [AvaloniaFact]
        public void SettingsWindow_ShouldShowExpectedActionButtonsAndOptions()
        {
            var viewModel = CreateViewModel();
            var window = CreateWindow(viewModel);

            var buttons = window.GetLogicalDescendants().OfType<Button>().ToList();
            var checkBoxes = window.GetLogicalDescendants().OfType<CheckBox>().ToList();
            var comboBoxes = window.GetLogicalDescendants().OfType<ComboBox>().ToList();

            Assert.Contains(buttons, x => string.Equals(x.Content?.ToString(), viewModel.Label_ChoosePath, StringComparison.Ordinal));
            Assert.Contains(buttons, x => string.Equals(x.Content?.ToString(), viewModel.Label_Save, StringComparison.Ordinal));

            Assert.Contains(checkBoxes, x => string.Equals(x.Content?.ToString(), viewModel.Label_AutoStart, StringComparison.Ordinal));
            Assert.Contains(checkBoxes, x => string.Equals(x.Content?.ToString(), viewModel.Label_DynamicWallpaper, StringComparison.Ordinal));
            Assert.Contains(checkBoxes, x => string.Equals(x.Content?.ToString(), viewModel.Label_SetWallpaper, StringComparison.Ordinal));
            Assert.Contains(checkBoxes, x => string.Equals(x.Content?.ToString(), viewModel.Label_SaveWallpaper, StringComparison.Ordinal));
            Assert.Contains(checkBoxes, x => string.Equals(x.Content?.ToString(), viewModel.Label_AllDynamicWallpaperMonitors, StringComparison.Ordinal));

            Assert.True(comboBoxes.Count >= 4);
        }

        [AvaloniaFact]
        public void SettingsWindow_ShouldReflectDynamicWallpaperFieldVisibility()
        {
            var viewModel = CreateViewModel();
            var window = CreateWindow(viewModel);

            var textBlocks = window.GetLogicalDescendants().OfType<TextBlock>().ToList();

            var recentHoursLabel = textBlocks.FirstOrDefault(x => string.Equals(x.Text, viewModel.Label_RecentHours, StringComparison.Ordinal));
            var frameIntervalLabel = textBlocks.FirstOrDefault(x => string.Equals(x.Text, viewModel.Label_FrameInterval, StringComparison.Ordinal));
            var loopPauseLabel = textBlocks.FirstOrDefault(x => string.Equals(x.Text, viewModel.Label_LoopPauseMilliseconds, StringComparison.Ordinal));

            Assert.NotNull(recentHoursLabel);
            Assert.NotNull(frameIntervalLabel);
            Assert.NotNull(loopPauseLabel);
            Assert.True(recentHoursLabel!.IsVisible);
            Assert.True(frameIntervalLabel!.IsVisible);
            Assert.True(loopPauseLabel!.IsVisible);
        }

        [AvaloniaFact]
        public void SettingsWindow_ShouldShowDynamicWallpaperMonitorSelection()
        {
            var viewModel = CreateViewModel();
            viewModel.AllDynamicWallpaperMonitors = false;
            viewModel.DynamicWallpaperMonitorListVisible = true;
            var window = CreateWindow(viewModel);

            var textBlocks = window.GetLogicalDescendants().OfType<TextBlock>().ToList();
            var monitorList = window.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(x => ReferenceEquals(x.ItemsSource, viewModel.DynamicWallpaperMonitors));

            Assert.Contains(textBlocks, x => string.Equals(x.Text, viewModel.Label_DynamicWallpaperMonitors, StringComparison.Ordinal));
            Assert.NotNull(monitorList);
            Assert.True(monitorList!.IsVisible);
            Assert.Same(viewModel.DynamicWallpaperMonitors, monitorList.ItemsSource);
        }

        [AvaloniaFact]
        public void SettingsWindow_ShouldHideDynamicWallpaperMonitorSelection_WhenDynamicWallpaperDisabled()
        {
            var viewModel = CreateViewModel();
            viewModel.DynamicWallpaper = false;
            viewModel.DynamicWallpaperMonitorListVisible = false;
            var window = CreateWindow(viewModel);

            var textBlocks = window.GetLogicalDescendants().OfType<TextBlock>().ToList();
            var monitorLabel = textBlocks.FirstOrDefault(x => string.Equals(x.Text, viewModel.Label_DynamicWallpaperMonitors, StringComparison.Ordinal));

            Assert.NotNull(monitorLabel);
            Assert.False(monitorLabel!.IsVisible);
        }

        private static SettingsWindow CreateWindow(SettingsWindowTestViewModel viewModel)
        {
            var window = new SettingsWindow
            {
                DataContext = viewModel,
                Width = 760,
                Height = 680
            };

            window.ApplyTemplate();
            window.Measure(new Size(window.Width, window.Height));
            window.Arrange(new Rect(0, 0, window.Width, window.Height));

            return window;
        }

        private static SettingsWindowTestViewModel CreateViewModel()
        {
            return new SettingsWindowTestViewModel
            {
                WindowTitle = "Settings - EarthBackground",
                HeaderTitle = "Settings",
                Label_CaptureSection = "Capture Settings",
                Label_DownloadSection = "Download Settings",
                Label_AutoStart = "Launch at startup",
                Label_DynamicWallpaper = "Dynamic wallpaper",
                Label_SetWallpaper = "Set as wallpaper",
                Label_SaveWallpaper = "Save wallpaper copy",
                Label_Satellite = "Satellite",
                Label_Resolution = "Resolution",
                Label_Interval = "Update interval (min)",
                Label_FrameInterval = "Frame interval (min)",
                Label_Zoom = "Zoom",
                Label_RecentHours = "Recent hours",
                Label_LoopPauseMilliseconds = "Loop pause (ms)",
                Label_DynamicWallpaperMonitors = "Apply to displays",
                Label_AllDynamicWallpaperMonitors = "All displays",
                Label_SavePath = "Save path",
                Label_ChoosePath = "Browse",
                Label_Downloader = "Downloader",
                Label_Username = "Username",
                Label_ApiKey = "API Key",
                Label_ApiSecret = "API Secret",
                Label_Domain = "Domain",
                Label_Bucket = "Bucket",
                Label_Zone = "Zone",
                Label_Save = "Save Settings",
                AutoStart = true,
                DynamicWallpaper = true,
                SetWallpaper = true,
                SaveWallpaper = false,
                SavePath = "images",
                Captors = new[] { new NameValueStub("Himawari-9") },
                Resolutions = new[] { new NameValueStub("2752*2752") },
                Downloaders = new[] { new NameValueStub("Direct Download") },
                Zones = new[] { new NameValueStub("East China") },
                SelectedCaptor = new NameValueStub("Himawari-9"),
                SelectedResolution = new NameValueStub("2752*2752"),
                SelectedDownloader = new NameValueStub("Direct Download"),
                SelectedZone = new NameValueStub("East China"),
                ChooseSavePathEnabled = true,
                AllDynamicWallpaperMonitors = true,
                DynamicWallpaperMonitorListVisible = false,
                DynamicWallpaperMonitors = new[] { new MonitorSelectionStub("DISPLAY1 (1920x1080)") }
            };
        }

        private sealed class SettingsWindowTestViewModel
        {
            public string WindowTitle { get; init; } = string.Empty;
            public string HeaderTitle { get; init; } = string.Empty;
            public string Label_CaptureSection { get; init; } = string.Empty;
            public string Label_DownloadSection { get; init; } = string.Empty;
            public string Label_AutoStart { get; init; } = string.Empty;
            public string Label_DynamicWallpaper { get; init; } = string.Empty;
            public string Label_SetWallpaper { get; init; } = string.Empty;
            public string Label_SaveWallpaper { get; init; } = string.Empty;
            public string Label_Satellite { get; init; } = string.Empty;
            public string Label_Resolution { get; init; } = string.Empty;
            public string Label_Interval { get; init; } = string.Empty;
            public string Label_FrameInterval { get; init; } = string.Empty;
            public string Label_Zoom { get; init; } = string.Empty;
            public string Label_RecentHours { get; init; } = string.Empty;
            public string Label_LoopPauseMilliseconds { get; init; } = string.Empty;
            public string Label_DynamicWallpaperMonitors { get; init; } = string.Empty;
            public string Label_AllDynamicWallpaperMonitors { get; init; } = string.Empty;
            public string Label_SavePath { get; init; } = string.Empty;
            public string Label_ChoosePath { get; init; } = string.Empty;
            public string Label_Downloader { get; init; } = string.Empty;
            public string Label_Username { get; init; } = string.Empty;
            public string Label_ApiKey { get; init; } = string.Empty;
            public string Label_ApiSecret { get; init; } = string.Empty;
            public string Label_Domain { get; init; } = string.Empty;
            public string Label_Bucket { get; init; } = string.Empty;
            public string Label_Zone { get; init; } = string.Empty;
            public string Label_Save { get; init; } = string.Empty;
            public bool AutoStart { get; init; }
            public bool DynamicWallpaper { get; set; }
            public bool SetWallpaper { get; init; }
            public bool SaveWallpaper { get; init; }
            public string SavePath { get; init; } = string.Empty;
            public IEnumerable Captors { get; init; } = Array.Empty<object>();
            public IEnumerable Resolutions { get; init; } = Array.Empty<object>();
            public IEnumerable Downloaders { get; init; } = Array.Empty<object>();
            public IEnumerable Zones { get; init; } = Array.Empty<object>();
            public IEnumerable DynamicWallpaperMonitors { get; init; } = Array.Empty<object>();
            public object? SelectedCaptor { get; init; }
            public object? SelectedResolution { get; init; }
            public object? SelectedDownloader { get; init; }
            public object? SelectedZone { get; init; }
            public int Interval { get; init; } = 20;
            public int FrameIntervalMinutes { get; init; } = 10;
            public int FrameIntervalMaximum { get; init; } = 360;
            public int Zoom { get; init; } = 100;
            public int RecentHours { get; init; } = 24;
            public int LoopPauseMilliseconds { get; init; } = 3000;
            public string Username { get; init; } = string.Empty;
            public string ApiKey { get; init; } = string.Empty;
            public string ApiSecret { get; init; } = string.Empty;
            public string Domain { get; init; } = string.Empty;
            public string Bucket { get; init; } = string.Empty;
            public bool UsernameEnabled { get; init; } = true;
            public bool ApiKeyEnabled { get; init; } = true;
            public bool ApiSecretEnabled { get; init; } = true;
            public bool ZoneEnabled { get; init; } = true;
            public bool DomainEnabled { get; init; } = true;
            public bool BucketEnabled { get; init; } = true;
            public bool ChooseSavePathEnabled { get; init; }
            public bool AllDynamicWallpaperMonitors { get; set; }
            public bool DynamicWallpaperMonitorListVisible { get; set; }
            public ICommand ChooseSavePathCommand { get; } = new NoOpCommand();
            public ICommand SaveCommand { get; } = new NoOpCommand();
        }

        private sealed class NameValueStub
        {
            public NameValueStub(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        private sealed class MonitorSelectionStub
        {
            public MonitorSelectionStub(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public bool IsSelected { get; set; }
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
