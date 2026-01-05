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
            tlpCapture = new System.Windows.Forms.TableLayoutPanel();
            CB_AutoStart = new System.Windows.Forms.CheckBox();
            l_sitellite = new System.Windows.Forms.Label();
            CB_Captor = new System.Windows.Forms.ComboBox();
            l_resolution = new System.Windows.Forms.Label();
            CB_Resolution = new System.Windows.Forms.ComboBox();
            l_interval = new System.Windows.Forms.Label();
            flpInterval = new System.Windows.Forms.FlowLayoutPanel();
            MUD_Interval = new System.Windows.Forms.NumericUpDown();
            l_min = new System.Windows.Forms.Label();
            CB_SetBackGround = new System.Windows.Forms.CheckBox();
            l_zoom = new System.Windows.Forms.Label();
            flpZoom = new System.Windows.Forms.FlowLayoutPanel();
            MUD_Zoom = new System.Windows.Forms.NumericUpDown();
            l_percent = new System.Windows.Forms.Label();
            CB_SaveWallpaper = new System.Windows.Forms.CheckBox();
            l_path = new System.Windows.Forms.Label();
            B_ChooseSavePath = new System.Windows.Forms.Button();
            L_SavePath = new System.Windows.Forms.Label();
            tlpDownload = new System.Windows.Forms.TableLayoutPanel();
            l_downloader = new System.Windows.Forms.Label();
            CB_Downloader = new System.Windows.Forms.ComboBox();
            l_username = new System.Windows.Forms.Label();
            TB_Username = new System.Windows.Forms.TextBox();
            l_apikey = new System.Windows.Forms.Label();
            TB_ApiKey = new System.Windows.Forms.TextBox();
            l_apisecret = new System.Windows.Forms.Label();
            TB_ApiSecret = new System.Windows.Forms.TextBox();
            l_domain = new System.Windows.Forms.Label();
            TB_Domain = new System.Windows.Forms.TextBox();
            l_bucket = new System.Windows.Forms.Label();
            TB_Bucket = new System.Windows.Forms.TextBox();
            l_zone = new System.Windows.Forms.Label();
            CB_Zone = new System.Windows.Forms.ComboBox();
            pnlHeader = new System.Windows.Forms.Panel();
            lblHeaderTitle = new System.Windows.Forms.Label();
            pictureBoxSettings = new System.Windows.Forms.PictureBox();
            SavePathDialog = new System.Windows.Forms.FolderBrowserDialog();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            tlpCapture.SuspendLayout();
            flpInterval.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)MUD_Interval).BeginInit();
            flpZoom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)MUD_Zoom).BeginInit();
            tlpDownload.SuspendLayout();
            pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxSettings).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // tlpCapture
            // 
            resources.ApplyResources(tlpCapture, "tlpCapture");
            tlpCapture.BackColor = System.Drawing.Color.White;
            tlpCapture.Controls.Add(CB_AutoStart, 0, 0);
            tlpCapture.Controls.Add(l_sitellite, 0, 1);
            tlpCapture.Controls.Add(CB_Captor, 1, 1);
            tlpCapture.Controls.Add(l_resolution, 0, 2);
            tlpCapture.Controls.Add(CB_Resolution, 1, 2);
            tlpCapture.Controls.Add(l_interval, 0, 3);
            tlpCapture.Controls.Add(flpInterval, 1, 3);
            tlpCapture.Controls.Add(CB_SetBackGround, 2, 0);
            tlpCapture.Controls.Add(l_zoom, 2, 1);
            tlpCapture.Controls.Add(flpZoom, 3, 1);
            tlpCapture.Controls.Add(CB_SaveWallpaper, 2, 2);
            tlpCapture.Controls.Add(l_path, 2, 3);
            tlpCapture.Controls.Add(B_ChooseSavePath, 3, 3);
            tlpCapture.Controls.Add(L_SavePath, 0, 4);
            tlpCapture.Name = "tlpCapture";
            // 
            // CB_AutoStart
            // 
            resources.ApplyResources(CB_AutoStart, "CB_AutoStart");
            tlpCapture.SetColumnSpan(CB_AutoStart, 2);
            CB_AutoStart.Cursor = System.Windows.Forms.Cursors.Hand;
            CB_AutoStart.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            CB_AutoStart.Name = "CB_AutoStart";
            CB_AutoStart.UseVisualStyleBackColor = true;
            // 
            // l_sitellite
            // 
            resources.ApplyResources(l_sitellite, "l_sitellite");
            l_sitellite.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            l_sitellite.Name = "l_sitellite";
            // 
            // CB_Captor
            // 
            resources.ApplyResources(CB_Captor, "CB_Captor");
            CB_Captor.BackColor = System.Drawing.Color.White;
            CB_Captor.DisplayMember = "Name";
            CB_Captor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            CB_Captor.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            CB_Captor.FormattingEnabled = true;
            CB_Captor.Name = "CB_Captor";
            CB_Captor.ValueMember = "Value";
            // 
            // l_resolution
            // 
            resources.ApplyResources(l_resolution, "l_resolution");
            l_resolution.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            l_resolution.Name = "l_resolution";
            // 
            // CB_Resolution
            // 
            resources.ApplyResources(CB_Resolution, "CB_Resolution");
            CB_Resolution.BackColor = System.Drawing.Color.White;
            CB_Resolution.DisplayMember = "Name";
            CB_Resolution.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            CB_Resolution.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            CB_Resolution.FormattingEnabled = true;
            CB_Resolution.Name = "CB_Resolution";
            CB_Resolution.ValueMember = "Value";
            // 
            // l_interval
            // 
            resources.ApplyResources(l_interval, "l_interval");
            l_interval.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            l_interval.Name = "l_interval";
            // 
            // flpInterval
            // 
            resources.ApplyResources(flpInterval, "flpInterval");
            flpInterval.Controls.Add(MUD_Interval);
            flpInterval.Controls.Add(l_min);
            flpInterval.Name = "flpInterval";
            // 
            // MUD_Interval
            // 
            MUD_Interval.BackColor = System.Drawing.Color.White;
            MUD_Interval.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(MUD_Interval, "MUD_Interval");
            MUD_Interval.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            MUD_Interval.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            MUD_Interval.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
            MUD_Interval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            MUD_Interval.Name = "MUD_Interval";
            MUD_Interval.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // l_min
            // 
            resources.ApplyResources(l_min, "l_min");
            l_min.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            l_min.Name = "l_min";
            // 
            // CB_SetBackGround
            // 
            resources.ApplyResources(CB_SetBackGround, "CB_SetBackGround");
            tlpCapture.SetColumnSpan(CB_SetBackGround, 2);
            CB_SetBackGround.Cursor = System.Windows.Forms.Cursors.Hand;
            CB_SetBackGround.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            CB_SetBackGround.Name = "CB_SetBackGround";
            CB_SetBackGround.UseVisualStyleBackColor = true;
            // 
            // l_zoom
            // 
            resources.ApplyResources(l_zoom, "l_zoom");
            l_zoom.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            l_zoom.Name = "l_zoom";
            // 
            // flpZoom
            // 
            resources.ApplyResources(flpZoom, "flpZoom");
            flpZoom.Controls.Add(MUD_Zoom);
            flpZoom.Controls.Add(l_percent);
            flpZoom.Name = "flpZoom";
            // 
            // MUD_Zoom
            // 
            MUD_Zoom.BackColor = System.Drawing.Color.White;
            MUD_Zoom.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(MUD_Zoom, "MUD_Zoom");
            MUD_Zoom.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            MUD_Zoom.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            MUD_Zoom.Name = "MUD_Zoom";
            MUD_Zoom.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // l_percent
            // 
            resources.ApplyResources(l_percent, "l_percent");
            l_percent.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            l_percent.Name = "l_percent";
            // 
            // CB_SaveWallpaper
            // 
            resources.ApplyResources(CB_SaveWallpaper, "CB_SaveWallpaper");
            tlpCapture.SetColumnSpan(CB_SaveWallpaper, 2);
            CB_SaveWallpaper.Cursor = System.Windows.Forms.Cursors.Hand;
            CB_SaveWallpaper.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            CB_SaveWallpaper.Name = "CB_SaveWallpaper";
            CB_SaveWallpaper.UseVisualStyleBackColor = true;
            CB_SaveWallpaper.CheckedChanged += CB_SaveWallpaper_CheckedChanged;
            // 
            // l_path
            // 
            resources.ApplyResources(l_path, "l_path");
            l_path.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            l_path.Name = "l_path";
            // 
            // B_ChooseSavePath
            // 
            resources.ApplyResources(B_ChooseSavePath, "B_ChooseSavePath");
            B_ChooseSavePath.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            B_ChooseSavePath.FlatAppearance.BorderSize = 0;
            B_ChooseSavePath.ForeColor = System.Drawing.Color.White;
            B_ChooseSavePath.Name = "B_ChooseSavePath";
            B_ChooseSavePath.UseVisualStyleBackColor = false;
            B_ChooseSavePath.Click += B_ChooseSavePath_Click;
            // 
            // L_SavePath
            // 
            resources.ApplyResources(L_SavePath, "L_SavePath");
            tlpCapture.SetColumnSpan(L_SavePath, 4);
            L_SavePath.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            L_SavePath.Name = "L_SavePath";
            // 
            // tlpDownload
            // 
            resources.ApplyResources(tlpDownload, "tlpDownload");
            tlpDownload.BackColor = System.Drawing.Color.White;
            tlpDownload.Controls.Add(l_downloader, 0, 0);
            tlpDownload.Controls.Add(CB_Downloader, 1, 0);
            tlpDownload.Controls.Add(l_username, 0, 1);
            tlpDownload.Controls.Add(TB_Username, 1, 1);
            tlpDownload.Controls.Add(l_apikey, 0, 2);
            tlpDownload.Controls.Add(TB_ApiKey, 1, 2);
            tlpDownload.Controls.Add(l_apisecret, 0, 3);
            tlpDownload.Controls.Add(TB_ApiSecret, 1, 3);
            tlpDownload.Controls.Add(l_domain, 0, 4);
            tlpDownload.Controls.Add(TB_Domain, 1, 4);
            tlpDownload.Controls.Add(l_bucket, 0, 5);
            tlpDownload.Controls.Add(TB_Bucket, 1, 5);
            tlpDownload.Controls.Add(l_zone, 0, 6);
            tlpDownload.Controls.Add(CB_Zone, 1, 6);
            tlpDownload.Name = "tlpDownload";
            // 
            // l_downloader
            // 
            resources.ApplyResources(l_downloader, "l_downloader");
            l_downloader.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            l_downloader.Name = "l_downloader";
            // 
            // CB_Downloader
            // 
            resources.ApplyResources(CB_Downloader, "CB_Downloader");
            CB_Downloader.BackColor = System.Drawing.Color.White;
            CB_Downloader.DisplayMember = "Name";
            CB_Downloader.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            CB_Downloader.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            CB_Downloader.FormattingEnabled = true;
            CB_Downloader.Name = "CB_Downloader";
            CB_Downloader.ValueMember = "Value";
            CB_Downloader.SelectedIndexChanged += CB_Downloader_SelectedIndexChanged;
            // 
            // l_username
            // 
            resources.ApplyResources(l_username, "l_username");
            l_username.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            l_username.Name = "l_username";
            // 
            // TB_Username
            // 
            resources.ApplyResources(TB_Username, "TB_Username");
            TB_Username.BackColor = System.Drawing.Color.White;
            TB_Username.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            TB_Username.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            TB_Username.Name = "TB_Username";
            // 
            // l_apikey
            // 
            resources.ApplyResources(l_apikey, "l_apikey");
            l_apikey.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            l_apikey.Name = "l_apikey";
            // 
            // TB_ApiKey
            // 
            resources.ApplyResources(TB_ApiKey, "TB_ApiKey");
            TB_ApiKey.BackColor = System.Drawing.Color.White;
            TB_ApiKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            TB_ApiKey.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            TB_ApiKey.Name = "TB_ApiKey";
            // 
            // l_apisecret
            // 
            resources.ApplyResources(l_apisecret, "l_apisecret");
            l_apisecret.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            l_apisecret.Name = "l_apisecret";
            // 
            // TB_ApiSecret
            // 
            resources.ApplyResources(TB_ApiSecret, "TB_ApiSecret");
            TB_ApiSecret.BackColor = System.Drawing.Color.White;
            TB_ApiSecret.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            TB_ApiSecret.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            TB_ApiSecret.Name = "TB_ApiSecret";
            // 
            // l_domain
            // 
            resources.ApplyResources(l_domain, "l_domain");
            l_domain.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            l_domain.Name = "l_domain";
            // 
            // TB_Domain
            // 
            resources.ApplyResources(TB_Domain, "TB_Domain");
            TB_Domain.BackColor = System.Drawing.Color.White;
            TB_Domain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            TB_Domain.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            TB_Domain.Name = "TB_Domain";
            // 
            // l_bucket
            // 
            resources.ApplyResources(l_bucket, "l_bucket");
            l_bucket.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            l_bucket.Name = "l_bucket";
            // 
            // TB_Bucket
            // 
            resources.ApplyResources(TB_Bucket, "TB_Bucket");
            TB_Bucket.BackColor = System.Drawing.Color.White;
            TB_Bucket.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            TB_Bucket.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            TB_Bucket.Name = "TB_Bucket";
            // 
            // l_zone
            // 
            resources.ApplyResources(l_zone, "l_zone");
            l_zone.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            l_zone.Name = "l_zone";
            // 
            // CB_Zone
            // 
            resources.ApplyResources(CB_Zone, "CB_Zone");
            CB_Zone.BackColor = System.Drawing.Color.White;
            CB_Zone.DisplayMember = "Name";
            CB_Zone.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            CB_Zone.FormattingEnabled = true;
            CB_Zone.Name = "CB_Zone";
            CB_Zone.ValueMember = "Value";
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(pictureBoxSettings);
            resources.ApplyResources(pnlHeader, "pnlHeader");
            pnlHeader.Name = "pnlHeader";
            // 
            // lblHeaderTitle
            // 
            resources.ApplyResources(lblHeaderTitle, "lblHeaderTitle");
            lblHeaderTitle.ForeColor = System.Drawing.Color.White;
            lblHeaderTitle.Name = "lblHeaderTitle";
            // 
            // pictureBoxSettings
            // 
            resources.ApplyResources(pictureBoxSettings, "pictureBoxSettings");
            pictureBoxSettings.Name = "pictureBoxSettings";
            pictureBoxSettings.TabStop = false;
            pictureBoxSettings.Paint += pictureSettings_Paint;
            // 
            // splitContainer1
            // 
            splitContainer1.BackColor = System.Drawing.Color.FromArgb(235, 235, 235);
            resources.ApplyResources(splitContainer1, "splitContainer1");
            splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            resources.ApplyResources(splitContainer1.Panel1, "splitContainer1.Panel1");
            splitContainer1.Panel1.BackColor = System.Drawing.Color.White;
            splitContainer1.Panel1.Controls.Add(tlpCapture);
            // 
            // splitContainer1.Panel2
            // 
            resources.ApplyResources(splitContainer1.Panel2, "splitContainer1.Panel2");
            splitContainer1.Panel2.BackColor = System.Drawing.Color.White;
            splitContainer1.Panel2.Controls.Add(tlpDownload);
            // 
            // SettingForm
            // 
            BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            resources.ApplyResources(this, "$this");
            Controls.Add(pnlHeader);
            Controls.Add(splitContainer1);
            Name = "SettingForm";
            FormClosed += SettingForm_FormClosed;
            tlpCapture.ResumeLayout(false);
            tlpCapture.PerformLayout();
            flpInterval.ResumeLayout(false);
            flpInterval.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)MUD_Interval).EndInit();
            flpZoom.ResumeLayout(false);
            flpZoom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)MUD_Zoom).EndInit();
            tlpDownload.ResumeLayout(false);
            tlpDownload.PerformLayout();
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxSettings).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox TB_Bucket;
        private System.Windows.Forms.TextBox TB_Domain;
        private System.Windows.Forms.Label l_bucket;
        private System.Windows.Forms.Label l_domain;
        private System.Windows.Forms.Label l_zone;
        private System.Windows.Forms.ComboBox CB_Zone;
        private System.Windows.Forms.TableLayoutPanel tlpCapture;
        private System.Windows.Forms.TableLayoutPanel tlpDownload;
        private System.Windows.Forms.FlowLayoutPanel flpInterval;
        private System.Windows.Forms.FlowLayoutPanel flpZoom;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblHeaderTitle;
        private System.Windows.Forms.PictureBox pictureBoxSettings;
    }
}