using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using EarthBackground.Oss;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EarthBackground
{
    public partial class SettingForm : Form
    {
        private readonly CaptureOption capture;
        private readonly OssOption oss;
        private readonly IConfigureSaver configureSaver;
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingForm));
        readonly CultureInfo current;
        public SettingForm(
            IServiceProvider serviceProvider,
            IOptionsSnapshot<CaptureOption> captureOption,
            IOptionsSnapshot<OssOption> ossOption,
            IConfigureSaver saver)
        {
            InitializeComponent();
            current = Thread.CurrentThread.CurrentUICulture;
            configureSaver = saver;
            capture = captureOption.Value;
            oss = ossOption.Value;
            var captors = serviceProvider.GetServices<ICaptor>().Select(s => new NameValue<string>(L(s.ProviderName), s.ProviderName)).ToArray();
            CB_Captor.Items.AddRange(captors);
            if (captureOption.Value.Captor.IsNullOrEmpty())
            {
                CB_Captor.SelectedItem = captors.First(c=> c.Value == capture.Captor);
            }
            else
            {
                CB_Captor.SelectedIndex = 0;
            }
            CB_AutoStart.Checked = capture.AutoStart;
            CB_SetBackGround.Checked = capture.SetWallpaper;
            CB_SaveWallpaper.Checked = capture.SaveWallpaper;
            L_SavePath.Text = capture.WallpaperFolder;
            var resolutionArray = GetResolutions().ToArray();
            CB_Resolution.Items.AddRange(resolutionArray);
            CB_Resolution.SelectedItem = resolutionArray.First(c => c.Value == capture.Resolution);
            MUD_Zoom.Value = capture.Zoom;
            MUD_Interval.Value = capture.Interval;
            B_ChooseSavePath.Enabled = capture.SaveWallpaper;

            var downloaders = serviceProvider.GetServices<IOssDownloader>().Select(s => new NameValue<string>(L(s.ProviderName), s.ProviderName)).ToArray();
            CB_Downloader.Items.AddRange(downloaders);
            if (!oss.CloudName.IsNullOrEmpty() && oss.IsEnable)
            {
                CB_Downloader.SelectedItem = downloaders.First(d => d.Value == oss.CloudName);
            }
            else
            {
                CB_Downloader.SelectedIndex = 0;
            }

            if ((CB_Downloader.SelectedItem as NameValue<string>).Value == NameConsts.DirectDownload)
            {
                TB_Username.Enabled = false;
                TB_ApiKey.Enabled = false;
                TB_ApiSecret.Enabled = false;
            }
            else
            {
                TB_Username.Text = oss.UserName;
                TB_ApiKey.Text = oss.ApiKey;
                TB_ApiSecret.Text = oss.ApiSecret;
                TB_Domain.Text = oss.Domain;
                TB_Bucket.Text = oss.Bucket;

                var zones = GetZones(oss.CloudName);
                CB_Zone.Items.Clear();
                CB_Zone.Items.AddRange(zones);
                if (!string.IsNullOrWhiteSpace(oss.Zone))
                {
                    var zone = zones.FirstOrDefault(z => z.Value == oss.Zone);
                    if(zone != null)
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
                NameConsts.Qiqiuyun => new[] { "z0", "z1", "z2", "na0", "as0" },
                _ => Array.Empty<string>(),
            };

            return zones.IsNullOrEmpty() ? Array.Empty<NameValue<string>>() :
                zones.Select(z => new NameValue<string>(L(z), z)).ToArray();
        }

        private string L(string key) => resources.GetString(key, current);

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
                capture.SavePath = SavePathDialog.SelectedPath;
                L_SavePath.Text = capture.SavePath;
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

            string cloud = (CB_Downloader.SelectedItem as NameValue<string>).Value;
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
                default:
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
            capture.Captor = (CB_Captor.SelectedItem as NameValue<string>).Value;
            capture.Interval = (int)Math.Round(MUD_Interval.Value, 10);
            capture.Resolution = (CB_Resolution.SelectedItem as NameValue<Resolution>).Value;
            capture.SaveWallpaper = CB_SaveWallpaper.Checked;
            capture.SetWallpaper = CB_SetBackGround.Checked;
            capture.Zoom = Convert.ToInt32(MUD_Zoom.Value);
            oss.IsEnable = true;
            oss.CloudName = (CB_Downloader.SelectedItem as NameValue<string>).Value;
            oss.UserName = string.IsNullOrWhiteSpace(TB_Username.Text) ? oss.UserName : TB_Username.Text;
            oss.ApiKey = string.IsNullOrWhiteSpace(TB_ApiKey.Text) ? oss.ApiKey : TB_ApiKey.Text;
            oss.ApiSecret = string.IsNullOrWhiteSpace(TB_ApiSecret.Text) ? oss.ApiSecret : TB_ApiSecret.Text;
            oss.Bucket = string.IsNullOrWhiteSpace(TB_Bucket.Text) ? oss.Bucket : TB_Bucket.Text;
            oss.Domain = string.IsNullOrWhiteSpace(TB_Domain.Text) ? oss.Domain :
                (!TB_Domain.Text.Contains("http://") && !TB_Domain.Text.Contains("https://")) ? $"http://{TB_Domain.Text}" : TB_Domain.Text;
            var selectZone = (CB_Zone.SelectedItem as NameValue<string>)?.Value;
            oss.Zone = string.IsNullOrWhiteSpace(selectZone) ? oss.Zone : selectZone;
            await configureSaver.SaveAsync(capture, oss);
        }
    }

    public class NameValue<T>
    {
        public string Name { get; set; }

        public T Value { get; set; }

        public NameValue(string name, T value)
        {
            Name = name;
            Value = value;
        }
    }
}
