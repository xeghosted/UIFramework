using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;
using Xunit;

namespace UIFramework.Tests.Rendering
{
    public class SkinPainterImageTests
    {
        private static Bitmap RedSquare()
        {
            var source = new Bitmap(4, 4);
            using (var g = Graphics.FromImage(source)) g.Clear(Color.Red);
            return source;
        }

        [Fact]
        public void An_enabled_image_fills_the_target_zone_scaled()
        {
            using (var source = RedSquare())
            using (var target = new Bitmap(40, 40))
            using (var g = Graphics.FromImage(target))
            {
                g.Clear(Color.White);

                SkinPainter.DrawScaledImage(g, source, new Rectangle(4, 4, 32, 32), true);

                Assert.Equal(Color.Red.ToArgb(), target.GetPixel(20, 20).ToArgb()); // Mitte: skaliert gefüllt
                Assert.Equal(Color.White.ToArgb(), target.GetPixel(1, 1).ToArgb()); // außerhalb: unberührt
            }
        }

        [Fact]
        public void A_disabled_image_is_visibly_faded_not_hidden()
        {
            using (var source = RedSquare())
            using (var target = new Bitmap(40, 40))
            using (var g = Graphics.FromImage(target))
            {
                g.Clear(Color.White);

                SkinPainter.DrawScaledImage(g, source, new Rectangle(4, 4, 32, 32), false);

                var center = target.GetPixel(20, 20);
                Assert.NotEqual(Color.Red.ToArgb(), center.ToArgb());   // nicht voll
                Assert.NotEqual(Color.White.ToArgb(), center.ToArgb()); // nicht weg
                Assert.True(center.R > center.G && center.R > center.B, // aber erkennbar rot
                    "Ausgegraut muss die Bildfarbe noch tragen, war " + center);
            }
        }

        [Fact]
        public void A_null_image_draws_nothing_and_does_not_throw()
        {
            using (var target = new Bitmap(10, 10))
            using (var g = Graphics.FromImage(target))
            {
                g.Clear(Color.White);
                SkinPainter.DrawScaledImage(g, null, new Rectangle(0, 0, 10, 10), true);
                Assert.Equal(Color.White.ToArgb(), target.GetPixel(5, 5).ToArgb());
            }
        }

        [Fact]
        public void The_vertical_separator_paints_border_color_in_the_middle_and_respects_padding()
        {
            using (var target = new Bitmap(10, 40))
            using (var g = Graphics.FromImage(target))
            {
                g.Clear(Color.White);
                var appearance = new ElementAppearance
                {
                    Background = Color.White,
                    BorderColor = Color.Red,
                    BorderWidth = 2,
                    Corners = CornerRadius.None,
                    ForeColor = Color.Black,
                    Font = new FontSpec("Segoe UI", 9f),
                    Padding = new Padding(0, 4, 0, 4)
                };

                SkinPainter.DrawVerticalSeparatorLine(g, new Rectangle(0, 0, 10, 40), appearance, 96);

                Assert.Equal(Color.Red.ToArgb(), target.GetPixel(5, 20).ToArgb()); // Mitte
                Assert.Equal(Color.White.ToArgb(), target.GetPixel(5, 1).ToArgb()); // im Padding oben
            }
        }
    }
}
