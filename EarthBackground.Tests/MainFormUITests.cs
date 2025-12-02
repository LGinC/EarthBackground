using System;
using System.IO;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace EarthBackground.Tests
{
    public class MainFormUITests
    {
        // Note: UI Tests require the app to be built and available.
        // We assume the app is built in the default location relative to tests.
        // Adjust path as needed.
        private const string AppPath = @"..\..\..\..\bin\Debug\net10.0-windows\EarthBackground.exe";

        [Fact]
        public void AppLaunch_ShouldShowMainForm()
        {
            // Skip if app not found (e.g. CI environment without build)
            if (!File.Exists(AppPath)) return;

            using var app = Application.Launch(AppPath);
            using var automation = new UIA3Automation();
            
            var window = app.GetMainWindow(automation);
            
            Assert.NotNull(window);
            Assert.Equal("EarthBackground", window.Title);
        }

        [Fact]
        public void StartButton_ShouldBeEnabled_OnLaunch()
        {
            if (!File.Exists(AppPath)) return;

            using var app = Application.Launch(AppPath);
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation);

            // Assuming button name/automationId is "B_start" or "Start"
            // WinForms usually uses the Control Name as AutomationId
            var startButton = window.FindFirstDescendant(cf => cf.ByAutomationId("B_start"))?.AsButton();
            
            Assert.NotNull(startButton);
            Assert.True(startButton.IsEnabled);
        }
    }
}
