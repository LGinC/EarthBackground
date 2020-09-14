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

            services.AddHttpClient("himawari8");

            services.AddHttpClient(NameConsts.Cloudinary, client =>
            {
                client.BaseAddress = new Uri($"https://res.cloudinary.com/{config["OssOptions:UserName"]}/image/fetch/");
            });
           
            services.AddLogging(builder =>
            {
                builder.AddDebug();
            });
            //���������Ϊ����
            services.AddSingleton(typeof(MainForm));
            var serviceProvider = services.BuildServiceProvider();

            Application.Run(services.BuildServiceProvider().GetRequiredService<MainForm>());
        }

    }
}
