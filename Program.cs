using System;
using System.Windows.Forms;
using EarthBackground.Captors;
using EarthBackground.Oss;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

            services.AddHttpClient(NameConsts.Himawari8, client =>
            {
                client.BaseAddress = new Uri("https://himawari8-dl.nict.go.jp/himawari8/");
            });

            services.AddHttpClient(NameConsts.Cloudinary, client =>
            {
                client.BaseAddress = new Uri($"https://res.cloudinary.com/{config["OssOptions:UserName"]}/image/fetch/");
            });

            services.AddLogging(builder =>
            {
                builder.AddDebug();
            });
            //添加主窗体为单例
            services.AddSingleton(typeof(MainForm));
            return services.BuildServiceProvider();
        }

    }
}
