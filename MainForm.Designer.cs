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
            this.SuspendLayout();
            // 
            // l_status
            // 
            resources.ApplyResources(this.l_status, "l_status");
            this.l_status.Name = "l_status";
            // 
            // B_start
            // 
            resources.ApplyResources(this.B_start, "B_start");
            this.B_start.Name = "B_start";
            this.B_start.UseVisualStyleBackColor = true;
            this.B_start.Click += new System.EventHandler(this.B_start_Click);
            // 
            // B_stop
            // 
            resources.ApplyResources(this.B_stop, "B_stop");
            this.B_stop.Name = "B_stop";
            this.B_stop.UseVisualStyleBackColor = true;
            this.B_stop.Click += new System.EventHandler(this.B_stop_Click);
            // 
            // B_settings
            // 
            resources.ApplyResources(this.B_settings, "B_settings");
            this.B_settings.Name = "B_settings";
            this.B_settings.UseVisualStyleBackColor = true;
            this.B_settings.Click += new System.EventHandler(this.B_settings_Click);
            // 
            // progressBar1
            // 
            resources.ApplyResources(this.progressBar1, "progressBar1");
            this.progressBar1.Name = "progressBar1";
            // 
            // l_progress
            // 
            resources.ApplyResources(this.l_progress, "l_progress");
            this.l_progress.Name = "l_progress";
            // 
            // notifyIcon1
            // 
            resources.ApplyResources(this.notifyIcon1, "notifyIcon1");
            this.notifyIcon1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseClick);
            // 
            // MainForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.l_progress);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.B_settings);
            this.Controls.Add(this.B_stop);
            this.Controls.Add(this.B_start);
            this.Controls.Add(this.l_status);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Deactivate += new System.EventHandler(this.MainForm_Deactivate);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label l_status;
        private System.Windows.Forms.Button B_start;
        private System.Windows.Forms.Button B_stop;
        private System.Windows.Forms.Button B_settings;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label l_progress;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
    }
}

