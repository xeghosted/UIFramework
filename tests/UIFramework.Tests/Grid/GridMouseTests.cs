using System.Drawing;
using System.Windows.Forms;
using UIFramework.Grid;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Grid
{
    [Collection(SkinManagerCollection.Name)]
    public class GridMouseTests
    {
        private static GridControl Grid()
        {
            var grid = new GridControl();
            grid.Columns.Add(new GridColumn("A", "A") { Width = 100 });
            grid.DataSource = new CountingDataSource(1000);
            grid.Size = new Size(400, 300);
            return grid;
        }

        /// <summary>Die Mitte der Zeile mit diesem Index, in Control-Koordinaten.</summary>
        private static Point RowCentre(GridControl grid, int rowIndex)
        {
            int y = grid.HeaderHeight + grid.CurrentRowViewport.RowTop(rowIndex) + grid.RowHeight / 2;
            return new Point(50, y);
        }

        [Fact]
        public void A_click_selects_the_row_under_the_pointer()
        {
            using (var grid = Grid())
            {
                grid.PerformClick(RowCentre(grid, 2), Keys.None);

                Assert.True(grid.Selection.IsSelected(2));
                Assert.Equal(1, grid.Selection.Count);
            }
        }

        [Fact]
        public void Ctrl_click_adds_a_second_row()
        {
            using (var grid = Grid())
            {
                grid.PerformClick(RowCentre(grid, 1), Keys.None);
                grid.PerformClick(RowCentre(grid, 4), Keys.Control);

                Assert.Equal(2, grid.Selection.Count);
                Assert.True(grid.Selection.IsSelected(1));
                Assert.True(grid.Selection.IsSelected(4));
            }
        }

        [Fact]
        public void Shift_click_spans_from_the_first_click()
        {
            using (var grid = Grid())
            {
                grid.PerformClick(RowCentre(grid, 1), Keys.None);
                grid.PerformClick(RowCentre(grid, 4), Keys.Shift);

                Assert.Equal(4, grid.Selection.Count);
            }
        }

        [Fact]
        public void A_click_below_the_last_row_clears_the_selection()
        {
            // Nicht "waehlt die letzte Zeile" — der Anwender hat bewusst
            // daneben geklickt.
            var grid = new GridControl();
            grid.Columns.Add(new GridColumn("A", "A") { Width = 100 });
            grid.DataSource = new CountingDataSource(2);
            grid.Size = new Size(400, 300);

            using (grid)
            {
                grid.PerformClick(RowCentre(grid, 0), Keys.None);
                grid.PerformClick(new Point(50, 280), Keys.None);

                Assert.Equal(0, grid.Selection.Count);
            }
        }

        [Fact]
        public void A_click_in_the_header_does_not_touch_the_selection()
        {
            using (var grid = Grid())
            {
                grid.PerformClick(RowCentre(grid, 2), Keys.None);
                grid.PerformClick(new Point(50, 2), Keys.None);

                Assert.True(grid.Selection.IsSelected(2));
            }
        }

        [Fact]
        public void A_click_right_of_the_last_column_still_selects_the_row()
        {
            // Die Auswahl gilt der ganzen Zeile.
            using (var grid = Grid())
            {
                int y = grid.HeaderHeight + grid.CurrentRowViewport.RowTop(3) + grid.RowHeight / 2;

                grid.PerformClick(new Point(350, y), Keys.None);

                Assert.True(grid.Selection.IsSelected(3));
            }
        }

        [Fact]
        public void A_click_selects_the_right_row_after_scrolling()
        {
            // Der Klassiker: Ohne den Versatz mitzurechnen waehlt jeder Klick
            // nach dem Scrollen die falsche Zeile.
            using (var grid = Grid())
            {
                grid.VerticalOffset = grid.RowHeight * 100;

                grid.PerformClick(RowCentre(grid, 100), Keys.None);

                Assert.True(grid.Selection.IsSelected(100));
            }
        }
    }
}
