using System.Drawing;
using UIFramework.Core.Interop;
using Xunit;

namespace UIFramework.Tests.Interop
{
    public class DwmTests
    {
        [Fact]
        public void A_colorref_puts_blue_first_and_red_last()
        {
            // COLORREF ist 0x00BBGGRR — genau andersherum als #RRGGBB.
            // R=1, G=2, B=3 muss also 0x00030201 ergeben, nicht 0x00010203.
            Assert.Equal(0x00030201, Dwm.ToColorRef(Color.FromArgb(255, 1, 2, 3)));
        }

        [Fact]
        public void Pure_red_lands_in_the_lowest_byte()
        {
            Assert.Equal(0x000000FF, Dwm.ToColorRef(Color.FromArgb(255, 255, 0, 0)));
        }

        [Fact]
        public void Pure_blue_lands_in_the_highest_colour_byte()
        {
            Assert.Equal(0x00FF0000, Dwm.ToColorRef(Color.FromArgb(255, 0, 0, 255)));
        }

        [Fact]
        public void The_alpha_channel_is_dropped_rather_than_shifted_in()
        {
            // Ein gesetztes Alpha darf das oberste Byte nicht belegen —
            // sonst deutet Windows den Wert als andere Farbe.
            Assert.Equal(0x00030201, Dwm.ToColorRef(Color.FromArgb(0, 1, 2, 3)));
        }

        [Fact]
        public void Black_is_zero_and_white_is_all_colour_bits()
        {
            Assert.Equal(0x00000000, Dwm.ToColorRef(Color.FromArgb(255, 0, 0, 0)));
            Assert.Equal(0x00FFFFFF, Dwm.ToColorRef(Color.FromArgb(255, 255, 255, 255)));
        }
    }
}
