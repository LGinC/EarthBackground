using System;
using System.Net.Http;
using System.Windows.Forms;
using EarthBackground.Background;
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
            //������ö�ȡ
            var config = new ConfigurationBuilder().AddJsonFile(ConfigFile, optional: true, reloadOnChange: true)
                .Build();

            //���DI
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSingleton(config);
            services.Configure<CaptureOption>(config.GetSection("CaptureOptions"));
            services.Configure<OssOption>(config.GetSection("OssOptions"));
            services.AddTransient<IConfigureSaver, ConfigureSaver>();

            //ע��ץȡ��
            services.AddTransient<ICaptorProvider, CaptorProvider>();
            services.AddTransient<ICaptor, Himawari8Captor>();

            //ע��oss
            services.AddTransient<IOssProvider, OssProvider>();
            services.AddTransient<IOssDownloader, DirectDownloader>();
            services.AddTransient<IOssDownloader, CloudinaryDownloader>();
            services.AddTransient<IOssDownloader, QiniuDownloader>();

            //ע���ֽ������
            services.AddTransient<IBackgroudSetProvider, BackgroudSetProvider>();
            services.AddTransient<IBackgroundSetter, WindowsBackgroudSetter>();

            services.AddHttpClient(NameConsts.Himawari8, client =>
            {
                client.BaseAddress = new Uri("https://himawari8-dl.nict.go.jp/himawari8/");
            }).ConfigurePrimaryHttpMessageHandler(builder => new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, a3, a4) => true }); 

            services.AddHttpClient(NameConsts.Cloudinary, client =>
            {
                client.BaseAddress = new Uri($"https://res.cloudinary.com/{config["OssOptions:UserName"]}/image/fetch/");
            }).ConfigurePrimaryHttpMessageHandler(builder => new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, a3, a4) => true });

            services.AddHttpClient(NameConsts.Qiqiuyun, client =>
            {
                client.BaseAddress = new Uri($"https://qiniu.com/{config["OssOptions:UserName"]}");
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Qiniu", "");
            }).ConfigurePrimaryHttpMessageHandler(builder => new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, a3, a4) => true });

            services.AddLogging(builder =>
            {
                builder.AddDebug();
            });
            //���������Ϊ����
            services.AddSingleton(typeof(MainForm));
            return services.BuildServiceProvider();
        }

    }
}
