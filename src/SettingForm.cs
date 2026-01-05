using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using EarthBackground.Oss;
using Microsoft.Extensions.Options;

namespace EarthBackground
{
    public partial class SettingForm : Form
    {
        private readonly CaptureOption _capture;
        private readonly OssOption _oss;
        private readonly IConfigureSaver _configureSaver;
        private readonly System.ComponentModel.ComponentResourceManager _resources = new(typeof(SettingForm));
        private readonly CultureInfo _current;
        private readonly EarthBackground.Background.WallpaperService _wallpaperService;

        public SettingForm(
            IOptionsSnapshot<CaptureOption> captureOption,
            IOptionsSnapshot<OssOption> ossOption,
            IConfigureSaver saver,
            EarthBackground.Background.WallpaperService wallpaperService)
        {
            InitializeComponent();
            _current = Thread.CurrentThread.CurrentUICulture;
            _configureSaver = saver;
            _wallpaperService = wallpaperService ?? throw new ArgumentNullException(nameof(wallpaperService));
            _capture = captureOption.Value;
            _oss = ossOption.Value;
            var captors = NameConsts.CaptorNames.Select(s => new NameValue<string>(L(s), s)).ToArray();
            CB_Captor.Items.AddRange(captors);
            if (!captureOption.Value.Captor.IsNullOrEmpty())
            {
                CB_Captor.SelectedItem = captors.First(c => c.Value == _capture.Captor);
            }
            else
            {
                CB_Captor.SelectedIndex = 0;
            }
            CB_AutoStart.Checked = _capture.AutoStart;
            CB_SetBackGround.Checked = _capture.SetWallpaper;
            CB_SaveWallpaper.Checked = _capture.SaveWallpaper;



            L_SavePath.Text = _capture.WallpaperFolder;
            var resolutionArray = GetResolutions().ToArray();
            CB_Resolution.Items.AddRange(resolutionArray);
            CB_Resolution.SelectedItem = resolutionArray.First(c => c.Value == _capture.Resolution);
            MUD_Zoom.Value = _capture.Zoom;
            MUD_Interval.Value = _capture.Interval;
            B_ChooseSavePath.Enabled = _capture.SaveWallpaper;

            var downloaders = NameConsts.DownloaderNames.Select(s => new NameValue<string>(L(s), s)).ToArray();
            CB_Downloader.Items.AddRange(downloaders);
            if (!_oss.CloudName.IsNullOrEmpty() && _oss.IsEnable)
            {
                CB_Downloader.SelectedItem = downloaders.First(d => d.Value == _oss.CloudName);
            }
            else
            {
                CB_Downloader.SelectedIndex = 0;
            }

            if ((CB_Downloader.SelectedItem as NameValue<string>)?.Value == NameConsts.DirectDownload)
            {
                TB_Username.Enabled = false;
                TB_ApiKey.Enabled = false;
                TB_ApiSecret.Enabled = false;
            }
            else
            {
                TB_Username.Text = _oss.UserName;
                TB_ApiKey.Text = _oss.ApiKey;
                TB_ApiSecret.Text = _oss.ApiSecret;
                TB_Domain.Text = _oss.Domain;
                TB_Bucket.Text = _oss.Bucket;

                var zones = GetZones(_oss.CloudName);
                CB_Zone.Items.Clear();
                CB_Zone.Items.AddRange(zones);
                if (!string.IsNullOrWhiteSpace(_oss.Zone))
                {
                    var zone = zones.FirstOrDefault(z => z.Value == _oss.Zone);
                    if (zone != null)
                    {
                        CB_Zone.SelectedItem = zone;
                    }
                }
            }
        }

        private NameValue<string>[] GetZones(string cloudName)
        {
            var zones = cloudName switch
            {
                NameConsts.Qiqiuyun => ["z0", "z1", "z2", "na0", "as0"],
                _ => Array.Empty<string>(),
            };

            return zones.IsNullOrEmpty() ? [] :
                [.. zones.Select(z => new NameValue<string>(L(z), z))];
        }

        private string L(string key) => _resources.GetString(key, _current) ?? key;



        /// <summary>
        /// 绘制设置图标
        /// </summary>
        private void pictureSettings_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var rect = new Rectangle(5, 5, 30, 30);
            var center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

            // 绘制齿轮外圈
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillEllipse(brush, rect);
            }

            // 绘制齿轮齿
            using (var pen = new Pen(Color.FromArgb(52, 152, 219), 2))
            {
                for (int i = 0; i < 8; i++)
                {
                    var angle = i * 45 * Math.PI / 180;
                    var x1 = center.X + (int)(12 * Math.Cos(angle));
                    var y1 = center.Y + (int)(12 * Math.Sin(angle));
                    var x2 = center.X + (int)(16 * Math.Cos(angle));
                    var y2 = center.Y + (int)(16 * Math.Sin(angle));
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
            }

            // 绘制中心圆
            var innerRect = new Rectangle(center.X - 6, center.Y - 6, 12, 12);
            using (var brush = new SolidBrush(Color.FromArgb(52, 152, 219)))
            {
                g.FillEllipse(brush, innerRect);
            }
        }

