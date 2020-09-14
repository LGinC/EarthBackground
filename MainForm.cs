using System;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace EarthBackground
{
    public partial class MainForm : Form
    {
        private ILogger Logger;
        private ICaptor _provider;

        public MainForm(ILogger<MainForm> logger, ICaptor pictureProvider)
        {
            Logger = logger;
            _provider = pictureProvider;
            InitializeComponent();
        }

        private async void B_start_Click(object sender, EventArgs e)
        {
            try
            {
                B_start.Enabled = false;
                Logger.LogInformation("已启动");
                var image = await _provider.GetImagePath();
                Logger.LogInformation($"壁纸已保存:{image}");
                B_start.Enabled = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.StackTrace);
                MessageBox.Show(ex.Message);
                B_start.Enabled = true;
            }
        }
    }
}
