using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EarthBackground.Oss
{
    public interface IOssProvider
    {
        /// <summary>
        /// 获取指定名称的下载器 如name和配置文件都未指定下载器，则默认使用直接下载
        /// </summary>
        /// <param name="name">下载器名称</param>
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
                name = _option.IsEnable ? _option.CloudName : NameConsts.DirectDownload;
            }
            return _provider.GetKeyedService< IOssDownloader >(name);
        }
    }
}
