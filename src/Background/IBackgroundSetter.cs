using System.Threading;
using System.Threading.Tasks;

namespace EarthBackground.Background
{
    public interface IBackgroundSetter
    {
        string Platform { get; }
        Task SetBackgroundAsync(string filePath, CancellationToken token = default);
    }
}
