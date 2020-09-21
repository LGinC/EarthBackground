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
            this.l_status.AutoSize = true;
            this.l_status.Location = new System.Drawing.Point(89, 24);
            this.l_status.Name = "l_status";
            this.l_status.Size = new System.Drawing.Size(75, 17);
            this.l_status.TabIndex = 0;
            this.l_status.Text = "wait for run";
            // 
            // B_start
            // 
            this.B_start.Location = new System.Drawing.Point(31, 107);
            this.B_start.Name = "B_start";
            this.B_start.Size = new System.Drawing.Size(60, 27);
            this.B_start.TabIndex = 1;
            this.B_start.Text = "start";
            this.B_start.UseVisualStyleBackColor = true;
            this.B_start.Click += new System.EventHandler(this.B_start_Click);
            // 
            // B_stop
            // 
            this.B_stop.Enabled = false;
            this.B_stop.Location = new System.Drawing.Point(97, 107);
            this.B_stop.Name = "B_stop";
            this.B_stop.Size = new System.Drawing.Size(60, 27);
            this.B_stop.TabIndex = 1;
            this.B_stop.Text = "stop";
            this.B_stop.UseVisualStyleBackColor = true;
            this.B_stop.Click += new System.EventHandler(this.B_stop_Click);
            // 
            // B_settings
            // 
            this.B_settings.Location = new System.Drawing.Point(163, 107);
            this.B_settings.Name = "B_settings";
            this.B_settings.Size = new System.Drawing.Size(64, 27);
            this.B_settings.TabIndex = 1;
            this.B_settings.Text = "settings";
            this.B_settings.UseVisualStyleBackColor = true;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(31, 44);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(196, 12);
            this.progressBar1.TabIndex = 2;
            // 
            // l_progress
            // 
            this.l_progress.AutoSize = true;
            this.l_progress.Location = new System.Drawing.Point(97, 70);
            this.l_progress.Name = "l_progress";
            this.l_progress.Size = new System.Drawing.Size(0, 17);
            this.l_progress.TabIndex = 3;
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "地球背景";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(254, 154);
            this.Controls.Add(this.l_progress);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.B_settings);
            this.Controls.Add(this.B_stop);
            this.Controls.Add(this.B_start);
            this.Controls.Add(this.l_status);
            this.Name = "MainForm";
            this.Text = "EarthBackgroud";
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

