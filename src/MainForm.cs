using System;
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

            var newText = status switch
            {
                "Running" => L("running"),
                "Downloading..." => L("running"),
                "Setting Wallpaper..." => L("running"),
                "Complete" => L("complete"),
                "Stopped" => L("wait for run"),
                _ => status
            };

            var newColor = status switch
            {
                "Running" or "Downloading..." or "Setting Wallpaper..." => Color.FromArgb(46, 204, 113),
                "Complete" => Color.FromArgb(52, 152, 219),
                "Stopped" => Color.FromArgb(52, 73, 94),
                _ => Color.FromArgb(52, 73, 94)
            };

            // 添加状态文字变化动画
            AnimateStatusChange(l_status, newText, newColor);
            
            // 根据状态添加图标动画
            AnimateStatusIcon(status);
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
        private void AnimateStatusIcon(string status)
        {
            if (status == "Running" || status == "Downloading...")
            {
                // 添加地球旋转动画
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
            
            // 重置按钮状态
            B_start.Enabled = true;
            B_stop.Enabled = false;
        }

        private void B_start_Click(object sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("用户点击开始按钮");
                
                // 直接执行，无动画避免闪烁
                _wallpaperService.Start();
                
                // 更新UI状态
                B_start.Enabled = false;
                B_stop.Enabled = true;
                
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

        private void B_stop_Click(object sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("用户点击停止按钮");
                
                // 直接执行，无动画避免闪烁
                _wallpaperService.Stop();
                
                // 重置UI状态
                progressBar1.Value = 0;
                l_status.Text = L("wait for run");
                l_status.ForeColor = Color.FromArgb(52, 73, 94);
                l_progress.Text = string.Empty;
                B_start.Enabled = true;
                B_stop.Enabled = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止壁纸服务时发生错误");
                // 即使出错也要重置按钮状态
                B_start.Enabled = true;
                B_stop.Enabled = false;
            }
        }


        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            Show();
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
            BringToFront();
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            if(WindowState != FormWindowState.Minimized)
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
            settingForm.FormClosed += (_, _) => Show();
        }

        /// <summary>
        /// 窗体加载事件
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // 初始化时设置按钮状态
            B_start.Enabled = true;
            B_stop.Enabled = false;
            l_status.Text = L("wait for run");
            
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
        private void pictureBoxEarth_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            
            var rect = new Rectangle(5, 5, 40, 40);
            var center = new PointF(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            
            // 应用旋转变换
            if (_earthRotationAngle > 0)
            {
                g.TranslateTransform(center.X, center.Y);
                g.RotateTransform(_earthRotationAngle);
                g.TranslateTransform(-center.X, -center.Y);
            }
            
            // 绘制地球背景（深度渐变）
            using (var earthBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, Color.FromArgb(52, 152, 219), Color.FromArgb(25, 111, 166), 
                System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal))
            {
                g.FillEllipse(earthBrush, rect);
            }
            
            // 绘制海洋纹理
            DrawOceanTexture(g, rect);
            
            // 绘制大陆（动态位置）
            DrawContinents(g, rect);
            
            // 绘制大气层效果
            DrawAtmosphere(g, rect);
            
            // 绘制高光效果
            DrawHighlight(g, rect);
            
            // 绘制边框
            using (var borderPen = new Pen(Color.FromArgb(149, 165, 166), 1.5f))
            {
                g.DrawEllipse(borderPen, rect);
            }
            
            // 重置变换
            g.ResetTransform();
        }

        /// <summary>
        /// 绘制海洋纹理
        /// </summary>
        private void DrawOceanTexture(Graphics g, Rectangle rect)
        {
            // 添加海洋波纹效果
            using (var waveBrush = new SolidBrush(Color.FromArgb(20, Color.White)))
            {
                for (int i = 0; i < 3; i++)
                {
                    var waveY = rect.Y + 10 + i * 8 + (int)(Math.Sin(_earthRotationAngle * Math.PI / 180 + i) * 2);
                    g.FillEllipse(waveBrush, rect.X + 5, waveY, rect.Width - 10, 3);
                }
            }
        }

        /// <summary>
        /// 绘制大陆
        /// </summary>
        private void DrawContinents(Graphics g, Rectangle rect)
        {
            using (var landBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, Color.FromArgb(46, 204, 113), Color.FromArgb(39, 174, 96),
                System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
                // 根据旋转角度调整大陆位置
                var rotationFactor = _earthRotationAngle / 360f;
                var offsetX = (int)(rotationFactor * 20);
                
                var continents = new[]
                {
                    new Rectangle(rect.X + 8 + offsetX % 20, rect.Y + 12, 12, 8),   // 亚洲
                    new Rectangle(rect.X + 15 - offsetX % 15, rect.Y + 22, 10, 6),  // 非洲
                    new Rectangle(rect.X + 5 + offsetX % 10, rect.Y + 25, 8, 5),    // 美洲
                };
                
                foreach (var continent in continents)
                {
                    // 确保大陆在地球范围内
                    if (IsInsideCircle(continent, rect))
                    {
                        g.FillEllipse(landBrush, continent);
                        
                        // 添加大陆边缘高光
                        using (var highlightBrush = new SolidBrush(Color.FromArgb(50, Color.White)))
                        {
                            var highlightRect = new Rectangle(continent.X, continent.Y, continent.Width, 2);
                            g.FillEllipse(highlightBrush, highlightRect);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 绘制大气层效果
        /// </summary>
        private void DrawAtmosphere(Graphics g, Rectangle rect)
        {
            var atmosphereRect = new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
            
            using (var atmosphereBrush = new System.Drawing.Drawing2D.PathGradientBrush(
                new[] { 
                    new PointF(atmosphereRect.Left, atmosphereRect.Top),
                    new PointF(atmosphereRect.Right, atmosphereRect.Top),
                    new PointF(atmosphereRect.Right, atmosphereRect.Bottom),
                    new PointF(atmosphereRect.Left, atmosphereRect.Bottom)
                }))
            {
                atmosphereBrush.CenterColor = Color.Transparent;
                atmosphereBrush.SurroundColors = new[] { Color.FromArgb(30, 135, 206, 235) };
                
                g.FillEllipse(atmosphereBrush, atmosphereRect);
            }
        }

        /// <summary>
        /// 绘制高光效果
        /// </summary>
        private void DrawHighlight(Graphics g, Rectangle rect)
        {
            // 主高光
            var highlightRect = new Rectangle(rect.X + 8, rect.Y + 8, 20, 15);
            using (var highlight = new System.Drawing.Drawing2D.LinearGradientBrush(
                highlightRect,
                Color.FromArgb(120, Color.White), Color.Transparent,
                System.Drawing.Drawing2D.LinearGradientMode.BackwardDiagonal))
            {
                g.FillEllipse(highlight, highlightRect);
            }
            
            // 次要高光
            var secondaryHighlight = new Rectangle(rect.X + 25, rect.Y + 15, 8, 6);
            using (var secondaryBrush = new SolidBrush(Color.FromArgb(60, Color.White)))
            {
                g.FillEllipse(secondaryBrush, secondaryHighlight);
            }
        }

        /// <summary>
        /// 检查矩形是否在圆形内
        /// </summary>
        private bool IsInsideCircle(Rectangle rect, Rectangle circle)
        {
            var centerX = circle.X + circle.Width / 2f;
            var centerY = circle.Y + circle.Height / 2f;
            var radius = circle.Width / 2f;
            
            var rectCenterX = rect.X + rect.Width / 2f;
            var rectCenterY = rect.Y + rect.Height / 2f;
            
            var distance = Math.Sqrt(Math.Pow(rectCenterX - centerX, 2) + Math.Pow(rectCenterY - centerY, 2));
            
            return distance <= radius - rect.Width / 2f;
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
