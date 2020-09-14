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
            this.l_status = new System.Windows.Forms.Label();
            this.B_start = new System.Windows.Forms.Button();
            this.B_stop = new System.Windows.Forms.Button();
            this.B_settings = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // l_status
            // 
            this.l_status.AutoSize = true;
            this.l_status.Location = new System.Drawing.Point(97, 48);
            this.l_status.Name = "l_status";
            this.l_status.Size = new System.Drawing.Size(55, 17);
            this.l_status.TabIndex = 0;
            this.l_status.Text = "Running";
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
            this.B_stop.Location = new System.Drawing.Point(97, 107);
            this.B_stop.Name = "B_stop";
            this.B_stop.Size = new System.Drawing.Size(60, 27);
            this.B_stop.TabIndex = 1;
            this.B_stop.Text = "stop";
            this.B_stop.UseVisualStyleBackColor = true;
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
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(254, 154);
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
    }
}

