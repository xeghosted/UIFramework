using System;
using UIFramework.Grid.Layout;
using Xunit;

namespace UIFramework.Tests.Grid
{
    public class ScrollBarReservationTests
    {
        // Alle Werte physische Pixel. Basisfall: Client 200x100, Kopf 20,
        // also 80 Inhaltsfläche unter dem Kopf, Leistendicke 12.

        [Fact]
        public void Fitting_content_shows_no_bars_and_keeps_the_full_area()
        {
            var r = new ScrollBarReservation(200, 100, 20, contentWidth: 100, contentHeight: 80, barThickness: 12);

            Assert.False(r.VerticalVisible);
            Assert.False(r.HorizontalVisible);
            Assert.Equal(200, r.ViewportWidth);
            Assert.Equal(80, r.ViewportHeight);
        }

        [Fact]
        public void Tall_content_shows_the_vertical_bar_and_narrows_the_viewport()
        {
            var r = new ScrollBarReservation(200, 100, 20, contentWidth: 100, contentHeight: 500, barThickness: 12);

            Assert.True(r.VerticalVisible);
            Assert.False(r.HorizontalVisible);
            Assert.Equal(188, r.ViewportWidth);
            Assert.Equal(80, r.ViewportHeight);
        }

        [Fact]
        public void Wide_content_shows_the_horizontal_bar_and_shortens_the_viewport()
        {
            var r = new ScrollBarReservation(200, 100, 20, contentWidth: 500, contentHeight: 60, barThickness: 12);

            Assert.False(r.VerticalVisible);
            Assert.True(r.HorizontalVisible);
            Assert.Equal(200, r.ViewportWidth);
            Assert.Equal(68, r.ViewportHeight);
        }

        [Fact]
        public void The_vertical_bar_can_force_the_horizontal_one()
        {
            // Inhalt 195 breit passt in 200 — aber nicht mehr in 188, sobald die
            // senkrechte Leiste ihren Platz nimmt. Genau der 2a-Befund.
            var r = new ScrollBarReservation(200, 100, 20, contentWidth: 195, contentHeight: 500, barThickness: 12);

            Assert.True(r.VerticalVisible);
            Assert.True(r.HorizontalVisible);
            Assert.Equal(188, r.ViewportWidth);
            Assert.Equal(68, r.ViewportHeight);
        }

        [Fact]
        public void The_horizontal_bar_can_force_the_vertical_one()
        {
            // Inhalt 80 hoch passt in 80 — aber nicht mehr in 68, sobald die
            // waagerechte Leiste ihren Platz nimmt.
            var r = new ScrollBarReservation(200, 100, 20, contentWidth: 500, contentHeight: 80, barThickness: 12);

            Assert.True(r.VerticalVisible);
            Assert.True(r.HorizontalVisible);
            Assert.Equal(188, r.ViewportWidth);
            Assert.Equal(68, r.ViewportHeight);
        }

        [Fact]
        public void Empty_content_never_shows_bars_even_in_a_tiny_client()
        {
            var r = new ScrollBarReservation(10, 10, 20, contentWidth: 0, contentHeight: 0, barThickness: 12);

            Assert.False(r.VerticalVisible);
            Assert.False(r.HorizontalVisible);
        }

        [Fact]
        public void A_tiny_client_clamps_the_viewport_to_zero_instead_of_negative()
        {
            var r = new ScrollBarReservation(10, 10, 20, contentWidth: 500, contentHeight: 500, barThickness: 12);

            Assert.True(r.ViewportWidth >= 0);
            Assert.True(r.ViewportHeight >= 0);
        }

        [Fact]
        public void A_non_positive_bar_thickness_throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ScrollBarReservation(200, 100, 20, 100, 100, 0));
        }
    }
}
