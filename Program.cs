using System;
using System.Net.Http;
using System.Threading;
using System.Windows.Forms;
using EarthBackground.Background;
using EarthBackground.Captors;
using EarthBackground.Oss;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace EarthBackground
{
    static class Program
    {
        public const string ConfigFile = "appsettings.json";
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh-CN");
            Application.Run(ConfigureServices().GetRequiredService<MainForm>());
        }

        static IServiceProvider ConfigureServices()
        {
            //添加配置读取
            var config = new ConfigurationBuilder().AddJsonFile(ConfigFile, optional: true, reloadOnChange: true)
                .Build();

            //添加DI
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSingleton(config);
            services.Configure<CaptureOption>(config.GetSection("CaptureOptions"));
            services.Configure<OssOption>(config.GetSection("OssOptions"));
            services.AddTransient<IConfigureSaver, ConfigureSaver>();

            //注入抓取器
            services.AddTransient<ICaptorProvider, CaptorProvider>();
            services.AddKeyedTransient<ICaptor, Himawari8Captor>(NameConsts.Himawari8);
            services.AddKeyedTransient<ICaptor, FY4Captor>(NameConsts.FY4);

            //注入oss
            services.AddTransient<IOssProvider, OssProvider>();
            services.AddKeyedTransient<IOssDownloader, DirectDownloader>(NameConsts.DirectDownload);
            services.AddKeyedTransient<IOssDownloader, CloudinaryDownloader>(NameConsts.Cloudinary);
            services.AddKeyedTransient<IOssDownloader, QiniuDownloader>(NameConsts.Qiqiuyun);

            //注入壁纸设置器
            services.AddTransient<IBackgroudSetProvider, BackgroudSetProvider>();
            services.AddTransient<IBackgroundSetter, WindowsBackgroudSetter>();

            services.AddHttpClients(config);

            //使用NLog
            var nlogConfig = new ConfigurationBuilder().AddJsonFile("nlog.json").Build();
            NLog.LogManager.Configuration = new NLogLoggingConfiguration(nlogConfig.GetSection("NLog"));
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddDebug();
                builder.AddNLog(nlogConfig.GetSection("NLog"));
            });
            //添加主窗体为单例
            services.AddSingleton(typeof(MainForm));
            services.AddTransient(typeof(SettingForm));
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// 3次重试策略
        /// </summary>
        /// <returns></returns>
        private static Polly.Retry.AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy() => HttpPolicyExtensions.HandleTransientHttpError().RetryAsync(5);

        static void AddHttpClients(this ServiceCollection services, IConfiguration config)
        {
            //跳过ssl验证
            var sslHandler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, a3, a4) => true };

            services.AddHttpClient(NameConsts.Himawari8, client =>
            {
                client.BaseAddress = new Uri($"https://rammb-slider.cira.colostate.edu/data/");
            }).ConfigurePrimaryHttpMessageHandler(builder => sslHandler).AddPolicyHandler(GetRetryPolicy());



            services.AddHttpClient(NameConsts.Cloudinary, client =>
            {
                client.BaseAddress = new Uri($"https://res.cloudinary.com/{config["OssOptions:UserName"]}/image/fetch/");
            }).ConfigurePrimaryHttpMessageHandler(builder => sslHandler).AddPolicyHandler(GetRetryPolicy());

            services.AddHttpClient(NameConsts.Qiqiuyun, client =>
            {
                client.BaseAddress = new Uri($"https://qiniu.com/{config["OssOptions:UserName"]}");
            }).ConfigurePrimaryHttpMessageHandler(builder => sslHandler).AddPolicyHandler(GetRetryPolicy());

            services.AddHttpClient(NameConsts.DirectDownload, client =>
            {
                client.DefaultRequestHeaders
                .Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.102 Safari/537.36 Edg/85.0.564.51")
                ;
            })
                .ConfigurePrimaryHttpMessageHandler(builder => sslHandler).AddPolicyHandler(GetRetryPolicy());
        }

    }
}
