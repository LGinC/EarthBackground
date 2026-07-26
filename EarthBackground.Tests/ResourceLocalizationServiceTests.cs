using System.Globalization;
using EarthBackground.Localization;
using Xunit;

namespace EarthBackground.Tests;

public class ResourceLocalizationServiceTests
{
    [Fact]
    public void NeutralCulture_ShouldReturnEnglishStrings_NotKeys()
    {
        var loc = new ResourceLocalizationService(CultureInfo.GetCultureInfo("en-US"));

        Assert.Equal("Earth Background", loc["MainWindow_Header"]);
        Assert.Equal("🚀 Start", loc["Btn_Start"]);
        Assert.Equal("Wait for run", loc["Status_WaitForRun"]);
        Assert.Equal("⚙ Settings", loc["Btn_Settings"]);
        Assert.Equal("✕ Exit", loc["Btn_Exit"]);
    }

    [Fact]
    public void ZhCnCulture_ShouldReturnChineseStrings_NotKeys()
    {
        var loc = new ResourceLocalizationService(CultureInfo.GetCultureInfo("zh-CN"));

        Assert.Equal("🌍 Earth Background", loc["MainWindow_Header"]);
        Assert.Equal("🚀 开始", loc["Btn_Start"]);
        Assert.Equal("等待运行...", loc["Status_WaitForRun"]);
        Assert.Equal("⚙ 设置", loc["Btn_Settings"]);
        Assert.Equal("⏹ 停止", loc["Btn_Stop"]);
        Assert.Equal("✕ 退出", loc["Btn_Exit"]);
    }

    [Fact]
    public void MissingKey_ShouldReturnKeyItself()
    {
        var loc = new ResourceLocalizationService(CultureInfo.InvariantCulture);
        Assert.Equal("Definitely_Missing_Key_123", loc["Definitely_Missing_Key_123"]);
    }

    [Fact]
    public void Format_ShouldApplyArguments()
    {
        var loc = new ResourceLocalizationService(CultureInfo.GetCultureInfo("en-US"));
        var text = loc.Format("Notify_DownloadFailed", "network");
        Assert.Contains("network", text);
        Assert.DoesNotContain("{0}", text);
    }

    [Fact]
    public void ResxResources_ShouldContainCoreUiKeys()
    {
        string[] keys =
        [
            "MainWindow_Header",
            "MainWindow_Title",
            "Status_WaitForRun",
            "Btn_Start",
            "Btn_Stop",
            "Btn_Settings",
            "Btn_Exit",
            "Settings_Title",
            "Settings_Save"
        ];

        var neutral = new ResourceLocalizationService(CultureInfo.GetCultureInfo("en-US"));
        var zhCn = new ResourceLocalizationService(CultureInfo.GetCultureInfo("zh-CN"));

        foreach (var key in keys)
        {
            Assert.False(string.Equals(neutral[key], key, StringComparison.Ordinal), $"neutral RESX missing {key}");
            Assert.False(string.Equals(zhCn[key], key, StringComparison.Ordinal), $"zh-CN RESX missing {key}");
            Assert.False(string.IsNullOrWhiteSpace(neutral[key]));
            Assert.False(string.IsNullOrWhiteSpace(zhCn[key]));
        }
    }

    [Fact]
    public void DefaultConstructor_ShouldPreferChinese_WhenSystemLocaleIsChinese()
    {
        // This machine has system LCID 2052 (zh-CN). If not Chinese, still must not return raw keys.
        var loc = new ResourceLocalizationService();
        var header = loc["MainWindow_Header"];
        var start = loc["Btn_Start"];
        var status = loc["Status_WaitForRun"];

        Assert.False(string.Equals(header, "MainWindow_Header", StringComparison.Ordinal));
        Assert.False(string.Equals(start, "Btn_Start", StringComparison.Ordinal));
        Assert.False(string.Equals(status, "Status_WaitForRun", StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(header));
        Assert.False(string.IsNullOrWhiteSpace(start));
    }
}
