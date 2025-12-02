using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EarthBackground.Controls
{
    /// <summary>
    /// 现代化通知提示控件
    /// </summary>
    public class ModernNotification : Form
    {
        private readonly Timer _showTimer;
        private readonly Timer _hideTimer;
        private readonly string _message;
        private readonly NotificationType _type;
        
        public enum NotificationType
        {
            Success,
            Warning,
            Error,
            Info
        }

        public ModernNotification(string message, NotificationType type = NotificationType.Info, int duration = 3000)
        {
            _message = message;
            _type = type;
            
            InitializeComponent();
            
            _showTimer = new Timer { Interval = 16 };
            _hideTimer = new Timer { Interval = duration };
            
            _showTimer.Tick += ShowTimer_Tick;
            _hideTimer.Tick += HideTimer_Tick;
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            
            // 窗体设置
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            Size = new Size(350, 80);
            
            // 设置位置（右下角）
            var workingArea = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(workingArea.Right - Width - 20, workingArea.Bottom - Height - 20);
            
            // 设置样式
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint | 
                     ControlStyles.DoubleBuffer | 
                     ControlStyles.ResizeRedraw, true);
            
            ResumeLayout();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // 获取颜色配置
            var colors = GetNotificationColors(_type);
            
            // 绘制阴影
            DrawDropShadow(g);
            
            // 绘制背景
            var backgroundRect = new Rectangle(5, 5, Width - 10, Height - 10);
            using (var backgroundBrush = new SolidBrush(Color.White))
            {
                var backgroundPath = GetRoundedRect(backgroundRect, 8);
                g.FillPath(backgroundBrush, backgroundPath);
            }
            
            // 绘制左侧彩色条
            var colorBarRect = new Rectangle(8, 8, 4, Height - 16);
            using (var colorBrush = new SolidBrush(colors.Primary))
            {
                var colorPath = GetRoundedRect(colorBarRect, 2);
                g.FillPath(colorBrush, colorPath);
            }
            
            // 绘制图标
            var iconRect = new Rectangle(20, 20, 24, 24);
            DrawIcon(g, iconRect, colors);
            
            // 绘制文字
            var textRect = new Rectangle(55, 15, Width - 70, Height - 30);
            using (var textBrush = new SolidBrush(Color.FromArgb(52, 73, 94)))
            {
                var font = new Font("Segoe UI", 10F);
                var format = new StringFormat 
                { 
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center
                };
                
                g.DrawString(_message, font, textBrush, textRect, format);
            }
            
            // 绘制边框
            using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
            {
                var borderPath = GetRoundedRect(backgroundRect, 8);
                g.DrawPath(borderPen, borderPath);
            }
        }

        private void DrawDropShadow(Graphics g)
        {
            for (int i = 0; i < 5; i++)
            {
                var shadowRect = new Rectangle(i, i, Width - i * 2, Height - i * 2);
                var alpha = (int)(20 * (5 - i) / 5.0);
                
                using (var shadowBrush = new SolidBrush(Color.FromArgb(alpha, Color.Black)))
                {
                    var shadowPath = GetRoundedRect(shadowRect, 8 + i);
                    g.FillPath(shadowBrush, shadowPath);
                }
            }
        }

        private void DrawIcon(Graphics g, Rectangle iconRect, NotificationColors colors)
        {
            using (var iconBrush = new SolidBrush(colors.Primary))
            using (var iconPen = new Pen(colors.Primary, 2))
            {
                switch (_type)
                {
                    case NotificationType.Success:
                        // 绘制勾号
                        var checkPoints = new[]
                        {
                            new Point(iconRect.X + 6, iconRect.Y + 12),
                            new Point(iconRect.X + 10, iconRect.Y + 16),
                            new Point(iconRect.X + 18, iconRect.Y + 8)
                        };
                        g.DrawLines(iconPen, checkPoints);
                        break;
                        
                    case NotificationType.Warning:
                        // 绘制感叹号
                        g.FillEllipse(iconBrush, iconRect.X + 10, iconRect.Y + 16, 4, 4);
                        g.DrawLine(iconPen, iconRect.X + 12, iconRect.Y + 6, iconRect.X + 12, iconRect.Y + 14);
                        break;
                        
                    case NotificationType.Error:
                        // 绘制X号
                        g.DrawLine(iconPen, iconRect.X + 6, iconRect.Y + 6, iconRect.X + 18, iconRect.Y + 18);
                        g.DrawLine(iconPen, iconRect.X + 18, iconRect.Y + 6, iconRect.X + 6, iconRect.Y + 18);
                        break;
                        
                    case NotificationType.Info:
                        // 绘制i号
                        g.FillEllipse(iconBrush, iconRect.X + 10, iconRect.Y + 6, 4, 4);
                        g.DrawLine(iconPen, iconRect.X + 12, iconRect.Y + 12, iconRect.X + 12, iconRect.Y + 18);
                        break;
                }
            }
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diameter = radius * 2;
            var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        private NotificationColors GetNotificationColors(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => new NotificationColors 
                { 
                    Primary = Color.FromArgb(46, 204, 113),
                    Background = Color.FromArgb(245, 255, 250) 
                },
                NotificationType.Warning => new NotificationColors 
                { 
                    Primary = Color.FromArgb(241, 196, 15),
                    Background = Color.FromArgb(255, 253, 240) 
                },
                NotificationType.Error => new NotificationColors 
                { 
                    Primary = Color.FromArgb(231, 76, 60),
                    Background = Color.FromArgb(255, 245, 245) 
                },
                NotificationType.Info => new NotificationColors 
                { 
                    Primary = Color.FromArgb(52, 152, 219),
                    Background = Color.FromArgb(240, 248, 255) 
                },
                _ => new NotificationColors 
                { 
                    Primary = Color.FromArgb(149, 165, 166),
                    Background = Color.White 
                }
            };
        }

        public void ShowNotification()
        {
            Opacity = 0;
            Show();
            _showTimer.Start();
            _hideTimer.Start();
        }

        private void ShowTimer_Tick(object sender, EventArgs e)
        {
            if (Opacity < 1)
            {
                Opacity += 0.05;
            }
            else
            {
                _showTimer.Stop();
            }
        }

        private void HideTimer_Tick(object sender, EventArgs e)
        {
            _hideTimer.Stop();
            var hideTimer = new Timer { Interval = 16 };
            
            hideTimer.Tick += (s, args) =>
            {
                if (Opacity > 0)
                {
                    Opacity -= 0.05;
                }
                else
                {
                    hideTimer.Stop();
                    hideTimer.Dispose();
                    Close();
                }
            };
            
            hideTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _showTimer?.Dispose();
                _hideTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private struct NotificationColors
        {
            public Color Primary;
            public Color Background;
        }

        /// <summary>
        /// 显示通知的静态方法
        /// </summary>
        public static void Show(string message, NotificationType type = NotificationType.Info, int duration = 3000)
        {
            var notification = new ModernNotification(message, type, duration);
            notification.ShowNotification();
        }
    }
}