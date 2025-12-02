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
            string filePath = "appsettings.json";
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(filePath));
            if(option == null && ossOption == null)
            {
                return Task.CompletedTask;
            }
            if(option != null)
            {
                settings.CaptureOptions = option;
            }
            if(ossOption != null)
            {
                settings.OssOptions = ossOption;
            }
            return File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(settings));
        }
    }
}
