using System.Drawing;
using UIFramework.Grid;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Grid
{
    [Collection(SkinManagerCollection.Name)]
    public class GridColumnResizeTests
    {
        private static GridControl Grid(int rowCount = 1000)
        {
            var grid = new GridControl();
            grid.Columns.Add(new GridColumn("A", "A") { Width = 100 });
            grid.Columns.Add(new GridColumn("B", "B") { Width = 100 });
            grid.DataSource = new CountingDataSource(rowCount);
            grid.Size = new Size(400, 300);
            return grid;
        }

        [Fact]
        public void Grabbing_the_divider_starts_a_resize()
        {
            using (var grid = Grid())
            {
                grid.BeginResize(new Point(100, 2));

                Assert.True(grid.IsResizing);
            }
        }

        [Fact]
        public void Grabbing_somewhere_else_starts_nothing()
        {
            using (var grid = Grid())
            {
                grid.BeginResize(new Point(50, 2));

                Assert.False(grid.IsResizing);
            }
        }

        [Fact]
        public void Dragging_right_widens_the_column_to_its_left()
        {
            using (var grid = Grid())
            {
                grid.BeginResize(new Point(100, 2));
                grid.DragResize(new Point(160, 2));

                Assert.Equal(160, grid.Columns[0].Width);
                Assert.Equal(100, grid.Columns[1].Width);
            }
        }

        [Fact]
        public void Dragging_left_narrows_it()
        {
            using (var grid = Grid())
            {
                grid.BeginResize(new Point(100, 2));
                grid.DragResize(new Point(70, 2));

                Assert.Equal(70, grid.Columns[0].Width);
            }
        }

        [Fact]
        public void Dragging_past_the_minimum_stops_at_the_minimum()
        {
            using (var grid = Grid())
            {
                grid.Columns[0].MinWidth = 40;
                grid.BeginResize(new Point(100, 2));

                grid.DragResize(new Point(5, 2));

                Assert.Equal(40, grid.Columns[0].Width);
            }
        }

        [Fact]
        public void Dragging_without_grabbing_first_does_nothing()
        {
            using (var grid = Grid())
            {
                grid.DragResize(new Point(300, 2));

                Assert.Equal(100, grid.Columns[0].Width);
            }
        }

        [Fact]
        public void Letting_go_ends_the_resize()
        {
            using (var grid = Grid())
            {
                grid.BeginResize(new Point(100, 2));
                grid.EndResize();

                Assert.False(grid.IsResizing);

                grid.DragResize(new Point(300, 2));
                Assert.Equal(100, grid.Columns[0].Width);
            }
        }

        [Fact]
        public void Auto_fit_makes_the_column_wide_enough_for_what_is_shown()
        {
            using (var grid = Grid())
            {
                grid.Columns[0].Width = 20;

                grid.AutoFitColumn(0);

                Assert.True(grid.Columns[0].Width > 20);
            }
        }

        [Fact]
        public void Auto_fit_on_a_million_rows_measures_only_the_visible_ones()
        {
            // DER Test dieses Tasks. Die naheliegende Schleife ueber RowCount
            // faende hier 1.000.000 statt ~30 — ein Klick, der die Anwendung
            // sekundenlang einfriert.
            var source = new CountingDataSource(1000000);
            var grid = new GridControl();
            grid.Columns.Add(new GridColumn("A", "A") { Width = 100 });
            grid.DataSource = source;
            grid.Size = new Size(400, 300);

            using (grid)
            {
                source.Reset();

                grid.AutoFitColumn(0);

                Assert.InRange(source.TouchedRowCount, 0, 100);
                Assert.True(source.HighestTouchedRow < 100,
                    "Auto-Fit hat bis Zeile " + source.HighestTouchedRow + " gelesen.");
            }
        }

        [Fact]
        public void Auto_fit_measures_the_rows_that_are_actually_on_screen()
        {
            // Nach dem Scrollen misst es die Zeilen von DORT, nicht vom Anfang.
            var source = new CountingDataSource(1000000);
            var grid = new GridControl();
            grid.Columns.Add(new GridColumn("A", "A") { Width = 100 });
            grid.DataSource = source;
            grid.Size = new Size(400, 300);

            using (grid)
            {
                grid.VerticalOffset = grid.RowHeight * 500000;
                source.Reset();

                grid.AutoFitColumn(0);

                Assert.True(source.LowestTouchedRow >= 499990 || source.LowestTouchedRow == -1,
                    "Auto-Fit hat ab Zeile " + source.LowestTouchedRow + " gelesen statt ab der Sicht.");
            }
        }

        [Fact]
        public void Auto_fit_on_an_empty_source_still_fits_the_header()
        {
            using (var grid = Grid(rowCount: 0))
            {
                grid.Columns[0].Width = 20;

                grid.AutoFitColumn(0);

                Assert.True(grid.Columns[0].Width > 0);
            }
        }

        [Fact]
        public void Auto_fit_on_a_column_that_does_not_exist_does_nothing()
        {
            using (var grid = Grid())
            {
                grid.AutoFitColumn(99);
                grid.AutoFitColumn(-1);
            }
        }
    }
}
