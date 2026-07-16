using System.Drawing;
using System.Windows.Forms;
using UIFramework.Grid;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Grid
{
    [Collection(SkinManagerCollection.Name)]
    public class GridKeyboardTests
    {
        private static GridControl Grid(int rowCount = 1000)
        {
            var grid = new GridControl();
            grid.Columns.Add(new GridColumn("A", "A") { Width = 100 });
            grid.DataSource = new CountingDataSource(rowCount);
            grid.Size = new Size(400, 300);
            return grid;
        }

        [Fact]
        public void Down_moves_the_current_row_one_further()
        {
            using (var grid = Grid())
            {
                grid.Selection.Select(5);

                grid.PerformKey(Keys.Down);

                Assert.Equal(6, grid.Selection.CurrentRow);
                Assert.True(grid.Selection.IsSelected(6));
                Assert.False(grid.Selection.IsSelected(5));
            }
        }

        [Fact]
        public void Up_moves_it_back()
        {
            using (var grid = Grid())
            {
                grid.Selection.Select(5);

                grid.PerformKey(Keys.Up);

                Assert.Equal(4, grid.Selection.CurrentRow);
            }
        }

        [Fact]
        public void Down_on_a_fresh_grid_starts_at_the_first_row()
        {
            using (var grid = Grid())
            {
                grid.PerformKey(Keys.Down);

                Assert.Equal(0, grid.Selection.CurrentRow);
            }
        }

        [Fact]
        public void Up_at_the_very_top_stays_put()
        {
            using (var grid = Grid())
            {
                grid.Selection.Select(0);

                grid.PerformKey(Keys.Up);

                Assert.Equal(0, grid.Selection.CurrentRow);
            }
        }

        [Fact]
        public void Down_at_the_very_bottom_stays_put()
        {
            using (var grid = Grid(rowCount: 10))
            {
                grid.Selection.Select(9);

                grid.PerformKey(Keys.Down);

                Assert.Equal(9, grid.Selection.CurrentRow);
            }
        }

        [Fact]
        public void Home_goes_to_the_first_row()
        {
            using (var grid = Grid())
            {
                grid.Selection.Select(500);

                grid.PerformKey(Keys.Home);

                Assert.Equal(0, grid.Selection.CurrentRow);
                Assert.Equal(0, grid.VerticalOffset);
            }
        }

        [Fact]
        public void End_goes_to_the_last_row_even_with_a_million()
        {
            using (var grid = Grid())
            {
                grid.PerformKey(Keys.End);

                Assert.Equal(999, grid.Selection.CurrentRow);
            }
        }

        [Fact]
        public void End_scrolls_so_the_last_row_is_really_visible()
        {
            using (var grid = Grid())
            {
                grid.PerformKey(Keys.End);

                var rows = grid.CurrentRowViewport;
                int last = rows.FirstVisibleRow + rows.VisibleRowCount - 1;

                Assert.True(last >= 999, "Die letzte Zeile liegt nicht im Sichtfenster.");
            }
        }

        [Fact]
        public void Page_down_jumps_about_a_screenful()
        {
            using (var grid = Grid())
            {
                grid.Selection.Select(0);
                int perPage = grid.CurrentRowViewport.VisibleRowCount;

                grid.PerformKey(Keys.PageDown);

                Assert.Equal(perPage - 1, grid.Selection.CurrentRow);
            }
        }

        [Fact]
        public void Page_up_jumps_back()
        {
            using (var grid = Grid())
            {
                grid.Selection.Select(100);
                int perPage = grid.CurrentRowViewport.VisibleRowCount;

                grid.PerformKey(Keys.PageUp);

                Assert.Equal(100 - (perPage - 1), grid.Selection.CurrentRow);
            }
        }

        [Fact]
        public void Page_down_near_the_end_lands_on_the_last_row_rather_than_past_it()
        {
            using (var grid = Grid(rowCount: 10))
            {
                grid.Selection.Select(8);

                grid.PerformKey(Keys.PageDown);

                Assert.Equal(9, grid.Selection.CurrentRow);
            }
        }

        [Fact]
        public void Walking_past_the_bottom_edge_drags_the_view_along()
        {
            // DER Test dieses Tasks: Ohne EnsureRowVisible wandert die Auswahl
            // aus dem Bild und der Anwender sieht seinen eigenen Cursor nicht mehr.
            using (var grid = Grid())
            {
                var rows = grid.CurrentRowViewport;
                int lastVisible = rows.FirstVisibleRow + rows.VisibleRowCount - 1;
                grid.Selection.Select(lastVisible);
                int before = grid.VerticalOffset;

                grid.PerformKey(Keys.Down);

                Assert.True(grid.VerticalOffset > before,
                    "Die Sicht ist nicht mitgewandert — die Auswahl steht ausserhalb.");
            }
        }

        [Fact]
        public void Walking_past_the_top_edge_drags_the_view_along_too()
        {
            using (var grid = Grid())
            {
                grid.VerticalOffset = grid.RowHeight * 100;
                grid.Selection.Select(grid.CurrentRowViewport.FirstVisibleRow);
                int before = grid.VerticalOffset;

                grid.PerformKey(Keys.Up);

                Assert.True(grid.VerticalOffset < before);
            }
        }

        [Fact]
        public void Shift_down_extends_the_selection_instead_of_moving_it()
        {
            using (var grid = Grid())
            {
                grid.Selection.Select(5);

                grid.PerformKey(Keys.Down | Keys.Shift);

                Assert.Equal(2, grid.Selection.Count);
                Assert.True(grid.Selection.IsSelected(5));
                Assert.True(grid.Selection.IsSelected(6));
            }
        }

        [Fact]
        public void An_empty_source_swallows_the_keys_without_throwing()
        {
            using (var grid = Grid(rowCount: 0))
            {
                grid.PerformKey(Keys.Down);
                grid.PerformKey(Keys.End);
                grid.PerformKey(Keys.PageDown);

                Assert.Equal(-1, grid.Selection.CurrentRow);
            }
        }
    }
}
