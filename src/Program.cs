using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EarthBackground.Background;
using EarthBackground.Captors;
using EarthBackground.Oss;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        static async Task Main(string[] args)
        {
            // 解析命令行参数
            bool runAsService = args.Length > 0 && (args[0] == "--service" || args[0] == "-s");
            
            if (runAsService)
            {
                // 作为后台服务运行（无UI）
                await RunAsBackgroundServiceAsync();
            }
            else
            {
                // 作为WinForm应用程序运行
                await RunAsWinFormAsync();
            }
        }

        /// <summary>
        /// 作为WinForm应用程序运行
        /// </summary>
        static async Task RunAsWinFormAsync()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh-CN");
            
            var serviceProvider = ConfigureServices(false);
            var mainForm = serviceProvider.GetRequiredService<MainForm>();
            
            var wallpaperService = serviceProvider.GetRequiredService<WallpaperService>();
            _ = Task.Run(() => wallpaperService.StartAsync(CancellationToken.None));
            wallpaperService.Start(); // 启动壁纸更新循环
            
            Application.Run(mainForm);
        }

        /// <summary>
        /// 作为后台服务运行（无UI）
        /// </summary>
        static async Task RunAsBackgroundServiceAsync()
        {
            var host = CreateHostBuilder().Build();
            
            // 启动壁纸服务
            var wallpaperService = host.Services.GetRequiredService<WallpaperService>();
            wallpaperService.Start();
            
            Console.WriteLine("EarthBackground 后台服务已启动...");
            Console.WriteLine("按 Ctrl+C 退出");
            
            await host.RunAsync();
        }

        /// <summary>
        /// 创建后台服务Host
        /// </summary>
        static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .UseWindowsService() // 支持Windows服务
                .ConfigureServices((context, services) =>
                {
                    ConfigureServicesInternal(services, true);
                });
        }

        static IServiceProvider ConfigureServices(bool isBackgroundService = false)
        {
            var services = new ServiceCollection();
            ConfigureServicesInternal(services, isBackgroundService);
            return services.BuildServiceProvider();
        }

        static void ConfigureServicesInternal(IServiceCollection services, bool isBackgroundService)
        {
            //添加配置读取
            var config = new ConfigurationBuilder().AddJsonFile(ConfigFile, optional: true, reloadOnChange: true)
                .Build();

            services.AddOptions();
            services.AddSingleton(config);
            services.Configure<CaptureOption>(config.GetSection("CaptureOptions"));
            services.Configure<OssOption>(config.GetSection("OssOptions"));
            services.AddTransient<IConfigureSaver, ConfigureSaver>();

            //注入抓取器
            services.AddTransient<ICaptorProvider, CaptorProvider>();
            services.AddKeyedTransient<ICaptor, Himawari8Captor>(NameConsts.Himawari8);
            services.AddKeyedTransient<ICaptor, FY4Captor>(NameConsts.Fy4);

            //注入oss
            services.AddTransient<IOssProvider, OssProvider>();
            services.AddKeyedTransient<IOssDownloader, DirectDownloader>(NameConsts.DirectDownload);
            services.AddKeyedTransient<IOssDownloader, CloudinaryDownloader>(NameConsts.Cloudinary);
            services.AddKeyedTransient<IOssDownloader, QiniuDownloader>(NameConsts.Qiqiuyun);

            //注入壁纸设置器
            services.AddTransient<IBackgroudSetProvider, BackgroudSetProvider>();
            services.AddTransient<IBackgroundSetter, WindowsBackgroudSetter>();

            AddHttpClients(services, config);

            //使用NLog
            var nlogConfig = new ConfigurationBuilder().AddJsonFile("nlog.json").Build();
            NLog.LogManager.Configuration = new NLogLoggingConfiguration(nlogConfig.GetSection("NLog"));
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddDebug();
                builder.AddNLog(nlogConfig.GetSection("NLog"));
            });

            if (isBackgroundService)
            {
                //后台服务模式：注册为HostedService
                services.AddHostedService<WallpaperService>();
            }
            else
            {
                //WinForm模式：注册窗体和单例服务
                services.AddSingleton(typeof(MainForm));
                services.AddTransient(typeof(SettingForm));
                services.AddSingleton<WallpaperService>();
            }
        }

        /// <summary>
        /// 3次重试策略
        /// </summary>
        /// <returns></returns>
        private static Polly.Retry.AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy() => HttpPolicyExtensions.HandleTransientHttpError().RetryAsync(5);

        static void AddHttpClients(IServiceCollection services, IConfiguration config)
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