        private IEnumerable<NameValue<Resolution>> GetResolutions()
        {
            foreach (Resolution resolution in Enum.GetValues(typeof(Resolution)))
            {
                yield return new NameValue<Resolution>(resolution.GetName(), resolution);
            }
        }

        private void B_ChooseSavePath_Click(object sender, EventArgs e)
        {
            if (SavePathDialog.ShowDialog() == DialogResult.OK)
            {
                _capture.SavePath = SavePathDialog.SelectedPath;
                L_SavePath.Text = _capture.SavePath;
            }
        }

        private void CB_SaveWallpaper_CheckedChanged(object sender, EventArgs e)
        {
            B_ChooseSavePath.Enabled = CB_SaveWallpaper.Checked;
        }

        private void CB_Downloader_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CB_Downloader.SelectedItem == null)
            {
                return;
            }

            string cloud = (CB_Downloader.SelectedItem as NameValue<string>)?.Value;
            switch (cloud)
            {
                case NameConsts.DirectDownload:
                    DirectDownloadSetting();
                    break;
                case NameConsts.Cloudinary:
                    CloudinarySetting();
                    break;
                case NameConsts.Qiqiuyun:
                    QiniuSetting(cloud);
                    break;
            }
        }

        private void QiniuSetting(string cloud)
        {
            TB_Username.Enabled = false;
            TB_ApiKey.Enabled = true;
            TB_ApiSecret.Enabled = true;
            CB_Zone.Enabled = true;
            TB_Domain.Enabled = true;
            TB_Bucket.Enabled = true;

            CB_Zone.Items.Clear();
            CB_Zone.Items.AddRange(GetZones(cloud));
        }

        private void CloudinarySetting()
        {
            TB_Username.Enabled = true;
            TB_ApiKey.Enabled = true;
            TB_ApiSecret.Enabled = true;
            CB_Zone.Enabled = false;
            TB_Domain.Enabled = false;
            TB_Bucket.Enabled = false;
        }

        private void DirectDownloadSetting()
        {
            TB_Username.Enabled = false;
            TB_ApiKey.Enabled = false;
            TB_ApiSecret.Enabled = false;
            CB_Zone.Enabled = false;
            TB_Domain.Enabled = false;
            TB_Bucket.Enabled = false;
        }

        private async void SettingForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _capture.Captor = (CB_Captor.SelectedItem as NameValue<string>)!.Value;
            _capture.Interval = (int)MUD_Interval.Value;//(int)Math.Round(MUD_Interval.Value, 10);
            _capture.Resolution = (CB_Resolution.SelectedItem as NameValue<Resolution>)!.Value;
            _capture.SaveWallpaper = CB_SaveWallpaper.Checked;
            _capture.SetWallpaper = CB_SetBackGround.Checked;
            _capture.Zoom = Convert.ToInt32(MUD_Zoom.Value);
            _oss.IsEnable = true;
            _oss.CloudName = (CB_Downloader.SelectedItem as NameValue<string>)!.Value;
            _oss.UserName = string.IsNullOrWhiteSpace(TB_Username.Text) ? _oss.UserName : TB_Username.Text;
            _oss.ApiKey = string.IsNullOrWhiteSpace(TB_ApiKey.Text) ? _oss.ApiKey : TB_ApiKey.Text;
            _oss.ApiSecret = string.IsNullOrWhiteSpace(TB_ApiSecret.Text) ? _oss.ApiSecret : TB_ApiSecret.Text;
            _oss.Bucket = string.IsNullOrWhiteSpace(TB_Bucket.Text) ? _oss.Bucket : TB_Bucket.Text;
            _oss.Domain = string.IsNullOrWhiteSpace(TB_Domain.Text) ? _oss.Domain :
                (!TB_Domain.Text.Contains("http://") && !TB_Domain.Text.Contains("https://")) ? $"http://{TB_Domain.Text}" : TB_Domain.Text;
            var selectZone = (CB_Zone.SelectedItem as NameValue<string>)?.Value;
            _oss.Zone = string.IsNullOrWhiteSpace(selectZone) ? _oss.Zone : selectZone;
            await _configureSaver.SaveAsync(_capture, _oss);
            if (File.Exists(NameConsts.ImageIdPath)) File.Delete(NameConsts.ImageIdPath);
            _wallpaperService.TriggerUpdate();
        }
    }

    public class NameValue<T>(string name, T value)
    {
        public string Name { get; set; } = name;

        public T Value { get; set; } = value;
    }
}
