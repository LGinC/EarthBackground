using System;
using System.Drawing;
using System.Windows.Forms;
using EarthBackground.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EarthBackground
{
    public partial class MainForm : Form
    {
        private ILogger Logger;
        private IServiceProvider _provider;
        private IBackgroundSetter _backgroundSetter;
        System.Threading.Timer timer;
        private readonly IOptionsSnapshot<CaptureOption> _options;

        public MainForm(ILogger<MainForm> logger, 
            IServiceProvider provider, 
            IBackgroudSetProvider backgroudSetProvider,
            IOptionsSnapshot<CaptureOption> options)
        {
            Logger = logger;
            _provider = provider;
            _backgroundSetter = backgroudSetProvider.GetSetter();
            _options = options;
            InitializeComponent();
        }

        private async void Timer_Tick()
        {
            try
            {
                using ICaptor provider = _provider.GetRequiredService<ICaptor>();
                Logger.LogInformation("已启动");

                provider.Downloader.SetTotal += t => Invoke(() =>
                {
                    progressBar1.Maximum = t;
                    l_status.Text = "running";
                    l_status.ForeColor = Color.Green;
                    l_progress.Text = $"0/{t}";
                });

                provider.Downloader.SetCurrentProgress += t => Invoke(() => 
                {
                    progressBar1.Value = t;
                    if (t == progressBar1.Maximum)
                    {
                        l_status.Text = "complete";
                        l_status.ForeColor = Color.Black;
                        l_progress.Text = string.Empty;
                    }
                    else
                      l_progress.Text = $"{t}/{l_progress.Text.Split("/")[1]}";
                });


                Invoke(() =>
                {
                    B_start.Enabled = false;
                    B_stop.Enabled = true;
                });
                var image = await provider.GetImagePath();
                Logger.LogInformation($"壁纸已保存:{image}");
                Logger.LogInformation($"保存壁纸: {_options.Value.SetWallpaper}");
                if (_options.Value.SetWallpaper)
                {
                    await _backgroundSetter.SetBackgroudAsync(image);
                }
                Invoke(() =>
                {
                    B_start.Enabled = true;
                    B_stop.Enabled = false;
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                Logger.LogError(ex.StackTrace);
                Invoke(() =>
                {
                    l_status.Text = "wait for run";
                    l_status.ForeColor = Color.Black;
                    l_progress.Text = string.Empty;
                    notifyIcon1.Visible = true;
                    notifyIcon1.ShowBalloonTip(3000, "图片下载失败", ex.Message, ToolTipIcon.Warning);
                    B_start.Enabled = true;
                    B_stop.Enabled = false;
                });
            }
        }

        private void Invoke(Action action)
        {
            BeginInvoke(action);
        }

        private  void B_start_Click(object sender, EventArgs e)
        {
            if (timer == null)
                timer = new System.Threading.Timer(e => Timer_Tick(), null, 0, 20 * 60000);
            else
                timer.Change(0, 20 * 6000);
        }

        private void B_stop_Click(object sender, EventArgs e)
        {
            
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            Show();
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
            BringToFront();
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            if(WindowState != FormWindowState.Minimized)
            {
                return;
            }

            ShowInTaskbar = false;
            Hide();
            notifyIcon1.Visible = true;
        }
    }
}
