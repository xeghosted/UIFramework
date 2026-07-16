using System;
using System.Windows.Forms;
using UIFramework.Core.Dpi;
using UIFramework.Core.Skinning;
using Xunit;

namespace UIFramework.Tests.Dpi
{
    public class DpiScaleTests
    {
        [Fact]
        public void At_96_dpi_nothing_changes()
        {
            Assert.Equal(10, DpiScale.Scale(10, 96));
        }

        [Theory]
        [InlineData(10, 144, 15)]   // 150 %
        [InlineData(10, 192, 20)]   // 200 %
        [InlineData(10, 120, 13)]   // 125 %, 12,5 → 13 (kaufmännisch)
        [InlineData(1, 144, 2)]     // 1,5 → 2: ein Rahmen darf nie verschwinden
        public void Scaling_rounds_half_away_from_zero(int logical, int dpi, int expected)
        {
            Assert.Equal(expected, DpiScale.Scale(logical, dpi));
        }

        [Fact]
        public void Zero_stays_zero_at_every_dpi()
        {
            Assert.Equal(0, DpiScale.Scale(0, 192));
        }

        [Fact]
        public void Padding_scales_on_all_four_sides()
        {
            var scaled = DpiScale.Scale(new Padding(2, 4, 6, 8), 192);

            Assert.Equal(new Padding(4, 8, 12, 16), scaled);
        }

        [Fact]
        public void CornerRadius_scales_on_all_four_corners()
        {
            var scaled = DpiScale.Scale(new CornerRadius(1, 2, 3, 4), 192);

            Assert.Equal(2, scaled.TopLeft);
            Assert.Equal(4, scaled.TopRight);
            Assert.Equal(6, scaled.BottomRight);
            Assert.Equal(8, scaled.BottomLeft);
        }

        [Theory]
        [InlineData(9f, 96, 12f)]    // 9pt @ 96dpi  = 12px
        [InlineData(9f, 144, 18f)]   // 9pt @ 144dpi = 18px
        [InlineData(12f, 96, 16f)]
        public void Points_convert_to_pixels_via_72_not_96(float points, int dpi, float expected)
        {
            Assert.Equal(expected, DpiScale.PointsToPixels(points, dpi), 3);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-96)]
        public void Scale_rejects_a_nonpositive_dpi(int dpi)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DpiScale.Scale(10, dpi));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-96)]
        public void ScaleF_rejects_a_nonpositive_dpi(int dpi)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DpiScale.ScaleF(10f, dpi));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-96)]
        public void PointsToPixels_rejects_a_nonpositive_dpi(int dpi)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DpiScale.PointsToPixels(9f, dpi));
        }

        [Fact]
        public void ScaleF_at_96_dpi_changes_nothing()
        {
            Assert.Equal(10f, DpiScale.ScaleF(10f, 96), 3);
        }

        [Theory]
        [InlineData(10f, 144, 15f)]     // 150 %
        [InlineData(10f, 192, 20f)]     // 200 %
        [InlineData(1f, 120, 1.25f)]    // 125 % — anders als Scale(int, int) wird hier NICHT gerundet
        public void ScaleF_scales_without_rounding(float logical, int dpi, float expected)
        {
            // Kein Selbstzweck trotz trivialer Implementierung: MainForm.OnLoad
            // haengt daran (Scale(dpi / 96f)) — der Durchreicher ist tragend.
            Assert.Equal(expected, DpiScale.ScaleF(logical, dpi), 3);
        }
    }
}
