using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EarthBackground.Oss
{
    public interface IOssProvider
    {
        /// <summary>
        /// 获取指定名称的下载器
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IOssDownloader GetDownloader(string name = null);
    }

    public class OssProvider : IOssProvider
    {
        private readonly OssOption _option;
        private readonly IServiceProvider _provider;

        public OssProvider(IOptionsSnapshot<OssOption> option, IServiceProvider provider)
        {
            _option = option.Value;
            _provider = provider;
        }

        public IOssDownloader GetDownloader(string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = _option.IsEnable ? _option.CloudName : "DirectDownload";
            }
            var downloaders =  _provider.GetServices<IOssDownloader>();
            return downloaders.FirstOrDefault(p => p.ProviderName == name);
        }
    }
}
