using System.Drawing;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;
using Xunit;

namespace UIFramework.Tests.Rendering
{
    public class ResourceCacheTests
    {
        [Fact]
        public void The_same_colour_returns_the_very_same_brush()
        {
            using (var cache = new ResourceCache())
            {
                var first = cache.GetBrush(Color.FromArgb(255, 1, 2, 3));
                var second = cache.GetBrush(Color.FromArgb(255, 1, 2, 3));

                Assert.Same(first, second);
            }
        }

        [Fact]
        public void Different_colours_return_different_brushes()
        {
            using (var cache = new ResourceCache())
            {
                var first = cache.GetBrush(Color.FromArgb(255, 1, 2, 3));
                var second = cache.GetBrush(Color.FromArgb(255, 3, 2, 1));

                Assert.NotSame(first, second);
            }
        }

        [Fact]
        public void Pens_are_keyed_by_colour_and_width()
        {
            using (var cache = new ResourceCache())
            {
                var a = cache.GetPen(Color.Red, 1);
                var b = cache.GetPen(Color.Red, 1);
                var c = cache.GetPen(Color.Red, 2);

                Assert.Same(a, b);
                Assert.NotSame(a, c);
            }
        }

        [Fact]
        public void Fonts_are_keyed_by_spec_and_dpi()
        {
            using (var cache = new ResourceCache())
            {
                var spec = new FontSpec("Segoe UI", 9f);

                var at96 = cache.GetFont(spec, 96);
                var again96 = cache.GetFont(spec, 96);
                var at144 = cache.GetFont(spec, 144);

                Assert.Same(at96, again96);
                Assert.NotSame(at96, at144);
            }
        }

        [Fact]
        public void A_font_is_created_in_pixels_so_gdi_does_not_scale_twice()
        {
            using (var cache = new ResourceCache())
            {
                var font = cache.GetFont(new FontSpec("Segoe UI", 9f), 144);

                Assert.Equal(GraphicsUnit.Pixel, font.Unit);
                Assert.Equal(18f, font.Size, 3);   // 9pt * 144 / 72
            }
        }

        [Fact]
        public void Clear_disposes_everything_it_handed_out()
        {
            var cache = new ResourceCache();
            var brush = cache.GetBrush(Color.Red);
            var pen = cache.GetPen(Color.Blue, 1);
            var font = cache.GetFont(new FontSpec("Segoe UI", 9f), 96);

            cache.Clear();

            Assert.Equal(0, cache.Count);

            // Hinweis: SolidBrush.Color, Pen.Color und Font.Size lesen bei .NET
            // Framework nur ein gecachtes verwaltetes Feld — sie rühren das native
            // GDI+-Handle nicht an und werfen deshalb auf einem disposten Objekt
            // NICHT. Erst eine echte Zeichenoperation greift auf das native Handle
            // zu und deckt die Disposal auf (ArgumentException ist GDI+-Eigenart).
            using (var bitmap = new Bitmap(1, 1))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                Assert.Throws<System.ArgumentException>(() => graphics.FillRectangle(brush, 0, 0, 1, 1));
                Assert.Throws<System.ArgumentException>(() => graphics.DrawLine(pen, 0, 0, 1, 1));
                Assert.Throws<System.ArgumentException>(() => graphics.DrawString("x", font, brush, 0, 0));
            }

            cache.Dispose();
        }

        [Fact]
        public void After_Clear_the_cache_hands_out_fresh_objects()
        {
            using (var cache = new ResourceCache())
            {
                var before = cache.GetBrush(Color.Red);
                cache.Clear();
                var after = cache.GetBrush(Color.Red);

                Assert.NotSame(before, after);
            }
        }
    }
}
