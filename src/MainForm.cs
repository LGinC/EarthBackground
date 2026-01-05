using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EarthBackground.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EarthBackground
{
    public partial class MainForm : Form
    {
        private readonly ILogger<MainForm> _logger;
        private readonly IServiceProvider _provider;
        private readonly WallpaperService _wallpaperService;
        readonly System.ComponentModel.ComponentResourceManager _resources = new(typeof(MainForm));
        readonly CultureInfo _current;

        private readonly Lock _lockObject = new();

        public MainForm(ILogger<MainForm> logger,
            IServiceProvider provider,
            WallpaperService wallpaperService)
        {
            _logger = logger;
            _provider = provider;
            _wallpaperService = wallpaperService;
            InitializeComponent();
            _current = Thread.CurrentThread.CurrentUICulture;

            // 订阅WallpaperService事件
            SubscribeToWallpaperServiceEvents();
        }

        private string L(string key) => _resources.GetString(key, _current) ?? key;

        /// <summary>
        /// 订阅WallpaperService事件
        /// </summary>
        private void SubscribeToWallpaperServiceEvents()
        {
            _wallpaperService.StatusChanged += OnStatusChanged;
            _wallpaperService.ProgressChanged += OnProgressChanged;
            _wallpaperService.ImageSaved += OnImageSaved;
            _wallpaperService.ErrorOccurred += OnErrorOccurred;
        }

        /// <summary>
        /// 状态变更事件处理
        /// </summary>
        private void OnStatusChanged(string status)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnStatusChanged(status));
                return;
            }

            // 同步按钮状态和样式
            UpdateButtonStates();

            var newText = status switch
            {
                "Running" => L("running"),
                "Initializing..." => L("running"),
                "Downloading..." => L("running"),
                "Setting Wallpaper..." => L("running"),
                "Complete" => L("complete"),
                "Stopped" => L("wait for run"),
                _ => status
            };

            var newColor = status switch
            {
                "Running" or "Initializing..." or "Downloading..." or "Setting Wallpaper..." => Color.FromArgb(46, 204, 113),
                "Complete" => Color.FromArgb(52, 152, 219),
                "Stopped" => Color.FromArgb(52, 73, 94),
                _ => Color.FromArgb(52, 73, 94)
            };

            // 添加状态文字变化动画
            AnimateStatusChange(l_status, newText, newColor);

            // 根据状态添加图标动画
            AnimateStatusIcon();
        }

        /// <summary>
        /// 状态文字变化动画
        /// </summary>
        private void AnimateStatusChange(Label label, string newText, Color newColor)
        {
            var fadeOutTimer = new System.Windows.Forms.Timer { Interval = 16 };
            var originalColor = label.ForeColor;
            float alpha = 1.0f;

            fadeOutTimer.Tick += (s, e) =>
            {
                alpha -= 0.1f;
                if (alpha <= 0)
                {
                    fadeOutTimer.Stop();
                    label.Text = newText;

                    // 淡入新内容
                    var fadeInTimer = new System.Windows.Forms.Timer { Interval = 16 };
                    fadeInTimer.Tick += (s2, e2) =>
                    {
                        alpha += 0.1f;
                        if (alpha >= 1.0f)
                        {
                            alpha = 1.0f;
                            label.ForeColor = newColor;
                            fadeInTimer.Stop();
                            fadeInTimer.Dispose();
                        }
                        else
                        {
                            var blendedColor = BlendColors(Color.Transparent, newColor, alpha);
                            label.ForeColor = blendedColor;
                        }
                    };
                    fadeInTimer.Start();
                    fadeOutTimer.Dispose();
                }
                else
                {
                    var blendedColor = BlendColors(Color.Transparent, originalColor, alpha);
                    label.ForeColor = blendedColor;
                }
            };

            fadeOutTimer.Start();
        }

        /// <summary>
        /// 状态图标动画
        /// </summary>
        private void AnimateStatusIcon()
        {
            if (_wallpaperService.IsRunning)
            {
                // 只要在运行，就保持地球旋转
                StartEarthRotationAnimation();
            }
            else
            {
                StopEarthRotationAnimation();
            }
        }

        private System.Windows.Forms.Timer? _earthRotationTimer;
        private float _earthRotationAngle = 0f;

        /// <summary>
        /// 开始地球旋转动画
        /// </summary>
        private void StartEarthRotationAnimation()
        {
            if (_earthRotationTimer == null)
            {
                _earthRotationTimer = new System.Windows.Forms.Timer { Interval = 50 };
                _earthRotationTimer.Tick += (s, e) =>
                {
                    _earthRotationAngle += 2f;
                    if (_earthRotationAngle >= 360f) _earthRotationAngle = 0f;
                    pictureBoxEarth.Invalidate();
                };
            }

            _earthRotationTimer.Start();
        }

        /// <summary>
        /// 停止地球旋转动画
        /// </summary>
        private void StopEarthRotationAnimation()
        {
            _earthRotationTimer?.Stop();
            _earthRotationAngle = 0f;
            pictureBoxEarth.Invalidate();
        }

        /// <summary>
        /// 颜色混合工具方法
        /// </summary>
        private Color BlendColors(Color color1, Color color2, float factor)
        {
            factor = Math.Max(0, Math.Min(1, factor));

            return Color.FromArgb(
                (int)(color1.A + (color2.A - color1.A) * factor),
                (int)(color1.R + (color2.R - color1.R) * factor),
                (int)(color1.G + (color2.G - color1.G) * factor),
                (int)(color1.B + (color2.B - color1.B) * factor)
            );
        }

        /// <summary>
        /// 进度变更事件处理
        /// </summary>
        private void OnProgressChanged(int current, int total)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnProgressChanged(current, total));
                return;
            }

            lock (_lockObject)
            {
                if (total > 0)
                {
                    progressBar1.Maximum = total;

                    // 使用动画效果更新进度条
                    var targetValue = Math.Min(current, total);
                    AnimateProgressBar(progressBar1.Value, targetValue);

                    l_progress.Text = $@"{current}/{total} ({(int)((float)current / total * 100)}%)";
                }

                // 完成时重置进度条
                if (current >= total && total > 0)
                {
                    Task.Delay(1500).ContinueWith(_ =>
                    {
                        if (InvokeRequired)
                        {
                            Invoke(() =>
                            {
                                AnimateProgressBar(progressBar1.Value, 0);
                                l_progress.Text = string.Empty;
                            });
                        }
                        else
                        {
                            AnimateProgressBar(progressBar1.Value, 0);
                            l_progress.Text = string.Empty;
                        }
                    });
                }
            }
        }

        /// <summary>
        /// 进度条动画
        /// </summary>
        private void AnimateProgressBar(int fromValue, int toValue, int duration = 300)
        {
            if (fromValue == toValue) return;

            var timer = new System.Windows.Forms.Timer { Interval = 16 }; // 60 FPS
            var startTime = DateTime.Now;
            var startValue = fromValue;

            timer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - startTime;
                var progress = Math.Min(1.0, elapsed.TotalMilliseconds / duration);

                if (progress >= 1.0)
                {
                    progressBar1.Value = toValue;
                    timer.Stop();
                    timer.Dispose();
                }
                else
                {
                    // 使用缓动函数实现平滑动画
                    var easeProgress = 1 - Math.Pow(1 - progress, 3); // easeOutCubic
                    progressBar1.Value = (int)(startValue + (toValue - startValue) * easeProgress);
                }
            };

            timer.Start();
        }

        /// <summary>
        /// 图片保存事件处理
        /// </summary>
        private void OnImageSaved(string imagePath)
        {
            _logger.LogInformation("壁纸已保存: {ImagePath}", imagePath);
        }

        /// <summary>
        /// 错误发生事件处理
        /// </summary>
        private void OnErrorOccurred(Exception ex)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnErrorOccurred(ex));
                return;
            }

            _logger.LogError(ex, "壁纸服务发生错误");

            l_status.Text = L("wait for run");
            l_status.ForeColor = Color.Black;
            l_progress.Text = string.Empty;
            progressBar1.Value = 0;

            // 使用现代化通知而不是系统托盘通知
            try
            {
                EarthBackground.Controls.ModernNotification.Show($"下载失败: {ex.Message}",
                    EarthBackground.Controls.ModernNotification.NotificationType.Error);
            }
            catch
            {
                // 如果现代通知失败，使用传统通知
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(3000, "图片下载失败", ex.Message, ToolTipIcon.Warning);
            }

            // 同步按钮状态和样式
            UpdateButtonStates();
        }

        private void B_start_Click(object sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("用户点击开始按钮");

                // 直接执行，无动画避免闪烁
                _wallpaperService.Start();

                // 同步按钮状态和样式
                UpdateButtonStates();

                // 重置进度显示
                progressBar1.Value = 0;
                l_progress.Text = string.Empty;

                // 显示启动通知
                EarthBackground.Controls.ModernNotification.Show("壁纸服务已启动",
                    EarthBackground.Controls.ModernNotification.NotificationType.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动壁纸服务时发生错误");
                OnErrorOccurred(ex);
            }
        }

        /// <summary>
        /// 同步按钮状态和颜色样式
        /// </summary>
        private void UpdateButtonStates()
        {
            if (InvokeRequired)
            {
                Invoke(UpdateButtonStates);
                return;
            }

            bool isRunning = _wallpaperService.IsRunning;

            // 开始按钮
            B_start.Enabled = !isRunning;
            B_start.BackColor = !isRunning ? Color.FromArgb(46, 204, 113) : Color.FromArgb(149, 165, 166);

            // 停止按钮
            B_stop.Enabled = isRunning;
            B_stop.BackColor = isRunning ? Color.FromArgb(231, 76, 60) : Color.FromArgb(149, 165, 166);
        }


        private void B_stop_Click(object sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("用户点击停止按钮");

                // 直接执行，无动画避免闪烁
                _wallpaperService.Stop();

                // 同步按钮状态和样式
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止壁纸服务时发生错误");
                // 即使出错也要同步状态
                UpdateButtonStates();
            }
        }


        private void NotifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            Show();
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
            BringToFront();
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
            {
                return;
            }

            ShowInTaskbar = false;
            Hide();
            notifyIcon1.Visible = true;
        }

        private void B_settings_Click(object sender, EventArgs e)
        {
            Hide();
            var settingForm = _provider.GetRequiredService<SettingForm>();
            settingForm.Show();
            settingForm.FormClosed += (_, _) =>
            {
                Show();
                // 重新显示时，再次校验动画状态
                AnimateStatusIcon();
            };
        }

        /// <summary>
        /// 窗体加载事件
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // 同步按钮状态和样式
            UpdateButtonStates();

            l_status.Text = _wallpaperService.IsRunning ? L("running") : L("wait for run");
            if (_wallpaperService.IsRunning)
            {
                StartEarthRotationAnimation();
            }

            // 应用现代化样式
            ApplyModernStyling();
        }

        /// <summary>
        /// 应用现代化样式
        /// </summary>
        private void ApplyModernStyling()
        {
            // 设置窗体阴影效果（Windows 10+）
            if (Environment.OSVersion.Version.Major >= 10)
            {
                var margin = new Padding(1);
                typeof(Control).GetProperty("Margin")?.SetValue(this, margin);
            }

            // 设置圆角进度条样式
            progressBar1.BackColor = Color.FromArgb(236, 240, 241);
            progressBar1.ForeColor = Color.FromArgb(46, 204, 113);

            // 应用按钮现代化样式
            ApplyButtonStyling();

            // 添加窗体淡入动画
            AnimateFormEntry();

            // 设置控件悬停效果
            SetupHoverEffects();
        }

        /// <summary>
        /// 应用按钮现代化样式
        /// </summary>
        private void ApplyButtonStyling()
        {
            // 开始按钮样式
            B_start.FlatStyle = FlatStyle.Flat;
            B_start.FlatAppearance.BorderSize = 0;
            B_start.BackColor = Color.FromArgb(46, 204, 113);
            B_start.ForeColor = Color.White;
            B_start.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            B_start.Cursor = Cursors.Hand;

            // 停止按钮样式
            B_stop.FlatStyle = FlatStyle.Flat;
            B_stop.FlatAppearance.BorderSize = 0;
            B_stop.BackColor = Color.FromArgb(231, 76, 60);
            B_stop.ForeColor = Color.White;
            B_stop.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            B_stop.Cursor = Cursors.Hand;

            // 设置按钮样式
            B_settings.FlatStyle = FlatStyle.Flat;
            B_settings.FlatAppearance.BorderSize = 0;
            B_settings.BackColor = Color.FromArgb(52, 152, 219);
            B_settings.ForeColor = Color.White;
            B_settings.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            B_settings.Cursor = Cursors.Hand;

            // 设置悬停颜色
            B_start.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 174, 96);
            B_stop.FlatAppearance.MouseOverBackColor = Color.FromArgb(192, 57, 43);
            B_settings.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
        }

        /// <summary>
        /// 窗体淡入动画
        /// </summary>
        private void AnimateFormEntry()
        {
            Opacity = 0;
            var fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };

            fadeTimer.Tick += (s, e) =>
            {
                if (Opacity < 1)
                {
                    Opacity += 0.05;
                }
                else
                {
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                }
            };

            fadeTimer.Start();
        }

        /// <summary>
        /// 设置控件悬停效果
        /// </summary>
        private void SetupHoverEffects()
        {
            // 为面板添加微妙的悬停效果
            panelStatus.MouseEnter += (s, e) =>
            {
                panelStatus.BackColor = Color.FromArgb(248, 250, 252);
            };

            panelStatus.MouseLeave += (s, e) =>
            {
                panelStatus.BackColor = Color.White;
            };

            // 为标题添加悬停效果
            lblTitle.MouseEnter += (s, e) =>
            {
                lblTitle.ForeColor = Color.FromArgb(240, 248, 255);
            };

            lblTitle.MouseLeave += (s, e) =>
            {
                lblTitle.ForeColor = Color.White;
            };
        }

        /// <summary>
        /// 绘制地球图标
        /// </summary>
        private void PictureBoxEarth_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            // 在 80x80 的空间中绘制一个 50x50 的地球
            var rect = new Rectangle(15, 15, 50, 50);

            // 1. 外部大气层 (最底层)
            DrawAtmosphere(g, rect);

            // 2. 球体底色
            using (var earthBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, Color.FromArgb(41, 128, 185), Color.FromArgb(20, 40, 80),
                System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal))
            {
                g.FillEllipse(earthBrush, rect);
            }

            // 3. 绘制地表层 (使用裁剪确保仅在圆内可见)
            var state = g.Save();
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                path.AddEllipse(rect);
                g.SetClip(path);
            }

            // --- 动态图层 (水平位移自转) ---
            DrawOceanTexture(g, rect);
            DrawContinents(g, rect);

            g.Restore(state);

            // 4. 内部体积阴影
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                path.AddEllipse(rect);
                using var pgb = new System.Drawing.Drawing2D.PathGradientBrush(path);
                pgb.CenterColor = Color.Transparent;
                pgb.SurroundColors = [Color.FromArgb(120, 0, 0, 0)];
                pgb.FocusScales = new PointF(0.85f, 0.85f);
                g.FillPath(pgb, path);
            }

            // 5. 固定光影与高光
            DrawHighlight(g, rect);

            // 6. 细边框
            using var borderPen = new Pen(Color.FromArgb(40, Color.White), 1f);
            g.DrawEllipse(borderPen, rect);
        }

        /// <summary>
        /// 绘制海洋纹理
        /// </summary>
        private void DrawOceanTexture(Graphics g, Rectangle rect)
        {
            var radius = rect.Width / 2f;
            var center = new PointF(rect.X + radius, rect.Y + radius);
            using var wavePen = new Pen(Color.FromArgb(20, Color.White), 1f);

            for (int i = 0; i < 3; i++)
            {
                float relativeY = 12 + i * 12;
                float waveRotation = (_earthRotationAngle * 0.8f + i * 60) % 360;
                float relAngle = waveRotation > 180 ? waveRotation - 360 : waveRotation;

                if (relAngle > -90 && relAngle < 90)
                {
                    float angleRad = relAngle * (float)Math.PI / 180f;
                    float sinFactor = (float)Math.Sin(angleRad);
                    float cosFactor = (float)Math.Cos(angleRad);

                    float x = center.X + sinFactor * radius;
                    float w = rect.Width * 0.4f * cosFactor;

                    if (w > 2f)
                    {
                        g.DrawArc(wavePen, x - w / 2, rect.Y + relativeY, w, 5, 180, 180);
                    }
                }
            }
        }

        /// <summary>
        /// 绘制大陆
        /// </summary>
        private void DrawContinents(Graphics g, Rectangle rect)
        {
            var radius = rect.Width / 2f;
            var center = new PointF(rect.X + radius, rect.Y + radius);
            using var landBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, Color.FromArgb(46, 204, 113), Color.FromArgb(20, 90, 50),
                System.Drawing.Drawing2D.LinearGradientMode.Vertical);

            // 高精细度大陆轮廓 (Longitude 0-360, Latitude -25 to 25)
            var continentSilhouettes = new List<PointF[]>
            {
                // 欧亚大陆 (更精细的海岸线)
                new PointF[] {
                    new(0, -18), new(15, -20), new(35, -22), new(55, -18), new(70, -10),
                    new(80, 5), new(75, 12), new(50, 15), new(30, 18), new(10, 15),
                    new(-5, 8), new(-15, 0), new(-10, -12)
                },
                // 非洲 (典型的倒三角但带弯曲)
                new PointF[] {
                    new(75, 5), new(95, 8), new(105, 15), new(100, 28), new(90, 35),
                    new(75, 25), new(68, 15), new(70, 8)
                },
                // 北美洲
                new PointF[] {
                    new(180, -20), new(210, -22), new(230, -15), new(235, -5),
                    new(220, 10), new(205, 12), new(190, 0), new(185, -10)
                },
                // 南美洲
                new PointF[] {
                    new(220, 12), new(240, 15), new(235, 30), new(215, 40),
                    new(205, 30), new(210, 15)
                },
                // 大洋洲 (澳洲主块)
                new PointF[] {
                    new(285, 12), new(305, 14), new(315, 22), new(305, 32),
                    new(285, 30), new(280, 20)
                },
                // 格陵兰/岛屿
                new PointF[] { new(160, -22), new(175, -25), new(180, -18), new(165, -15) }
            };

            foreach (var silhouette in continentSilhouettes)
            {
                var projectedPoints = new List<PointF>();
                bool anyVisible = false;

                foreach (var p in silhouette)
                {
                    float lon = (p.X + _earthRotationAngle) % 360;
                    float relAngle = lon > 180 ? lon - 360 : lon;

                    if (relAngle > -100 && relAngle < 100)
                    {
                        float angleRad = relAngle * (float)Math.PI / 180f;
                        float sinFactor = (float)Math.Sin(angleRad);

                        float x = center.X + sinFactor * radius;
                        float y = center.Y + p.Y;

                        projectedPoints.Add(new PointF(x, y));
                        anyVisible = true;
                    }
                }

                if (anyVisible && projectedPoints.Count > 2)
                {
                    using var path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddClosedCurve(projectedPoints.ToArray(), 0.4f);
                    g.FillPath(landBrush, path);

                    // 增加细微的边缘阴影
                    using var shadowPen = new Pen(Color.FromArgb(50, Color.Black), 0.5f);
                    g.DrawPath(shadowPen, path);
                }
            }
        }

        /// <summary>
        /// 绘制大气层效果
        /// </summary>
        private static void DrawAtmosphere(Graphics g, Rectangle rect)
        {
            // 更柔和、范围更广的辉光
            var glowRect = new Rectangle(rect.X - 12, rect.Y - 12, rect.Width + 24, rect.Height + 24);
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(glowRect);

            using var pgb = new System.Drawing.Drawing2D.PathGradientBrush(path);
            pgb.CenterColor = Color.FromArgb(60, 52, 152, 219);
            pgb.SurroundColors = [Color.Transparent];
            pgb.FocusScales = new PointF(0.4f, 0.4f);

            g.FillPath(pgb, path);
        }

        /// <summary>
        /// 绘制高光效果
        /// </summary>
        private static void DrawHighlight(Graphics g, Rectangle rect)
        {
            // 主光源 (左上方)
            using (var lightBrush = new System.Drawing.Drawing2D.PathGradientBrush([
                new PointF(rect.X + 15, rect.Y + 15),
                new PointF(rect.X + 35, rect.Y + 15),
                new PointF(rect.X + 35, rect.Y + 35),
                new PointF(rect.X + 15, rect.Y + 35)
            ]))
            {
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddEllipse(rect.X + 5, rect.Y + 5, 25, 20);
                using var pgb = new System.Drawing.Drawing2D.PathGradientBrush(path);
                pgb.CenterColor = Color.FromArgb(140, Color.White);
                pgb.SurroundColors = [Color.Transparent];
                g.FillPath(pgb, path);
            }

            // 右下方的环境遮挡
            var bottomRect = new Rectangle(rect.X, rect.Y + rect.Height / 2, rect.Width, rect.Height / 2);
            using var shadowBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                bottomRect, Color.Transparent, Color.FromArgb(60, Color.Black),
                System.Drawing.Drawing2D.LinearGradientMode.Vertical);
            g.FillEllipse(shadowBrush, bottomRect);
        }

         /// <summary>
        /// 窗体关闭事件，清理事件订阅
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 取消事件订阅
            if (_wallpaperService != null)
            {
                _wallpaperService.StatusChanged -= OnStatusChanged;
                _wallpaperService.ProgressChanged -= OnProgressChanged;
                _wallpaperService.ImageSaved -= OnImageSaved;
                _wallpaperService.ErrorOccurred -= OnErrorOccurred;
            }
            base.OnFormClosed(e);
        }
    }
}
