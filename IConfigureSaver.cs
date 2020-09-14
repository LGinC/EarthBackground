using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace EarthBackground
{
    public interface IConfigureSaver
    {
        Task SaveAsync(CaptureOption option);
    }

    public class ConfigureSaver : IConfigureSaver
    {
        public Task SaveAsync(CaptureOption option)
        {
            return File.WriteAllTextAsync(Program.ConfigFile, JsonSerializer.Serialize(option));
        }
    }
}
