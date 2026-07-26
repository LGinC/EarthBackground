using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Runtime.InteropServices;

namespace EarthBackground.Localization
{
    /// <summary>
    /// RESX-backed localization service with live language switching.
    /// </summary>
    public class ResourceLocalizationService : ILocalizationService
    {
        private const string ResourceBaseName = "EarthBackground.Assets.Strings.Strings";
        private static readonly ResourceManager Resources = new(
            ResourceBaseName,
            typeof(ResourceLocalizationService).Assembly);

        private readonly object _gate = new();
        private CultureInfo _culture;

        public ResourceLocalizationService()
            : this(ResolvePreferredUiCulture(), writeDiagnostics: true)
        {
        }

        public ResourceLocalizationService(CultureInfo culture)
            : this(culture, writeDiagnostics: false)
        {
        }

        private ResourceLocalizationService(CultureInfo culture, bool writeDiagnostics)
        {
            ArgumentNullException.ThrowIfNull(culture);
            _culture = NormalizeCulture(culture);
            ApplyThreadCulture(_culture);

            if (writeDiagnostics)
                TryWriteDiagnostics(_culture);
        }

        public string CurrentLanguage
        {
            get
            {
                lock (_gate)
                    return _culture.Name;
            }
        }

        public event Action<string>? LanguageChanged;

        public string this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                    return string.Empty;

                CultureInfo culture;
                lock (_gate)
                    culture = _culture;

                return GetString(key, culture);
            }
        }

        public string Format(string key, params object[] args)
        {
            var template = this[key];
            try { return string.Format(CultureInfo.CurrentCulture, template, args); }
            catch { return template; }
        }

        public void SetLanguage(string language)
        {
            ArgumentNullException.ThrowIfNull(language);

            CultureInfo culture;
            try
            {
                culture = NormalizeCulture(CultureInfo.GetCultureInfo(language));
            }
            catch (CultureNotFoundException)
            {
                culture = CultureInfo.InvariantCulture;
            }

            lock (_gate)
            {
                if (string.Equals(_culture.Name, culture.Name, StringComparison.OrdinalIgnoreCase)
                    && IsChinese(_culture) == IsChinese(culture))
                {
                    return;
                }

                _culture = culture;
                ApplyThreadCulture(culture);
            }

            LanguageChanged?.Invoke(culture.Name);
        }

        public static CultureInfo ResolvePreferredUiCulture()
        {
            foreach (var candidate in EnumeratePreferredCultures())
            {
                if (candidate is null)
                    continue;

                if (candidate.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                    return CultureInfo.GetCultureInfo("zh-CN");
            }

            return CultureInfo.CurrentUICulture ?? CultureInfo.InvariantCulture;
        }

        private static string GetString(string key, CultureInfo culture)
        {
            try
            {
                return Resources.GetString(key, culture) ?? key;
            }
            catch
            {
                return key;
            }
        }

        private static CultureInfo NormalizeCulture(CultureInfo culture)
        {
            if (IsChinese(culture))
                return CultureInfo.GetCultureInfo("zh-CN");
            return culture;
        }

        private static void ApplyThreadCulture(CultureInfo culture)
        {
            try
            {
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                CultureInfo.CurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
            }
            catch
            {
                // best-effort only
            }
        }

        private static bool IsChinese(CultureInfo culture)
        {
            for (var c = culture; c != null && !Equals(c, CultureInfo.InvariantCulture); c = c.Parent)
            {
                if (c.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static IEnumerable<CultureInfo?> EnumeratePreferredCultures()
        {
            yield return CultureInfo.CurrentUICulture;
            yield return CultureInfo.CurrentCulture;
            yield return CultureInfo.DefaultThreadCurrentUICulture;
            yield return CultureInfo.DefaultThreadCurrentCulture;

            if (OperatingSystem.IsWindows())
            {
                CultureInfo? windowsUi = null;
                try
                {
                    windowsUi = CultureInfo.GetCultureInfo(GetUserDefaultUILanguage());
                }
                catch
                {
                    // ignore
                }

                if (windowsUi is not null)
                    yield return windowsUi;
            }
        }

        [DllImport("kernel32.dll")]
        private static extern ushort GetUserDefaultUILanguage();

        private static void TryWriteDiagnostics(CultureInfo culture)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var path = Path.Combine(baseDir, "loc-diag.txt");
                var keys = new[]
                {
                    "MainWindow_Header",
                    "MainWindow_Title",
                    "Status_WaitForRun",
                    "Btn_Start",
                    "Btn_Stop",
                    "Btn_Settings",
                    "Btn_Exit",
                };
                var lines = new List<string>
                {
                    $"time={DateTimeOffset.Now:O}",
                    $"baseDir={baseDir}",
                    $"culture={culture.Name}",
                    $"ui={CultureInfo.CurrentUICulture.Name}",
                    $"resourceBaseName={ResourceBaseName}",
                };

                foreach (var key in keys)
                    lines.Add($"{key} => {GetString(key, culture)}");

                File.WriteAllLines(path, lines);
            }
            catch
            {
                // diagnostics must never break startup
            }
        }
    }
}
