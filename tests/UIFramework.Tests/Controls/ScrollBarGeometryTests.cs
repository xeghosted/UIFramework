using System;
using UIFramework.Controls;
using Xunit;

namespace UIFramework.Tests.Controls
{
    public class ScrollBarGeometryTests
    {
        // Die WinForms-Konvention, die dieser Typ übernimmt: der größte
        // erreichbare Wert ist Maximum - LargeChange + 1, nicht Maximum.
        // LargeChange ist die Größe des sichtbaren Ausschnitts.

        [Fact]
        public void Nothing_to_scroll_when_everything_fits()
        {
            var g = new ScrollBarGeometry(200, 0, 50, 0, 100, 20);

            Assert.False(g.IsScrollable);
        }

        [Fact]
        public void The_thumb_fills_the_track_when_everything_fits()
        {
            var g = new ScrollBarGeometry(200, 0, 50, 0, 100, 20);

            Assert.Equal(200, g.ThumbLength);
            Assert.Equal(0, g.ThumbOffset);
        }

        [Fact]
        public void The_thumb_is_proportional_to_the_visible_share()
        {
            // 100 sichtbar von 400 gesamt -> ein Viertel der Rinne.
            var g = new ScrollBarGeometry(200, 0, 400, 0, 100, 10);

            Assert.Equal(50, g.ThumbLength);
        }

        [Fact]
        public void The_thumb_never_shrinks_below_the_minimum()
        {
            // 10 sichtbar von 1.000.000 -> rechnerisch 0px. Ein unsichtbarer
            // Daumen liesse sich nicht greifen.
            var g = new ScrollBarGeometry(200, 0, 1000000, 0, 10, 16);

            Assert.Equal(16, g.ThumbLength);
        }

        [Fact]
        public void At_the_minimum_the_thumb_sits_at_the_start()
        {
            var g = new ScrollBarGeometry(200, 0, 400, 0, 100, 10);

            Assert.Equal(0, g.ThumbOffset);
        }

        [Fact]
        public void At_the_maximum_the_thumb_ends_flush_with_the_track()
        {
            // Groesster erreichbarer Wert = 400 - 100 + 1 = 301.
            var g = new ScrollBarGeometry(200, 0, 400, 301, 100, 10);

            Assert.Equal(200, g.ThumbOffset + g.ThumbLength);
        }

        [Fact]
        public void Halfway_puts_the_thumb_halfway_down_the_free_track()
        {
            // Wertebereich 0..301, Mitte ~150. Freie Rinne = 200 - 50 = 150.
            var g = new ScrollBarGeometry(200, 0, 400, 150, 100, 10);

            // 150/301 * 150 = 74,75 -> 75 (kaufmaennisch, wie DpiScale)
            Assert.Equal(75, g.ThumbOffset);
        }

        [Fact]
        public void Dragging_maps_a_track_position_back_to_a_value()
        {
            var g = new ScrollBarGeometry(200, 0, 400, 0, 100, 10);

            Assert.Equal(0, g.ValueAt(0));
            Assert.Equal(301, g.ValueAt(150));
        }

        [Fact]
        public void A_position_beyond_the_track_clamps_instead_of_overshooting()
        {
            var g = new ScrollBarGeometry(200, 0, 400, 0, 100, 10);

            Assert.Equal(0, g.ValueAt(-50));
            Assert.Equal(301, g.ValueAt(9999));
        }

        [Theory]
        [InlineData(-5, 0)]      // unter Minimum
        [InlineData(0, 0)]
        [InlineData(150, 150)]
        [InlineData(301, 301)]   // groesster erreichbarer Wert
        [InlineData(400, 301)]   // Maximum selbst ist NICHT erreichbar
        [InlineData(9999, 301)]
        public void Values_are_clamped_to_the_reachable_range(int input, int expected)
        {
            Assert.Equal(expected, ScrollBarGeometry.ClampValue(input, 0, 400, 100));
        }

        [Fact]
        public void An_inverted_range_does_not_throw_and_reports_nothing_to_scroll()
        {
            // Maximum <= Minimum kommt vor, wenn eine Quelle leer ist.
            var g = new ScrollBarGeometry(200, 100, 100, 100, 10, 16);

            Assert.False(g.IsScrollable);
            Assert.Equal(100, ScrollBarGeometry.ClampValue(999, 100, 100, 10));
        }

        [Fact]
        public void A_track_shorter_than_the_minimum_thumb_does_not_produce_a_negative_free_track()
        {
            // Sonst rechnete ValueAt mit einer negativen Division.
            var g = new ScrollBarGeometry(10, 0, 400, 50, 100, 16);

            Assert.True(g.ThumbLength <= 10);
            Assert.True(g.ThumbOffset >= 0);
            Assert.True(g.ThumbOffset + g.ThumbLength <= 10);
        }

        [Fact]
        public void A_nonpositive_track_length_is_a_programming_error()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ScrollBarGeometry(0, 0, 400, 0, 100, 16));
        }
    }
}
