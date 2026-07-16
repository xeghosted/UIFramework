using System;
using UIFramework.Grid;
using UIFramework.Grid.Layout;
using Xunit;

namespace UIFramework.Tests.Grid
{
    public class ColumnLayoutTests
    {
        // Drei Spalten a 100 logisch. Bei 96 dpi also 100 physisch.
        private static GridColumnCollection ThreeColumns()
        {
            var columns = new GridColumnCollection();
            columns.Add(new GridColumn("A", "A") { Width = 100 });
            columns.Add(new GridColumn("B", "B") { Width = 100 });
            columns.Add(new GridColumn("C", "C") { Width = 100 });
            return columns;
        }

        [Fact]
        public void At_the_left_the_first_column_is_column_zero()
        {
            var layout = new ColumnLayout(ThreeColumns(), 0, 250, 96);

            Assert.Equal(0, layout.FirstVisibleColumn);
        }

        [Fact]
        public void A_partially_visible_column_at_the_right_still_counts()
        {
            // Sichtfenster 250 breit: A (0..100), B (100..200), C angeschnitten.
            var layout = new ColumnLayout(ThreeColumns(), 0, 250, 96);

            Assert.Equal(3, layout.VisibleColumnCount);
        }

        [Fact]
        public void Column_lefts_accumulate()
        {
            var layout = new ColumnLayout(ThreeColumns(), 0, 400, 96);

            Assert.Equal(0, layout.ColumnLeft(0));
            Assert.Equal(100, layout.ColumnLeft(1));
            Assert.Equal(200, layout.ColumnLeft(2));
        }

        [Fact]
        public void Scrolling_right_shifts_the_lefts_and_skips_columns()
        {
            var layout = new ColumnLayout(ThreeColumns(), 150, 250, 96);

            // A liegt jetzt bei -150 und ist ganz draussen; B bei -50, angeschnitten.
            Assert.Equal(1, layout.FirstVisibleColumn);
            Assert.Equal(-50, layout.ColumnLeft(1));
        }

        [Fact]
        public void The_width_is_scaled_by_dpi_here_and_nowhere_else()
        {
            // 100 logisch bei 144 dpi (150 %) = 150 physisch.
            var layout = new ColumnLayout(ThreeColumns(), 0, 400, 144);

            Assert.Equal(150, layout.ColumnWidth(0));
            Assert.Equal(150, layout.ColumnLeft(1));
        }

        [Fact]
        public void The_total_width_is_scaled_too()
        {
            var layout = new ColumnLayout(ThreeColumns(), 0, 400, 144);

            Assert.Equal(450, layout.TotalWidth);
        }

        [Fact]
        public void A_point_maps_back_to_the_column_under_it()
        {
            var layout = new ColumnLayout(ThreeColumns(), 0, 400, 96);

            Assert.Equal(0, layout.ColumnAt(0));
            Assert.Equal(0, layout.ColumnAt(99));
            Assert.Equal(1, layout.ColumnAt(100));
            Assert.Equal(2, layout.ColumnAt(250));
        }

        [Fact]
        public void A_point_maps_back_correctly_when_scrolled()
        {
            var layout = new ColumnLayout(ThreeColumns(), 150, 250, 96);

            // x=0 liegt in B (die bei -50 beginnt), x=60 schon in C.
            Assert.Equal(1, layout.ColumnAt(0));
            Assert.Equal(2, layout.ColumnAt(60));
        }

        [Fact]
        public void A_point_right_of_the_last_column_belongs_to_no_column()
        {
            // Sonst waehlte ein Klick in die Leere rechts eine Spalte aus.
            var layout = new ColumnLayout(ThreeColumns(), 0, 400, 96);

            Assert.Equal(-1, layout.ColumnAt(350));
        }

        [Fact]
        public void A_negative_point_belongs_to_no_column()
        {
            var layout = new ColumnLayout(ThreeColumns(), 0, 400, 96);

            Assert.Equal(-1, layout.ColumnAt(-1));
        }

        [Fact]
        public void No_columns_at_all_is_not_an_error()
        {
            var layout = new ColumnLayout(new GridColumnCollection(), 0, 400, 96);

            Assert.Equal(0, layout.VisibleColumnCount);
            Assert.Equal(0, layout.TotalWidth);
            Assert.Equal(0, layout.MaxScrollOffset);
            Assert.Equal(-1, layout.ColumnAt(10));
        }

        [Fact]
        public void Columns_narrower_than_the_viewport_do_not_scroll()
        {
            var layout = new ColumnLayout(ThreeColumns(), 0, 999, 96);

            Assert.Equal(0, layout.MaxScrollOffset);
        }

        [Fact]
        public void The_max_offset_leaves_the_last_column_flush_with_the_right_edge()
        {
            // 300 gesamt, Sichtfenster 250 -> 50.
            var layout = new ColumnLayout(ThreeColumns(), 0, 250, 96);

            Assert.Equal(50, layout.MaxScrollOffset);
        }

        [Fact]
        public void A_column_wider_than_the_viewport_is_still_the_only_visible_one()
        {
            var columns = new GridColumnCollection();
            columns.Add(new GridColumn("Breit", "Breit") { Width = 900 });
            var layout = new ColumnLayout(columns, 0, 250, 96);

            Assert.Equal(1, layout.VisibleColumnCount);
            Assert.Equal(0, layout.ColumnAt(200));
        }

        [Fact]
        public void An_offset_beyond_the_content_shows_nothing_rather_than_throwing()
        {
            var layout = new ColumnLayout(ThreeColumns(), 99999, 250, 96);

            Assert.Equal(0, layout.VisibleColumnCount);
        }

        [Fact]
        public void A_null_collection_is_a_programming_error()
        {
            Assert.Throws<ArgumentNullException>(() => new ColumnLayout(null, 0, 250, 96));
        }

        [Fact]
        public void A_nonpositive_dpi_is_a_programming_error()
        {
            // DpiScale wuerde ohnehin werfen; hier soll der Fehler an der Stelle
            // auftauchen, an der er entstanden ist.
            Assert.Throws<ArgumentOutOfRangeException>(() => new ColumnLayout(ThreeColumns(), 0, 250, 0));
        }
    }
}
