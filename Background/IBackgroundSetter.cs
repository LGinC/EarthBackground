using System.Threading.Tasks;

namespace EarthBackground.Background
{
    public interface IBackgroundSetter
    {
        string Platform { get; }
        Task SetBackgroudAsync(string filePath);
    }
}
