namespace EarthBackground.Localization
{
    public interface ILocalizationService
    {
        string this[string key] { get; }
        string Format(string key, params object[] args);
    }
}
