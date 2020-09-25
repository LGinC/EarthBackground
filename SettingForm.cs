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
            }
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

            var enable = (CB_Downloader.SelectedItem as NameValue<string>).Value switch
            {
                NameConsts.DirectDownload => false,
                NameConsts.Cloudinary => true,
                _ => true,
            };

            var extensionEnable = (CB_Downloader.SelectedItem as NameValue<string>).Value switch
            {
                NameConsts.DirectDownload => false,
                NameConsts.Cloudinary => false,
                NameConsts.Qiqiuyun => true,
                _ => true,
            };

            TB_Username.Enabled = enable;
            TB_ApiKey.Enabled = enable;
            TB_ApiSecret.Enabled = enable;
            TB_Zone.Enabled = extensionEnable;
            TB_Domain.Enabled = extensionEnable;
            TB_Bucket.Enabled = extensionEnable;
        }

        private async void SettingForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            capture.Captor = (CB_Captor.SelectedItem as NameValue<string>).Value;
            capture.Interval = (int)Math.Round(MUD_Interval.Value, 10);
            capture.Resolution = (CB_Resolution.SelectedItem as NameValue<Resolution>).Value;
            capture.SaveWallpaper = CB_SaveWallpaper.Checked;
            capture.SetWallpaper = CB_SetBackGround.Checked;
            capture.Zoom = Convert.ToInt32(MUD_Zoom.Value);
            if (oss.CloudName != (CB_Downloader.SelectedItem as NameValue<string>).Value)
            {
                oss.IsEnable = true;
                oss.CloudName = (CB_Downloader.SelectedItem as NameValue<string>).Value;
                oss.UserName = string.IsNullOrWhiteSpace(TB_Username.Text) ? oss.UserName : TB_Username.Text;
                oss.ApiKey = string.IsNullOrWhiteSpace(TB_ApiKey.Text) ? oss.ApiKey : TB_ApiKey.Text;
                oss.ApiSecret = string.IsNullOrWhiteSpace(TB_ApiSecret.Text) ? oss.ApiSecret : TB_ApiSecret.Text;
            }
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
