using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EarthBackground.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EarthBackground
{
    public partial class MainForm : Form
    {
        private readonly ILogger Logger;
        private readonly IServiceProvider _provider;
        private readonly IBackgroundSetter _backgroundSetter;
        System.Threading.Timer _timer;
        readonly System.ComponentModel.ComponentResourceManager _resources = new(typeof(MainForm));
        private readonly IOptionsSnapshot<CaptureOption> _options;
        readonly CultureInfo _current;
        private readonly TaskScheduler _scheduler;
        private int _ossFetchCount;

        private readonly object lockObject = new object();

        public MainForm(ILogger<MainForm> logger, 
            IServiceProvider provider, 
            IBackgroudSetProvider backgroundSetProvider,
            IOptionsSnapshot<CaptureOption> options)
        {
            Logger = logger;
            _provider = provider;
            _backgroundSetter = backgroundSetProvider.GetSetter();
            _options = options;
            InitializeComponent();
            _current = Thread.CurrentThread.CurrentUICulture;
            _scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        private string L(string key) => _resources.GetString(key, _current);

        private async void Timer_Tick()
        {
            try
            {
                using ICaptor provider = _provider.GetRequiredService<ICaptor>();
                Logger.LogInformation("已启动");
                provider.Downloader.SetTotal += t => Task.Factory.StartNew(()=>
                {
                    progressBar1.Maximum = t;
                    l_status.Text = L("running");
                    l_status.ForeColor = Color.Green;
                    l_progress.Text = $"0/{t}";
                }, CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, _scheduler);

                provider.Downloader.SetCurrentProgress += () => Task.Factory.StartNew(()=>
                {
                    lock (lockObject)
                    {
                        progressBar1.Value++;
                    }
                    if (progressBar1.Value == progressBar1.Maximum)
                    {
                        l_status.Text = L("complete");
                        l_status.ForeColor = Color.Black;
                        l_progress.Text = string.Empty;
                    }
                    else
                      l_progress.Text = $"{progressBar1.Value}/{l_progress.Text.Split("/")[1]}";
                }, CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, _scheduler);

                await Task.Factory.StartNew(() =>
                {
                    B_start.Enabled = false;
                    B_stop.Enabled = true;
                }, CancellationToken.None, TaskCreationOptions.AttachedToParent, _scheduler);
                var image = await provider.GetImagePath();
                _ossFetchCount++;
                if(_ossFetchCount > 3)
                {
                    _ossFetchCount = 0;
                    await provider.ResetAsync();
                }
                Logger.LogInformation($"壁纸已保存:{image}");
                if (_options.Value.SetWallpaper)
                {
                    await _backgroundSetter.SetBackgroundAsync(image);
                }
                await Task.Factory.StartNew(() =>
                {
                    B_start.Enabled = true;
                    B_stop.Enabled = false;
                }, CancellationToken.None, TaskCreationOptions.AttachedToParent, _scheduler);

                if (!_options.Value.SaveWallpaper) return;
                
                var info = new FileInfo(image);
                if (!Directory.Exists(_options.Value.SavePath))
                {
                    Directory.CreateDirectory(_options.Value.SavePath);
                }

                string wallpaper = Path.Combine(_options.Value.SavePath, info.Name);
                if (wallpaper == image) return;
                if (File.Exists(wallpaper))
                {
                    File.Delete(wallpaper);
                }
                File.Copy(image, wallpaper);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                Logger.LogError(ex.StackTrace);
                await Task.Factory.StartNew(
                () =>
                {
                    l_status.Text = L("wait for run");
                    l_status.ForeColor = Color.Black;
                    l_progress.Text = string.Empty;
                    notifyIcon1.Visible = true;
                    notifyIcon1.ShowBalloonTip(3000, "图片下载失败", ex.Message, ToolTipIcon.Warning);
                    B_start.Enabled = true;
                    B_stop.Enabled = false;
                }, CancellationToken.None, TaskCreationOptions.None, _scheduler);
            }
        }

        private  void B_start_Click(object sender, EventArgs e)
        {
            if (_timer == null)
                _timer = new System.Threading.Timer(_ => Timer_Tick(), null, TimeSpan.Zero, TimeSpan.FromMinutes(_options.Value.Interval));
            else
                _timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(_options.Value.Interval));
        }

        private void B_stop_Click(object sender, EventArgs e)
        {
            _timer?.Dispose();
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
            settingForm.FormClosed += (_, _) => Show();
        }
    }
}
