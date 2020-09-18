using System;
using System.Net.Http;
using System.Windows.Forms;
using EarthBackground.Background;
using EarthBackground.Captors;
using EarthBackground.Oss;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            services.AddTransient<ICaptor, Himawari8Captor>();

            //注入oss
            services.AddTransient<IOssProvider, OssProvider>();
            services.AddTransient<IOssDownloader, DirectDownloader>();
            services.AddTransient<IOssDownloader, CloudinaryDownloader>();
            services.AddTransient<IOssDownloader, QiniuDownloader>();

            //注入壁纸设置器
            services.AddTransient<IBackgroudSetProvider, BackgroudSetProvider>();
            services.AddTransient<IBackgroundSetter, WindowsBackgroudSetter>();

            services.AddHttpClients(config);


            services.AddLogging(builder =>
            {
                builder.AddDebug();
            });
            //添加主窗体为单例
            services.AddSingleton(typeof(MainForm));
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
                client.BaseAddress = new Uri($"https://rammb-slider.cira.colostate.edu/data/imagery/{DateTime.UtcNow.AddHours(-1.5):yyyyMMdd}/himawari---full_disk/geocolor/");
            }).ConfigurePrimaryHttpMessageHandler(builder => sslHandler).AddPolicyHandler(GetRetryPolicy());



            services.AddHttpClient(NameConsts.Cloudinary, client =>
            {
                client.BaseAddress = new Uri($"https://res.cloudinary.com/{config["OssOptions:UserName"]}/image/fetch/");
            }).ConfigurePrimaryHttpMessageHandler(builder => sslHandler).AddPolicyHandler(GetRetryPolicy());

            services.AddHttpClient(NameConsts.Qiqiuyun, client =>
            {
                client.BaseAddress = new Uri($"https://qiniu.com/{config["OssOptions:UserName"]}");
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Qiniu", "");
            }).ConfigurePrimaryHttpMessageHandler(builder => sslHandler).AddPolicyHandler(GetRetryPolicy());

            services.AddHttpClient(NameConsts.DirectDownload, client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.102 Safari/537.36 Edg/85.0.564.51");
            })
                .ConfigurePrimaryHttpMessageHandler(builder => sslHandler).AddPolicyHandler(GetRetryPolicy());
        }

    }
}
