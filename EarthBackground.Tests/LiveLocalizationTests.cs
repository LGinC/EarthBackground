using System.Globalization;
using Avalonia.Headless.XUnit;
using EarthBackground.Localization;
using Xunit;

namespace EarthBackground.Tests;

public class LiveLocalizationTests
{
    [Fact]
    public void SetLanguage_ShouldRaiseLanguageChanged_AndSwitchStrings()
    {
        var loc = new ResourceLocalizationService(CultureInfo.GetCultureInfo("en-US"));
        string? raised = null;
        loc.LanguageChanged += lang => raised = lang;

        Assert.Equal("🚀 Start", loc["Btn_Start"]);
        Assert.Equal("en-US", loc.CurrentLanguage);

        loc.SetLanguage("zh-CN");

        Assert.Equal("zh-CN", raised);
        Assert.Equal("zh-CN", loc.CurrentLanguage);
        Assert.Equal("🚀 开始", loc["Btn_Start"]);
        Assert.Equal("🌍 Earth Background", loc["MainWindow_Header"]);
    }

    [AvaloniaFact]
    public void LocalizedStrings_Attach_ShouldRefreshEntriesOnLanguageChange()
    {
        var loc = new ResourceLocalizationService(CultureInfo.GetCultureInfo("en-US"));
        LocalizedStrings.Instance.Attach(loc);

        var entry = LocalizedStrings.Instance.GetEntry("Btn_Start");
        Assert.Equal("🚀 Start", entry.Value);

        var notified = false;
        entry.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LocalizedText.Value))
                notified = true;
        };

        loc.SetLanguage("zh-CN");

        Assert.True(notified);
        Assert.Equal("🚀 开始", entry.Value);
        Assert.Equal("🚀 开始", LocalizedStrings.Instance["Btn_Start"]);
    }

    [Fact]
    public void SetLanguage_SameLanguage_ShouldNotRaise()
    {
        var loc = new ResourceLocalizationService(CultureInfo.GetCultureInfo("zh-CN"));
        var count = 0;
        loc.LanguageChanged += _ => count++;

        loc.SetLanguage("zh-CN");
        Assert.Equal(0, count);
    }
}
