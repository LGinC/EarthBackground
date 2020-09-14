using System.Threading.Tasks;

namespace EarthBackground
{
    public interface ICaptor
    {
        /// <summary>
        /// 提供器名称
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 获取图片路径
        /// </summary>
        /// <returns></returns>
        Task<string> GetImagePath();

        /// <summary>
        /// 重置
        /// </summary>
        /// <returns></returns>
        Task ResetAsync();
    }
}
