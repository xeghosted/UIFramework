using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;
using Xunit;

namespace UIFramework.Tests.Rendering
{
    public class SkinPainterTests
    {
        private static readonly Color Fill = Color.FromArgb(255, 200, 30, 40);
        private static readonly Color Edge = Color.FromArgb(255, 10, 20, 220);

        private static Bitmap Render(int size, System.Action<Graphics> draw)
        {
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.FromArgb(0, 0, 0, 0));
                draw(g);
            }
            return bitmap;
        }

        [Fact]
        public void A_solid_background_fills_the_centre()
        {
            var appearance = new ElementAppearance { Background = Fill, Corners = CornerRadius.None };

            using (var bitmap = Render(40, g =>
                SkinPainter.DrawBackground(g, new Rectangle(0, 0, 40, 40), appearance, 96)))
            {
                Assert.Equal(Fill.ToArgb(), bitmap.GetPixel(20, 20).ToArgb());
            }
        }

        [Fact]
        public void A_square_background_reaches_into_the_corner()
        {
            var appearance = new ElementAppearance { Background = Fill, Corners = CornerRadius.None };

            using (var bitmap = Render(40, g =>
                SkinPainter.DrawBackground(g, new Rectangle(0, 0, 40, 40), appearance, 96)))
            {
                Assert.Equal(Fill.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
            }
        }

        [Fact]
        public void A_rounded_background_leaves_the_corner_transparent()
        {
            var appearance = new ElementAppearance { Background = Fill, Corners = new CornerRadius(10) };

            using (var bitmap = Render(40, g =>
                SkinPainter.DrawBackground(g, new Rectangle(0, 0, 40, 40), appearance, 96)))
            {
                // Die Ecke wird ausgespart, die Mitte nicht.
                Assert.Equal(0, bitmap.GetPixel(0, 0).A);
                Assert.Equal(Fill.ToArgb(), bitmap.GetPixel(20, 20).ToArgb());
            }
        }

        [Fact]
        public void The_corner_radius_grows_with_dpi()
        {
            var appearance = new ElementAppearance { Background = Fill, Corners = new CornerRadius(6) };

            using (var at96 = Render(60, g =>
                SkinPainter.DrawBackground(g, new Rectangle(0, 0, 60, 60), appearance, 96)))
            using (var at192 = Render(60, g =>
                SkinPainter.DrawBackground(g, new Rectangle(0, 0, 60, 60), appearance, 192)))
            {
                // Bei (3,3) ist der kleine Radius schon gefüllt, der doppelte noch nicht.
                Assert.NotEqual(0, at96.GetPixel(3, 3).A);
                Assert.Equal(0, at192.GetPixel(3, 3).A);
            }
        }

        [Fact]
        public void A_border_paints_on_the_edge_but_not_in_the_centre()
        {
            var appearance = new ElementAppearance
            {
                Background = Color.Transparent,
                BorderColor = Edge,
                BorderWidth = 1,
                Corners = CornerRadius.None
            };

            using (var bitmap = Render(40, g =>
                SkinPainter.DrawBorder(g, new Rectangle(0, 0, 40, 40), appearance, 96)))
            {
                Assert.Equal(Edge.ToArgb(), bitmap.GetPixel(20, 0).ToArgb());
                Assert.Equal(0, bitmap.GetPixel(20, 20).A);
            }
        }

        [Fact]
        public void A_zero_width_border_paints_nothing()
        {
            var appearance = new ElementAppearance
            {
                Background = Color.Transparent,
                BorderColor = Edge,
                BorderWidth = 0
            };

            using (var bitmap = Render(40, g =>
                SkinPainter.DrawBorder(g, new Rectangle(0, 0, 40, 40), appearance, 96)))
            {
                Assert.Equal(0, bitmap.GetPixel(20, 0).A);
            }
        }

        [Fact]
        public void An_empty_rectangle_is_survived_without_an_exception()
        {
            var appearance = new ElementAppearance { Background = Fill, BorderColor = Edge, BorderWidth = 1 };

            using (var bitmap = new Bitmap(10, 10))
            using (var g = Graphics.FromImage(bitmap))
            {
                SkinPainter.DrawBackground(g, new Rectangle(0, 0, 0, 0), appearance, 96);
                SkinPainter.DrawBorder(g, new Rectangle(0, 0, 0, 0), appearance, 96);
            }
        }

        [Fact]
        public void Text_is_measured_larger_at_higher_dpi()
        {
            var appearance = new ElementAppearance { Font = new FontSpec("Segoe UI", 9f), ForeColor = Edge };

            using (var bitmap = new Bitmap(10, 10))
            using (var g = Graphics.FromImage(bitmap))
            {
                var at96 = SkinPainter.MeasureText(g, "Beispiel", appearance, 96);
                var at192 = SkinPainter.MeasureText(g, "Beispiel", appearance, 192);

                Assert.True(at192.Width > at96.Width);
                Assert.True(at192.Height > at96.Height);
            }
        }

        [Fact]
        public void Null_or_empty_text_measures_to_nothing_and_paints_nothing()
        {
            var appearance = new ElementAppearance { Font = new FontSpec("Segoe UI", 9f), ForeColor = Edge };

            using (var bitmap = new Bitmap(10, 10))
            using (var g = Graphics.FromImage(bitmap))
            {
                Assert.Equal(Size.Empty, SkinPainter.MeasureText(g, null, appearance, 96));
                Assert.Equal(Size.Empty, SkinPainter.MeasureText(g, "", appearance, 96));

                SkinPainter.DrawText(g, null, new Rectangle(0, 0, 10, 10), appearance, 96, ContentAlignment.MiddleCenter);
            }
        }
    }
}
