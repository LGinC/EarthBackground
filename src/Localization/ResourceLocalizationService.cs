using System;
using System.Globalization;
using System.Resources;

namespace EarthBackground.Localization
{
    public class ResourceLocalizationService : ILocalizationService
    {
        private readonly ResourceManager _resourceManager;

        public ResourceLocalizationService()
        {
            _resourceManager = new ResourceManager(
                "EarthBackground.Assets.Strings.Strings",
                typeof(ResourceLocalizationService).Assembly);
        }

        public string this[string key]
        {
            get
            {
                try
                {
                    return _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
                }
                catch
                {
                    return key;
                }
            }
        }

        public string Format(string key, params object[] args)
        {
            var template = this[key];
            try { return string.Format(template, args); }
            catch { return template; }
        }
    }
}
