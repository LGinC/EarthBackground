using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;
using EarthBackground.Background;
using EarthBackground.Captors;
using EarthBackground.Localization;
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

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .WithInterFont()
                .LogToTrace();

        [STAThread]
        static async Task<int> Main(string[] args)
        {
            // 解析命令行参数
            bool runAsService = args.Length > 0 && (args[0] == "--service" || args[0] == "-s");

            if (runAsService)
            {
                await RunAsBackgroundServiceAsync();
                return 0;
            }
            else
            {
                return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
        }

        /// <summary>
        /// 作为后台服务运行（无UI）
        /// </summary>
        static async Task RunAsBackgroundServiceAsync()
        {
            var host = CreateHostBuilder().Build();

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
                .UseWindowsService()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServicesInternal(services, true);
                });
        }

        public static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            ConfigureServicesInternal(services, false);
            return services.BuildServiceProvider();
        }

        static void ConfigureServicesInternal(IServiceCollection services, bool isBackgroundService)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(ConfigFile, optional: true, reloadOnChange: true)
                .Build();

            services.AddOptions();
            services.AddSingleton(config);
            services.Configure<CaptureOption>(config.GetSection("CaptureOptions"));
            services.Configure<OssOption>(config.GetSection("OssOptions"));
            services.AddTransient<IConfigureSaver, ConfigureSaver>();
            services.AddSingleton<ILocalizationService, ResourceLocalizationService>();

            services.AddTransient<ICaptorProvider, CaptorProvider>();
            services.AddKeyedTransient<ICaptor, Himawari8Captor>(NameConsts.Himawari8);
            services.AddKeyedTransient<ICaptor, FY4Captor>(NameConsts.Fy4);

            services.AddTransient<IOssProvider, OssProvider>();
            services.AddKeyedTransient<IOssDownloader, DirectDownloader>(NameConsts.DirectDownload);
            services.AddKeyedTransient<IOssDownloader, CloudinaryDownloader>(NameConsts.Cloudinary);
            services.AddKeyedTransient<IOssDownloader, QiniuDownloader>(NameConsts.Qiqiuyun);

            services.AddTransient<IBackgroudSetProvider, BackgroudSetProvider>();
            services.AddTransient<IBackgroundSetter, WindowsBackgroudSetter>();
            services.AddSingleton<WindowsDynamicWallpaperSetter>();

            AddHttpClients(services, config);

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
                services.AddHostedService<WallpaperService>();
            }
            else
            {
                services.AddSingleton<WallpaperService>();
            }
        }

        private static Polly.Retry.AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
            => HttpPolicyExtensions.HandleTransientHttpError().RetryAsync(5);

        static void AddHttpClients(IServiceCollection services, IConfiguration config)
        {
            services.AddHttpClient(NameConsts.Himawari8, client =>
            {
                client.BaseAddress = new Uri($"https://rammb-slider.cira.colostate.edu/data/");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (m, c, a3, a4) => true
            }).AddPolicyHandler(GetRetryPolicy());

            services.AddHttpClient(NameConsts.Cloudinary, client =>
            {
                client.BaseAddress = new Uri($"https://res.cloudinary.com/{config["OssOptions:UserName"]}/image/fetch/");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (m, c, a3, a4) => true
            }).AddPolicyHandler(GetRetryPolicy());

            services.AddHttpClient(NameConsts.Qiqiuyun, client =>
            {
                client.BaseAddress = new Uri($"https://qiniu.com/{config["OssOptions:UserName"]}");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (m, c, a3, a4) => true
            }).AddPolicyHandler(GetRetryPolicy());

            services.AddHttpClient(NameConsts.DirectDownload, client =>
            {
                client.DefaultRequestHeaders
                    .Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.102 Safari/537.36 Edg/85.0.564.51");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (m, c, a3, a4) => true
            }).AddPolicyHandler(GetRetryPolicy());
        }
    }
}
