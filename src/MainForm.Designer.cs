namespace EarthBackground
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.l_status = new System.Windows.Forms.Label();
            this.B_start = new System.Windows.Forms.Button();
            this.B_stop = new System.Windows.Forms.Button();
            this.B_settings = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.l_progress = new System.Windows.Forms.Label();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pictureBoxEarth = new System.Windows.Forms.PictureBox();
            this.panelMain = new System.Windows.Forms.Panel();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxEarth)).BeginInit();
            this.panelMain.SuspendLayout();
            this.panelButtons.SuspendLayout();
            this.panelStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.pictureBoxEarth);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(480, 80);
            this.panelHeader.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(80, 25);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(250, 32);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "🌍 Earth Background";
            // 
            // pictureBoxEarth
            // 
            this.pictureBoxEarth.BackColor = System.Drawing.Color.Transparent;
            this.pictureBoxEarth.Location = new System.Drawing.Point(20, 15);
            this.pictureBoxEarth.Name = "pictureBoxEarth";
            this.pictureBoxEarth.Size = new System.Drawing.Size(50, 50);
            this.pictureBoxEarth.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxEarth.TabIndex = 0;
            this.pictureBoxEarth.TabStop = false;
            this.pictureBoxEarth.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBoxEarth_Paint);
            // 
            // panelMain
            // 
            this.panelMain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.panelMain.Controls.Add(this.panelStatus);
            this.panelMain.Controls.Add(this.panelButtons);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 80);
            this.panelMain.Name = "panelMain";
            this.panelMain.Padding = new System.Windows.Forms.Padding(20);
            this.panelMain.Size = new System.Drawing.Size(480, 220);
            this.panelMain.TabIndex = 1;
            // 
            // panelStatus
            // 
            this.panelStatus.BackColor = System.Drawing.Color.White;
            this.panelStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelStatus.Controls.Add(this.l_status);
            this.panelStatus.Controls.Add(this.progressBar1);
            this.panelStatus.Controls.Add(this.l_progress);
            this.panelStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelStatus.Location = new System.Drawing.Point(20, 20);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Padding = new System.Windows.Forms.Padding(20);
            this.panelStatus.Size = new System.Drawing.Size(440, 120);
            this.panelStatus.TabIndex = 0;
            // 
            // l_status
            // 
            this.l_status.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular);
            this.l_status.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.l_status.Location = new System.Drawing.Point(20, 20);
            this.l_status.Name = "l_status";
            this.l_status.Size = new System.Drawing.Size(398, 25);
            this.l_status.TabIndex = 0;
            this.l_status.Text = "等待运行...";
            this.l_status.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // progressBar1
            // 
            this.progressBar1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(240)))), ((int)(((byte)(241)))));
            this.progressBar1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
            this.progressBar1.Location = new System.Drawing.Point(20, 55);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(398, 20);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 1;
            // 
            // l_progress
            // 
            this.l_progress.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.l_progress.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(149)))), ((int)(((byte)(165)))), ((int)(((byte)(166)))));
            this.l_progress.Location = new System.Drawing.Point(20, 80);
            this.l_progress.Name = "l_progress";
            this.l_progress.Size = new System.Drawing.Size(398, 20);
            this.l_progress.TabIndex = 2;
            this.l_progress.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelButtons
            // 
            this.panelButtons.BackColor = System.Drawing.Color.White;
            this.panelButtons.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelButtons.Controls.Add(this.B_start);
            this.panelButtons.Controls.Add(this.B_stop);
            this.panelButtons.Controls.Add(this.B_settings);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelButtons.Location = new System.Drawing.Point(20, 140);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Padding = new System.Windows.Forms.Padding(20, 10, 20, 10);
            this.panelButtons.Size = new System.Drawing.Size(440, 60);
            this.panelButtons.TabIndex = 1;
            // 
            // B_start
            // 
            this.B_start.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
            this.B_start.FlatAppearance.BorderSize = 0;
            this.B_start.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.B_start.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.B_start.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.B_start.ForeColor = System.Drawing.Color.White;
            this.B_start.Location = new System.Drawing.Point(20, 10);
            this.B_start.Name = "B_start";
            this.B_start.Size = new System.Drawing.Size(100, 40);
            this.B_start.TabIndex = 0;
            this.B_start.Text = "🚀 开始";
            this.B_start.UseVisualStyleBackColor = false;
            this.B_start.Click += new System.EventHandler(this.B_start_Click);
            // 
            // B_stop
            // 
            this.B_stop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(231)))), ((int)(((byte)(76)))), ((int)(((byte)(60)))));
            this.B_stop.FlatAppearance.BorderSize = 0;
            this.B_stop.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(57)))), ((int)(((byte)(43)))));
            this.B_stop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.B_stop.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.B_stop.ForeColor = System.Drawing.Color.White;
            this.B_stop.Location = new System.Drawing.Point(130, 10);
            this.B_stop.Name = "B_stop";
            this.B_stop.Size = new System.Drawing.Size(100, 40);
            this.B_stop.TabIndex = 1;
            this.B_stop.Text = "⏹ 停止";
            this.B_stop.UseVisualStyleBackColor = false;
            this.B_stop.Click += new System.EventHandler(this.B_stop_Click);
            // 
            // B_settings
            // 
            this.B_settings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.B_settings.FlatAppearance.BorderSize = 0;
            this.B_settings.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.B_settings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.B_settings.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.B_settings.ForeColor = System.Drawing.Color.White;
            this.B_settings.Location = new System.Drawing.Point(240, 10);
            this.B_settings.Name = "B_settings";
            this.B_settings.Size = new System.Drawing.Size(100, 40);
            this.B_settings.TabIndex = 2;
            this.B_settings.Text = "⚙ 设置";
            this.B_settings.UseVisualStyleBackColor = false;
            this.B_settings.Click += new System.EventHandler(this.B_settings_Click);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "EarthBackground";
            this.notifyIcon1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseClick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(480, 300);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.panelHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "EarthBackground - 地球背景";
            this.Deactivate += new System.EventHandler(this.MainForm_Deactivate);
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxEarth)).EndInit();
            this.panelMain.ResumeLayout(false);
            this.panelButtons.ResumeLayout(false);
            this.panelStatus.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label l_status;
        private System.Windows.Forms.Button B_start;
        private System.Windows.Forms.Button B_stop;
        private System.Windows.Forms.Button B_settings;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label l_progress;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.PictureBox pictureBoxEarth;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Panel panelStatus;
    }
}

