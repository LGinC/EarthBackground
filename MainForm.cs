using System;
using System.Drawing;
using System.Windows.Forms;
using EarthBackground.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EarthBackground
{
    public partial class MainForm : System.Windows.Forms.Form
    {
        private ILogger Logger;
        private IServiceProvider _provider;
        private IBackgroundSetter _backgroundSetter;
        System.Threading.Timer timer;

        public MainForm(ILogger<MainForm> logger, IServiceProvider provider, IBackgroudSetProvider backgroudSetProvider)
        {
            Logger = logger;
            _provider = provider;
            _backgroundSetter = backgroudSetProvider.GetSetter();
            InitializeComponent();
        }

        private async void Timer_Tick()
        {
            try
            {
                ICaptor provider = _provider.GetRequiredService<ICaptor>();
                Logger.LogInformation("已启动");

                provider.Downloader.SetTotal += t => BeginInvoke((Action)(() =>
                {
                    progressBar1.Maximum = t;
                    l_status.Text = "running";
                    l_status.ForeColor = Color.Green;
                    l_progress.Text = $"0/{t}";
                }));

                provider.Downloader.SetCurrentProgress += t => BeginInvoke((Action)(() => 
                {
                    progressBar1.Value = t;
                    if (t == progressBar1.Maximum)
                    {
                        l_status.Text = "complete";
                        l_status.ForeColor = Color.Black;
                        l_progress.Text = string.Empty;
                    }
                    l_progress.Text = $"{t}/{l_progress.Text.Split("/")[1]}";
                }));

                var image = await provider.GetImagePath();
                Logger.LogInformation($"壁纸已保存:{image}");
                await _backgroundSetter.SetBackgroudAsync(image);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                Logger.LogError(ex.StackTrace);
                MessageBox.Show(ex.Message);
            }
        }

        private  void B_start_Click(object sender, EventArgs e)
        {
            if (timer == null)
                timer = new System.Threading.Timer(e => Timer_Tick(), null, 0, 20 * 60000);
            else
                timer.Change(0, 20 * 6000);
        }
    }
}
