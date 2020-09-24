namespace EarthBackground
{
    partial class SettingForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.FolderBrowserDialog SavePathDialog;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.CheckBox CB_AutoStart;
        private System.Windows.Forms.Label l_sitellite;
        private System.Windows.Forms.ComboBox CB_Captor;
        private System.Windows.Forms.CheckBox CB_SetBackGround;
        private System.Windows.Forms.ComboBox CB_Downloader;
        private System.Windows.Forms.Button B_ChooseSavePath;
        private System.Windows.Forms.ComboBox CB_Resolution;
        private System.Windows.Forms.CheckBox CB_SaveWallpaper;
        private System.Windows.Forms.Label l_resolution;
        private System.Windows.Forms.Label l_path;
        private System.Windows.Forms.Label L_SavePath;
        private System.Windows.Forms.NumericUpDown MUD_Interval;
        private System.Windows.Forms.NumericUpDown MUD_Zoom;
        private System.Windows.Forms.Label l_interval;
        private System.Windows.Forms.Label l_zoom;
        private System.Windows.Forms.Label l_min;
        private System.Windows.Forms.Label l_percent;
        private System.Windows.Forms.Label l_downloader;
        private System.Windows.Forms.TextBox TB_Username;
        private System.Windows.Forms.TextBox TB_ApiKey;
        private System.Windows.Forms.Label l_username;
        private System.Windows.Forms.Label l_apisecret;
        private System.Windows.Forms.Label l_apikey;
        private System.Windows.Forms.TextBox TB_ApiSecret;
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingForm));
            this.SavePathDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.MUD_Zoom = new System.Windows.Forms.NumericUpDown();
            this.MUD_Interval = new System.Windows.Forms.NumericUpDown();
            this.CB_Resolution = new System.Windows.Forms.ComboBox();
            this.B_ChooseSavePath = new System.Windows.Forms.Button();
            this.L_SavePath = new System.Windows.Forms.Label();
            this.l_zoom = new System.Windows.Forms.Label();
            this.l_percent = new System.Windows.Forms.Label();
            this.l_min = new System.Windows.Forms.Label();
            this.l_interval = new System.Windows.Forms.Label();
            this.l_path = new System.Windows.Forms.Label();
            this.l_resolution = new System.Windows.Forms.Label();
            this.l_sitellite = new System.Windows.Forms.Label();
            this.CB_Captor = new System.Windows.Forms.ComboBox();
            this.CB_SaveWallpaper = new System.Windows.Forms.CheckBox();
            this.CB_SetBackGround = new System.Windows.Forms.CheckBox();
            this.CB_AutoStart = new System.Windows.Forms.CheckBox();
            this.TB_ApiSecret = new System.Windows.Forms.TextBox();
            this.TB_ApiKey = new System.Windows.Forms.TextBox();
            this.TB_Username = new System.Windows.Forms.TextBox();
            this.l_apikey = new System.Windows.Forms.Label();
            this.l_apisecret = new System.Windows.Forms.Label();
            this.l_username = new System.Windows.Forms.Label();
            this.l_downloader = new System.Windows.Forms.Label();
            this.CB_Downloader = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MUD_Zoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MUD_Interval)).BeginInit();
            this.SuspendLayout();
            // 
            // SavePathDialog
            // 
            this.SavePathDialog.RootFolder = System.Environment.SpecialFolder.History;
            // 
            // splitContainer1
            // 
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.MUD_Zoom);
            this.splitContainer1.Panel1.Controls.Add(this.MUD_Interval);
            this.splitContainer1.Panel1.Controls.Add(this.CB_Resolution);
            this.splitContainer1.Panel1.Controls.Add(this.B_ChooseSavePath);
            this.splitContainer1.Panel1.Controls.Add(this.L_SavePath);
            this.splitContainer1.Panel1.Controls.Add(this.l_zoom);
            this.splitContainer1.Panel1.Controls.Add(this.l_percent);
            this.splitContainer1.Panel1.Controls.Add(this.l_min);
            this.splitContainer1.Panel1.Controls.Add(this.l_interval);
            this.splitContainer1.Panel1.Controls.Add(this.l_path);
            this.splitContainer1.Panel1.Controls.Add(this.l_resolution);
            this.splitContainer1.Panel1.Controls.Add(this.l_sitellite);
            this.splitContainer1.Panel1.Controls.Add(this.CB_Captor);
            this.splitContainer1.Panel1.Controls.Add(this.CB_SaveWallpaper);
            this.splitContainer1.Panel1.Controls.Add(this.CB_SetBackGround);
            this.splitContainer1.Panel1.Controls.Add(this.CB_AutoStart);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.TB_ApiSecret);
            this.splitContainer1.Panel2.Controls.Add(this.TB_ApiKey);
            this.splitContainer1.Panel2.Controls.Add(this.TB_Username);
            this.splitContainer1.Panel2.Controls.Add(this.l_apikey);
            this.splitContainer1.Panel2.Controls.Add(this.l_apisecret);
            this.splitContainer1.Panel2.Controls.Add(this.l_username);
            this.splitContainer1.Panel2.Controls.Add(this.l_downloader);
            this.splitContainer1.Panel2.Controls.Add(this.CB_Downloader);
            // 
            // MUD_Zoom
            // 
            resources.ApplyResources(this.MUD_Zoom, "MUD_Zoom");
            this.MUD_Zoom.Name = "MUD_Zoom";
            // 
            // MUD_Interval
            // 
            this.MUD_Interval.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            resources.ApplyResources(this.MUD_Interval, "MUD_Interval");
            this.MUD_Interval.Maximum = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            this.MUD_Interval.Name = "MUD_Interval";
            // 
            // CB_Resolution
            // 
            this.CB_Resolution.DisplayMember = "Name";
            this.CB_Resolution.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CB_Resolution.FormattingEnabled = true;
            resources.ApplyResources(this.CB_Resolution, "CB_Resolution");
            this.CB_Resolution.Name = "CB_Resolution";
            this.CB_Resolution.ValueMember = "Value";
            // 
            // B_ChooseSavePath
            // 
            resources.ApplyResources(this.B_ChooseSavePath, "B_ChooseSavePath");
            this.B_ChooseSavePath.Name = "B_ChooseSavePath";
            this.B_ChooseSavePath.UseVisualStyleBackColor = true;
            this.B_ChooseSavePath.Click += new System.EventHandler(this.B_ChooseSavePath_Click);
            // 
            // L_SavePath
            // 
            resources.ApplyResources(this.L_SavePath, "L_SavePath");
            this.L_SavePath.Name = "L_SavePath";
            // 
            // l_zoom
            // 
            resources.ApplyResources(this.l_zoom, "l_zoom");
            this.l_zoom.Name = "l_zoom";
            // 
            // l_percent
            // 
            resources.ApplyResources(this.l_percent, "l_percent");
            this.l_percent.Name = "l_percent";
            // 
            // l_min
            // 
            resources.ApplyResources(this.l_min, "l_min");
            this.l_min.Name = "l_min";
            // 
            // l_interval
            // 
            resources.ApplyResources(this.l_interval, "l_interval");
            this.l_interval.Name = "l_interval";
            // 
            // l_path
            // 
            resources.ApplyResources(this.l_path, "l_path");
            this.l_path.Name = "l_path";
            // 
            // l_resolution
            // 
            resources.ApplyResources(this.l_resolution, "l_resolution");
            this.l_resolution.Name = "l_resolution";
            // 
            // l_sitellite
            // 
            resources.ApplyResources(this.l_sitellite, "l_sitellite");
            this.l_sitellite.Name = "l_sitellite";
            // 
            // CB_Captor
            // 
            this.CB_Captor.DisplayMember = "Name";
            this.CB_Captor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CB_Captor.FormattingEnabled = true;
            resources.ApplyResources(this.CB_Captor, "CB_Captor");
            this.CB_Captor.Name = "CB_Captor";
            this.CB_Captor.ValueMember = "Value";
            // 
            // CB_SaveWallpaper
            // 
            resources.ApplyResources(this.CB_SaveWallpaper, "CB_SaveWallpaper");
            this.CB_SaveWallpaper.Name = "CB_SaveWallpaper";
            this.CB_SaveWallpaper.UseVisualStyleBackColor = true;
            this.CB_SaveWallpaper.CheckedChanged += new System.EventHandler(this.CB_SaveWallpaper_CheckedChanged);
            // 
            // CB_SetBackGround
            // 
            resources.ApplyResources(this.CB_SetBackGround, "CB_SetBackGround");
            this.CB_SetBackGround.Name = "CB_SetBackGround";
            this.CB_SetBackGround.UseVisualStyleBackColor = true;
            // 
            // CB_AutoStart
            // 
            resources.ApplyResources(this.CB_AutoStart, "CB_AutoStart");
            this.CB_AutoStart.Name = "CB_AutoStart";
            this.CB_AutoStart.UseVisualStyleBackColor = true;
            // 
            // TB_ApiSecret
            // 
            resources.ApplyResources(this.TB_ApiSecret, "TB_ApiSecret");
            this.TB_ApiSecret.Name = "TB_ApiSecret";
            // 
            // TB_ApiKey
            // 
            resources.ApplyResources(this.TB_ApiKey, "TB_ApiKey");
            this.TB_ApiKey.Name = "TB_ApiKey";
            // 
            // TB_Username
            // 
            resources.ApplyResources(this.TB_Username, "TB_Username");
            this.TB_Username.Name = "TB_Username";
            // 
            // l_apikey
            // 
            resources.ApplyResources(this.l_apikey, "l_apikey");
            this.l_apikey.Name = "l_apikey";
            // 
            // l_apisecret
            // 
            resources.ApplyResources(this.l_apisecret, "l_apisecret");
            this.l_apisecret.Name = "l_apisecret";
            // 
            // l_username
            // 
            resources.ApplyResources(this.l_username, "l_username");
            this.l_username.Name = "l_username";
            // 
            // l_downloader
            // 
            resources.ApplyResources(this.l_downloader, "l_downloader");
            this.l_downloader.Name = "l_downloader";
            // 
            // CB_Downloader
            // 
            this.CB_Downloader.DisplayMember = "Name";
            this.CB_Downloader.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CB_Downloader.FormattingEnabled = true;
            resources.ApplyResources(this.CB_Downloader, "CB_Downloader");
            this.CB_Downloader.Name = "CB_Downloader";
            this.CB_Downloader.ValueMember = "Value";
            this.CB_Downloader.SelectedIndexChanged += new System.EventHandler(this.CB_Downloader_SelectedIndexChanged);
            // 
            // SettingForm
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.splitContainer1);
            this.Name = "SettingForm";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SettingForm_FormClosed);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MUD_Zoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MUD_Interval)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
    }
}