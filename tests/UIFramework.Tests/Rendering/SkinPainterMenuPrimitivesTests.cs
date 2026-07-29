using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;
using Xunit;

namespace UIFramework.Tests.Rendering
{
    public class SkinPainterMenuPrimitivesTests
    {
        private static ElementAppearance Plain(Color fore, Color border, int borderWidth, Padding padding)
        {
            return new ElementAppearance
            {
                Background = Color.White,
                BorderColor = border,
                BorderWidth = borderWidth,
                Corners = CornerRadius.None,
                ForeColor = fore,
                Font = new FontSpec("Segoe UI", 9f),
                Padding = padding
            };
        }

        [Fact]
        public void A_mnemonic_marker_does_not_widen_the_text()
        {
            // "&Datei" wird als "Datei" (mit Unterstrich) gezeichnet — die Breite
            // muss der von "Datei" entsprechen, sonst stimmt kein Menü-Layout.
            using (var bmp = new Bitmap(8, 8))
            using (var g = Graphics.FromImage(bmp))
            {
                var appearance = Plain(Color.Black, Color.Black, 0, Padding.Empty);

                var with = SkinPainter.MeasureMnemonicText(g, "&Datei", appearance, 96);
                var without = SkinPainter.MeasureMnemonicText(g, "Datei", appearance, 96);

                Assert.Equal(without, with);
            }
        }

        [Fact]
        public void A_doubled_ampersand_measures_like_a_single_literal_one()
        {
            using (var bmp = new Bitmap(8, 8))
            using (var g = Graphics.FromImage(bmp))
            {
                var appearance = Plain(Color.Black, Color.Black, 0, Padding.Empty);

                var doubled = SkinPainter.MeasureMnemonicText(g, "A && B", appearance, 96);
                var literal = SkinPainter.MeasureText(g, "A & B", appearance, 96);

                Assert.Equal(literal.Width, doubled.Width);
            }
        }

        [Fact]
        public void The_separator_line_paints_border_color_pixels_in_the_middle()
        {
            using (var bmp = new Bitmap(40, 10))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                var appearance = Plain(Color.Black, Color.Red, 2, new Padding(4, 0, 4, 0));

                SkinPainter.DrawSeparatorLine(g, new Rectangle(0, 0, 40, 10), appearance, 96);

                // Mittig, innerhalb des horizontalen Paddings: Linienfarbe.
                Assert.Equal(Color.Red.ToArgb(), bmp.GetPixel(20, 5).ToArgb());
                // Im Padding-Bereich links: unberührt.
                Assert.Equal(Color.White.ToArgb(), bmp.GetPixel(1, 5).ToArgb());
            }
        }

        [Fact]
        public void A_zero_width_separator_appearance_paints_nothing()
        {
            using (var bmp = new Bitmap(40, 10))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                var appearance = Plain(Color.Black, Color.Red, 0, Padding.Empty);

                SkinPainter.DrawSeparatorLine(g, new Rectangle(0, 0, 40, 10), appearance, 96);

                Assert.Equal(Color.White.ToArgb(), bmp.GetPixel(20, 5).ToArgb());
            }
        }
    }
}
