using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EarthBackground.Captors
{
    public interface ICaptorProvider
    {
        /// <summary>
        /// 获取抓取器
        /// </summary>
        /// <param name="name">抓取器名称</param>
        /// <returns></returns>
        ICaptor GetCaptor(string name = null);
    }

    public class CaptorProvider : ICaptorProvider
    {
        private readonly IServiceProvider _provider;
        private readonly CaptureOption _option;

        public CaptorProvider(IServiceProvider provider, IOptionsSnapshot<CaptureOption> options)
        {
            _provider = provider;
            _option = options.Value;
        }

        public ICaptor GetCaptor(string name = null)
        {
            name = string.IsNullOrEmpty(name) ? _option.Captor : name;
            var captors = _provider.GetServices<ICaptor>();
            return captors.FirstOrDefault(p => p.ProviderName == name);
        }
    }
}
