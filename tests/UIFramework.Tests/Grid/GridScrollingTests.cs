using System.Drawing;
using UIFramework.Grid;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Grid
{
    [Collection(SkinManagerCollection.Name)]
    public class GridScrollingTests
    {
        private static GridControl Grid(int rowCount, int columnWidth = 100)
        {
            var grid = new GridControl();
            grid.Columns.Add(new GridColumn("A", "A") { Width = columnWidth });
            grid.Columns.Add(new GridColumn("B", "B") { Width = columnWidth });
            grid.DataSource = new CountingDataSource(rowCount);
            grid.Size = new Size(400, 300);
            return grid;
        }

        [Fact]
        public void With_more_rows_than_fit_the_vertical_bar_appears()
        {
            using (var grid = Grid(1000))
            {
                Assert.True(grid.VerticalScrollBar.Visible);
            }
        }

        [Fact]
        public void With_everything_fitting_the_vertical_bar_stays_away()
        {
            using (var grid = Grid(2))
            {
                Assert.False(grid.VerticalScrollBar.Visible);
            }
        }

        [Fact]
        public void With_narrow_columns_the_horizontal_bar_stays_away()
        {
            using (var grid = Grid(1000, columnWidth: 50))
            {
                Assert.False(grid.HorizontalScrollBar.Visible);
            }
        }

        [Fact]
        public void With_wide_columns_the_horizontal_bar_appears()
        {
            using (var grid = Grid(1000, columnWidth: 500))
            {
                Assert.True(grid.HorizontalScrollBar.Visible);
            }
        }

        [Fact]
        public void Moving_the_bar_moves_the_grid()
        {
            using (var grid = Grid(1000))
            {
                grid.VerticalScrollBar.Value = 150;

                Assert.Equal(150, grid.VerticalOffset);
            }
        }

        [Fact]
        public void Moving_the_grid_moves_the_bar()
        {
            using (var grid = Grid(1000))
            {
                grid.VerticalOffset = 210;

                Assert.Equal(210, grid.VerticalScrollBar.Value);
            }
        }

        [Fact]
        public void The_two_do_not_chase_each_other_forever()
        {
            // Leiste meldet Scroll -> Grid setzt Versatz -> Grid gleicht Leiste ab
            // -> Leiste meldet Scroll. Ohne Bremse haengt der Prozess hier.
            // Dass dieser Test ueberhaupt zurueckkehrt, IST die Zusicherung.
            using (var grid = Grid(1000))
            {
                grid.VerticalScrollBar.Value = 90;
                grid.VerticalOffset = 120;
                grid.VerticalScrollBar.Value = 60;

                Assert.Equal(60, grid.VerticalOffset);
            }
        }

        [Fact]
        public void The_bar_range_follows_the_content()
        {
            using (var grid = Grid(1000))
            {
                int expected = grid.RowHeight * 1000;

                Assert.Equal(expected, grid.VerticalScrollBar.Maximum);
            }
        }

        [Fact]
        public void A_shrinking_source_shrinks_the_bar_and_pulls_the_value_back()
        {
            using (var grid = Grid(1000))
            {
                grid.VerticalOffset = grid.RowHeight * 900;

                grid.DataSource = new CountingDataSource(3);

                Assert.Equal(0, grid.VerticalScrollBar.Value);
                Assert.False(grid.VerticalScrollBar.Visible);
            }
        }

        [Fact]
        public void The_wheel_over_the_grid_scrolls_it()
        {
            // Der Zeiger steht ueber dem Grid, nicht ueber der Leiste — deren
            // OnMouseWheel erreicht das Rad also nie. Das Grid muss es weiterreichen.
            using (var grid = Grid(1000))
            {
                int before = grid.VerticalOffset;

                grid.PerformWheel(-120);

                Assert.True(grid.VerticalOffset > before);
            }
        }
    }
}
