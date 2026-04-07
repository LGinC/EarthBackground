using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace EarthBackground.Controls
{
    /// <summary>
    /// Avalonia 地球旋转动画控件，替代 WinForms 的 PictureBox + Paint 事件
    /// </summary>
    public class EarthCanvas : Control
    {
        public static readonly StyledProperty<float> RotationAngleProperty =
            AvaloniaProperty.Register<EarthCanvas, float>(nameof(RotationAngle), 0f);

        public float RotationAngle
        {
            get => GetValue(RotationAngleProperty);
            set => SetValue(RotationAngleProperty, value);
        }

        static EarthCanvas()
        {
            RotationAngleProperty.Changed.AddClassHandler<EarthCanvas>((x, _) => x.InvalidateVisual());
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var width = Bounds.Width;
            var height = Bounds.Height;

            // Draw in 80x80 space, earth at 50x50 centered
            var rect = new Rect(15, 15, 50, 50);

            // 1. Atmosphere glow
            DrawAtmosphere(context, rect);

            // 2. Earth base gradient
            DrawEarthBase(context, rect);

            // 3. Ocean texture + continents (clipped to circle)
            // Use a rounded rect with large radius to approximate ellipse clip
            var clipRect = new RoundedRect(rect, rect.Width / 2);
            using (context.PushClip(clipRect))
            {
                DrawOceanTexture(context, rect);
                DrawContinents(context, rect);
            }

            // 4. Inner shadow
            DrawInnerShadow(context, rect);

            // 5. Highlight
            DrawHighlight(context, rect);

            // 6. Border
            context.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1), rect.Center, rect.Width / 2, rect.Height / 2);
        }

        private void DrawAtmosphere(DrawingContext context, Rect rect)
        {
            var glowRect = rect.Inflate(12);
            var brush = new RadialGradientBrush
            {
                Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
                RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(60, 52, 152, 219), 0.4),
                    new GradientStop(Color.FromArgb(0, 52, 152, 219), 1.0)
                }
            };
            context.DrawEllipse(brush, null, glowRect.Center, glowRect.Width / 2, glowRect.Height / 2);
        }

        private void DrawEarthBase(DrawingContext context, Rect rect)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(255, 41, 128, 185), 0),
                    new GradientStop(Color.FromArgb(255, 20, 40, 80), 1)
                }
            };
            context.DrawEllipse(brush, null, rect.Center, rect.Width / 2, rect.Height / 2);
        }

        private void DrawOceanTexture(DrawingContext context, Rect rect)
        {
            var radius = rect.Width / 2.0;
            var center = rect.Center;
            var wavePen = new Pen(new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)), 1);

            for (int i = 0; i < 3; i++)
            {
                float relativeY = 12 + i * 12;
                float waveRotation = (RotationAngle * 0.8f + i * 60) % 360;
                float relAngle = waveRotation > 180 ? waveRotation - 360 : waveRotation;

                if (relAngle > -90 && relAngle < 90)
                {
                    double angleRad = relAngle * Math.PI / 180.0;
                    double sinFactor = Math.Sin(angleRad);
                    double cosFactor = Math.Cos(angleRad);

                    double x = center.X + sinFactor * radius;
                    double w = rect.Width * 0.4 * cosFactor;

                    if (w > 2)
                    {
                        var arcRect = new Rect(x - w / 2, rect.Y + relativeY, w, 5);
                        var geo = CreateArcGeometry(arcRect, 180, 180);
                        context.DrawGeometry(null, wavePen, geo);
                    }
                }
            }
        }

        private static StreamGeometry CreateArcGeometry(Rect rect, double startAngle, double sweepAngle)
        {
            var geo = new StreamGeometry();
            using var ctx = geo.Open();
            var cx = rect.Center.X;
            var cy = rect.Center.Y;
            var rx = rect.Width / 2;
            var ry = rect.Height / 2;
            var startRad = startAngle * Math.PI / 180;
            var endRad = (startAngle + sweepAngle) * Math.PI / 180;
            var startPt = new Point(cx + rx * Math.Cos(startRad), cy + ry * Math.Sin(startRad));
            var endPt = new Point(cx + rx * Math.Cos(endRad), cy + ry * Math.Sin(endRad));
            ctx.BeginFigure(startPt, false);
            ctx.ArcTo(endPt, new Size(rx, ry), 0, sweepAngle > 180, SweepDirection.Clockwise);
            ctx.EndFigure(false);
            return geo;
        }

        private void DrawContinents(DrawingContext context, Rect rect)
        {
            var radius = rect.Width / 2.0;
            var center = rect.Center;

            var landBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(255, 46, 204, 113), 0),
                    new GradientStop(Color.FromArgb(255, 20, 90, 50), 1)
                }
            };

            var continentSilhouettes = new List<(float X, float Y)[]>
            {
                // Eurasia
                new[] { (0f,-18f),(15f,-20f),(35f,-22f),(55f,-18f),(70f,-10f),(80f,5f),(75f,12f),(50f,15f),(30f,18f),(10f,15f),(-5f,8f),(-15f,0f),(-10f,-12f) },
                // Africa
                new[] { (75f,5f),(95f,8f),(105f,15f),(100f,28f),(90f,35f),(75f,25f),(68f,15f),(70f,8f) },
                // North America
                new[] { (180f,-20f),(210f,-22f),(230f,-15f),(235f,-5f),(220f,10f),(205f,12f),(190f,0f),(185f,-10f) },
                // South America
                new[] { (220f,12f),(240f,15f),(235f,30f),(215f,40f),(205f,30f),(210f,15f) },
                // Australia
                new[] { (285f,12f),(305f,14f),(315f,22f),(305f,32f),(285f,30f),(280f,20f) },
                // Greenland
                new[] { (160f,-22f),(175f,-25f),(180f,-18f),(165f,-15f) }
            };

            foreach (var silhouette in continentSilhouettes)
            {
                var projectedPoints = new List<Point>();
                bool anyVisible = false;

                foreach (var p in silhouette)
                {
                    float lon = (p.X + RotationAngle) % 360;
                    float relAngle = lon > 180 ? lon - 360 : lon;

                    if (relAngle > -100 && relAngle < 100)
                    {
                        double angleRad = relAngle * Math.PI / 180.0;
                        double sinFactor = Math.Sin(angleRad);
                        double x = center.X + sinFactor * radius;
                        double y = center.Y + p.Y;
                        projectedPoints.Add(new Point(x, y));
                        anyVisible = true;
                    }
                }

                if (anyVisible && projectedPoints.Count > 2)
                {
                    var geo = CreateClosedCurveGeometry(projectedPoints);
                    context.DrawGeometry(landBrush, new Pen(new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)), 0.5), geo);
                }
            }
        }

        private StreamGeometry CreateClosedCurveGeometry(List<Point> points)
        {
            var geo = new StreamGeometry();
            using var ctx = geo.Open();
            if (points.Count < 2) return geo;

            ctx.BeginFigure(points[0], true);
            for (int i = 1; i < points.Count; i++)
                ctx.LineTo(points[i]);
            ctx.EndFigure(true);
            return geo;
        }

        private void DrawInnerShadow(DrawingContext context, Rect rect)
        {
            var brush = new RadialGradientBrush
            {
                Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                GradientOrigin = new RelativePoint(0.85, 0.85, RelativeUnit.Relative),
                RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
                RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.85),
                    new GradientStop(Color.FromArgb(120, 0, 0, 0), 1.0)
                }
            };
            context.DrawEllipse(brush, null, rect.Center, rect.Width / 2, rect.Height / 2);
        }

        private void DrawHighlight(DrawingContext context, Rect rect)
        {
            var highlightRect = new Rect(rect.X + 5, rect.Y + 5, 25, 20);
            var brush = new RadialGradientBrush
            {
                Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
                RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(140, 255, 255, 255), 0),
                    new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                }
            };
            context.DrawEllipse(brush, null, highlightRect.Center, highlightRect.Width / 2, highlightRect.Height / 2);
        }
    }

}
