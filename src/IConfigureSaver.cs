using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EarthBackground.Oss;

namespace EarthBackground
{
    public interface IConfigureSaver
    {
        Task SaveAsync(CaptureOption option, OssOption ossOption);
    }

    public class ConfigureSaver : IConfigureSaver
    {
        public Task SaveAsync(CaptureOption option, OssOption ossOption)
        {
            string filePath = AppPaths.AppSettingsPath;
            var settings = File.Exists(filePath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(filePath))
                : null;

            if (option == null && ossOption == null)
            {
                return Task.CompletedTask;
            }

            settings ??= new AppSettings();

            if (option != null)
            {
                settings.CaptureOptions = option;
            }

            if (ossOption != null)
            {
                settings.OssOptions = ossOption;
            }

            return File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(settings));
        }
    }
}
