using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using UIFramework.Core.Dpi;
using UIFramework.Core.Skinning;

namespace UIFramework.Core.Rendering
{
    /// <summary>
    /// Bündelt sämtliche GDI+-Aufrufe des Frameworks an einer Stelle.
    ///
    /// Zustandslos und ohne Kenntnis von Controls: bekommt Graphics, Rectangle,
    /// ElementAppearance und dpi — sonst nichts. Genau deshalb kann das DataGrid
    /// (Teilprojekt 2) diese Methoden später pro Zelle rufen, wo es gar keine
    /// Control-Instanzen gibt.
    ///
    /// Dies ist auch der einzige Ort, an dem ein Direct2D-Backend andocken müsste.
    /// </summary>
    public static class SkinPainter
    {
        public static void DrawBackground(Graphics g, Rectangle bounds, ElementAppearance appearance, int dpi)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            if (appearance.Background.A == 0 && !appearance.HasGradient) return;

            var corners = DpiScale.Scale(appearance.Corners, dpi);
            var previousMode = g.SmoothingMode;
            g.SmoothingMode = corners.IsZero ? SmoothingMode.None : SmoothingMode.AntiAlias;

            try
            {
                using (var path = RoundedRectangle.Create(bounds, corners))
                {
                    if (appearance.HasGradient)
                    {
                        using (var brush = new LinearGradientBrush(
                            bounds, appearance.Background, appearance.BackgroundGradientEnd.Value,
                            LinearGradientMode.Vertical))
                        {
                            g.FillPath(brush, path);
                        }
                    }
                    else
                    {
                        g.FillPath(ResourceCache.Shared.GetBrush(appearance.Background), path);
                    }
                }
            }
            finally
            {
                g.SmoothingMode = previousMode;
            }
        }

        public static void DrawBorder(Graphics g, Rectangle bounds, ElementAppearance appearance, int dpi)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            if (appearance.BorderWidth <= 0 || appearance.BorderColor.A == 0) return;

            int width = DpiScale.Scale(appearance.BorderWidth, dpi);
            if (width <= 0) return;

            var corners = DpiScale.Scale(appearance.Corners, dpi);
            var previousMode = g.SmoothingMode;
            g.SmoothingMode = corners.IsZero ? SmoothingMode.None : SmoothingMode.AntiAlias;

            try
            {
                // Der Stift zeichnet mittig auf dem Pfad: ohne dieses Einrücken
                // läge die halbe Rahmenbreite außerhalb der Bounds.
                int inset = width / 2;
                var rect = Rectangle.Inflate(bounds, -inset, -inset);
                if (rect.Width <= 0 || rect.Height <= 0) return;

                using (var path = RoundedRectangle.Create(rect, corners))
                {
                    var pen = ResourceCache.Shared.GetPen(appearance.BorderColor, width);
                    pen.Alignment = PenAlignment.Center;
                    g.DrawPath(pen, path);
                }
            }
            finally
            {
                g.SmoothingMode = previousMode;
            }
        }

        public static void DrawText(Graphics g, string text, Rectangle bounds, ElementAppearance appearance,
            int dpi, ContentAlignment alignment)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (string.IsNullOrEmpty(text)) return;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            var font = ResourceCache.Shared.GetFont(appearance.Font, dpi);

            // TextRenderer statt Graphics.DrawString: ClearType. Bei 9pt ist der
            // Unterschied deutlich sichtbar.
            TextRenderer.DrawText(g, text, font, bounds, appearance.ForeColor, ToTextFormatFlags(alignment));
        }

        public static Size MeasureText(Graphics g, string text, ElementAppearance appearance, int dpi)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));
            if (string.IsNullOrEmpty(text)) return Size.Empty;

            var font = ResourceCache.Shared.GetFont(appearance.Font, dpi);
            return TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        public static void DrawFocus(Graphics g, Rectangle bounds, ElementAppearance appearance, int dpi)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (appearance == null) throw new ArgumentNullException(nameof(appearance));

            var padding = DpiScale.Scale(appearance.Padding, dpi);
            var rect = new Rectangle(
                bounds.Left + padding.Left,
                bounds.Top + padding.Top,
                bounds.Width - padding.Horizontal,
                bounds.Height - padding.Vertical);

            if (rect.Width <= 0 || rect.Height <= 0) return;

            DrawBorder(g, rect, appearance, dpi);
        }

        private static TextFormatFlags ToTextFormatFlags(ContentAlignment alignment)
        {
            var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;

            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    return flags | TextFormatFlags.Top | TextFormatFlags.Left;
                case ContentAlignment.TopCenter:
                    return flags | TextFormatFlags.Top | TextFormatFlags.HorizontalCenter;
                case ContentAlignment.TopRight:
                    return flags | TextFormatFlags.Top | TextFormatFlags.Right;
                case ContentAlignment.MiddleLeft:
                    return flags | TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
                case ContentAlignment.MiddleRight:
                    return flags | TextFormatFlags.VerticalCenter | TextFormatFlags.Right;
                case ContentAlignment.BottomLeft:
                    return flags | TextFormatFlags.Bottom | TextFormatFlags.Left;
                case ContentAlignment.BottomCenter:
                    return flags | TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter;
                case ContentAlignment.BottomRight:
                    return flags | TextFormatFlags.Bottom | TextFormatFlags.Right;
                default:
                    return flags | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter;
            }
        }
    }
}
