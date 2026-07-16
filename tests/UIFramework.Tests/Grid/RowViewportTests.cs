using System;
using UIFramework.Grid.Layout;
using Xunit;

namespace UIFramework.Tests.Grid
{
    public class RowViewportTests
    {
        // Durchgaengig: Zeilenhoehe 30, Sichtfenster 100 hoch.
        // Sichtbar sind damit vier Zeilen, die letzte angeschnitten.

        [Fact]
        public void At_the_top_the_first_row_is_row_zero()
        {
            var v = new RowViewport(30, 100, 0, 1000);

            Assert.Equal(0, v.FirstVisibleRow);
        }

        [Fact]
        public void A_partially_visible_row_at_the_bottom_still_counts()
        {
            // 100 / 30 = 3,33 -> Zeilen 0,1,2 ganz und 3 angeschnitten.
            // Wer abrundet, laesst am unteren Rand einen ungezeichneten Streifen.
            var v = new RowViewport(30, 100, 0, 1000);

            Assert.Equal(4, v.VisibleRowCount);
        }

        [Fact]
        public void A_partially_visible_row_at_the_top_still_counts()
        {
            // Versatz 15: Zeile 0 ist zur Haelfte da und muss gezeichnet werden.
            var v = new RowViewport(30, 100, 15, 1000);

            Assert.Equal(0, v.FirstVisibleRow);
            Assert.Equal(4, v.VisibleRowCount);
        }

        [Fact]
        public void Scrolling_exactly_one_row_moves_the_window_by_one()
        {
            var v = new RowViewport(30, 100, 30, 1000);

            Assert.Equal(1, v.FirstVisibleRow);
        }

        [Fact]
        public void The_top_of_the_first_row_is_negative_when_it_is_cut_off()
        {
            var v = new RowViewport(30, 100, 15, 1000);

            Assert.Equal(-15, v.RowTop(0));
        }

        [Fact]
        public void Row_tops_follow_the_row_height()
        {
            var v = new RowViewport(30, 100, 0, 1000);

            Assert.Equal(0, v.RowTop(0));
            Assert.Equal(30, v.RowTop(1));
            Assert.Equal(90, v.RowTop(3));
        }

        [Fact]
        public void A_point_maps_back_to_the_row_under_it()
        {
            var v = new RowViewport(30, 100, 0, 1000);

            Assert.Equal(0, v.RowAt(0));
            Assert.Equal(0, v.RowAt(29));
            Assert.Equal(1, v.RowAt(30));
            Assert.Equal(3, v.RowAt(95));
        }

        [Fact]
        public void A_point_maps_back_correctly_when_scrolled()
        {
            var v = new RowViewport(30, 100, 15, 1000);

            // y=0 liegt in Zeile 0 (die bei -15 beginnt), y=20 schon in Zeile 1.
            Assert.Equal(0, v.RowAt(0));
            Assert.Equal(1, v.RowAt(20));
        }

        [Fact]
        public void A_point_below_the_last_row_belongs_to_no_row()
        {
            // Drei Zeilen insgesamt, y=100 liegt darunter. Ein falsches "Zeile 3"
            // liesse einen Klick ins Leere eine Zeile auswaehlen, die es nicht gibt.
            var v = new RowViewport(30, 200, 0, 3);

            Assert.Equal(-1, v.RowAt(100));
        }

        [Fact]
        public void A_negative_point_belongs_to_no_row()
        {
            var v = new RowViewport(30, 100, 0, 1000);

            Assert.Equal(-1, v.RowAt(-1));
        }

        [Fact]
        public void An_empty_source_shows_nothing()
        {
            var v = new RowViewport(30, 100, 0, 0);

            Assert.Equal(0, v.VisibleRowCount);
            Assert.Equal(0, v.TotalHeight);
            Assert.Equal(0, v.MaxScrollOffset);
            Assert.Equal(-1, v.RowAt(10));
        }

        [Fact]
        public void Fewer_rows_than_fit_are_all_visible_and_nothing_scrolls()
        {
            var v = new RowViewport(30, 100, 0, 2);

            Assert.Equal(2, v.VisibleRowCount);
            Assert.Equal(0, v.MaxScrollOffset);
        }

        [Fact]
        public void At_the_very_bottom_the_last_row_is_visible()
        {
            // 10 Zeilen a 30 = 300 gesamt, Sichtfenster 100 -> max. Versatz 200.
            var v = new RowViewport(30, 100, 200, 10);

            Assert.Equal(200, v.MaxScrollOffset);
            Assert.Equal(6, v.FirstVisibleRow);
            Assert.Equal(4, v.VisibleRowCount);   // Zeilen 6,7,8,9
        }

        [Fact]
        public void The_visible_count_never_runs_past_the_last_row()
        {
            // Sichtfenster hoeher als der Inhalt: Es gibt nur 3 Zeilen.
            var v = new RowViewport(30, 500, 0, 3);

            Assert.Equal(3, v.VisibleRowCount);
        }

        [Fact]
        public void An_offset_beyond_the_content_shows_nothing_rather_than_throwing()
        {
            // Kann waehrend eines Layout-Wechsels auftreten (Quelle schrumpft,
            // der Versatz ist noch der alte). Ein Wurf hier waere ein Absturz
            // mitten im Zeichnen.
            var v = new RowViewport(30, 100, 99999, 3);

            Assert.Equal(0, v.VisibleRowCount);
        }

        [Fact]
        public void A_negative_offset_is_treated_as_the_top()
        {
            var v = new RowViewport(30, 100, -50, 1000);

            Assert.Equal(0, v.FirstVisibleRow);
            Assert.Equal(0, v.RowTop(0));
        }

        [Fact]
        public void A_million_rows_do_not_overflow_the_total_height()
        {
            // 1.000.000 x 30 = 30.000.000 — passt in int, aber nur knapp genug,
            // dass es geprueft gehoert. Bei 100px Zeilenhoehe waeren es 100 Mio.
            var v = new RowViewport(30, 100, 0, 1000000);

            Assert.Equal(30000000, v.TotalHeight);
            Assert.True(v.MaxScrollOffset > 0);
        }

        [Fact]
        public void A_million_rows_still_only_show_a_handful()
        {
            // Der Kern der Sache, auf der Ebene der reinen Rechnung.
            var v = new RowViewport(30, 100, 0, 1000000);

            Assert.Equal(4, v.VisibleRowCount);
        }

        [Fact]
        public void A_nonpositive_row_height_is_a_programming_error()
        {
            // Sonst teilte FirstVisibleRow durch null.
            Assert.Throws<ArgumentOutOfRangeException>(() => new RowViewport(0, 100, 0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RowViewport(-5, 100, 0, 10));
        }

        [Fact]
        public void A_negative_row_count_is_a_programming_error()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RowViewport(30, 100, 0, -1));
        }
    }
}
