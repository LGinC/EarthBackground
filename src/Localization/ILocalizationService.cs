namespace EarthBackground.Localization
{
    /// <summary>
    /// RESX-backed localization with optional live language switching.
    /// </summary>
    public interface ILocalizationService
    {
        string this[string key] { get; }

        string Format(string key, params object[] args);

        /// <summary>Current UI culture name (e.g. "zh-CN", "en-US").</summary>
        string CurrentLanguage { get; }

        /// <summary>Switch UI language; raises <see cref="LanguageChanged"/>.</summary>
        void SetLanguage(string language);

        /// <summary>Raised after language changes so live bindings can refresh.</summary>
        event System.Action<string>? LanguageChanged;
    }
}
