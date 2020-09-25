using System;
using System.Drawing;
using System.Globalization;
using System.Threading;
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
        System.Threading.Timer _timer;
        System.ComponentModel.ComponentResourceManager _resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        private readonly IOptionsSnapshot<CaptureOption> _options;
        readonly CultureInfo _current;
        private int _ossFetchCount = 0;

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
            _current = Thread.CurrentThread.CurrentUICulture;
        }

        private string L(string key) => _resources.GetString(key, _current);

        private async void Timer_Tick()
        {
            try
            {
                using ICaptor provider = _provider.GetRequiredService<ICaptor>();
                Logger.LogInformation("已启动");
                provider.Downloader.SetTotal += t => Invoke(() =>
                {
                    progressBar1.Maximum = t;
                    l_status.Text = L("running");
                    l_status.ForeColor = Color.Green;
                    l_progress.Text = $"0/{t}";
                });

                provider.Downloader.SetCurrentProgress += t => Invoke(() => 
                {
                    progressBar1.Value = t;
                    if (t == progressBar1.Maximum)
                    {
                        l_status.Text = L("complete");
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
                _ossFetchCount++;
                if(_ossFetchCount > 3)
                {
                    _ossFetchCount = 0;
                    await provider.ResetAsync();
                }
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
                    l_status.Text = L("wait for run");
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
            if (_timer == null)
                _timer = new System.Threading.Timer(e => Timer_Tick(), null, TimeSpan.Zero, TimeSpan.FromMinutes(_options.Value.Interval));
            else
                _timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(_options.Value.Interval));
        }

        private void B_stop_Click(object sender, EventArgs e)
        {
            _timer.Dispose();
            _timer = null;
            progressBar1.Value = 0;
            l_status.Text = L("wait for run");
            l_status.ForeColor = Color.Black;
            l_progress.Text = string.Empty;
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

        private void B_settings_Click(object sender, EventArgs e)
        {
            Hide();
            var settingForm = _provider.GetRequiredService<SettingForm>();
            settingForm.Show();
            settingForm.FormClosed += (s, e) => Invoke(() => Show());
        }
    }
}
