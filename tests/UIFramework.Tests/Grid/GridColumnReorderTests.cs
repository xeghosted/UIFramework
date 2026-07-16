using System.Drawing;
using System.Linq;
using UIFramework.Grid;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Grid
{
    [Collection(SkinManagerCollection.Name)]
    public class GridColumnReorderTests
    {
        private static GridControl Grid()
        {
            var grid = new GridControl();
            grid.Columns.Add(new GridColumn("A", "A") { Width = 100 });
            grid.Columns.Add(new GridColumn("B", "B") { Width = 100 });
            grid.Columns.Add(new GridColumn("C", "C") { Width = 100 });
            grid.DataSource = new CountingDataSource(100);
            grid.Size = new Size(400, 300);
            return grid;
        }

        private static string[] Keys(GridControl grid)
        {
            return grid.Columns.Select(c => c.Key).ToArray();
        }

        [Fact]
        public void Grabbing_a_header_starts_a_reorder()
        {
            using (var grid = Grid())
            {
                grid.BeginReorder(new Point(50, 2));

                Assert.Equal(0, grid.ReorderingColumn);
            }
        }

        [Fact]
        public void Grabbing_a_divider_starts_no_reorder()
        {
            // Dort wird gezogen, nicht umgeordnet. Beides zugleich waere ein
            // Griff, der zwei Dinge tut.
            using (var grid = Grid())
            {
                grid.BeginReorder(new Point(100, 2));

                Assert.Equal(-1, grid.ReorderingColumn);
            }
        }

        [Fact]
        public void Grabbing_a_cell_starts_no_reorder()
        {
            using (var grid = Grid())
            {
                grid.BeginReorder(new Point(50, 100));

                Assert.Equal(-1, grid.ReorderingColumn);
            }
        }

        [Fact]
        public void Dragging_over_another_header_marks_it_as_the_target()
        {
            using (var grid = Grid())
            {
                grid.BeginReorder(new Point(50, 2));

                grid.DragReorder(new Point(250, 2));

                Assert.Equal(2, grid.ReorderTargetIndex);
            }
        }

        [Fact]
        public void Letting_go_moves_the_column_there()
        {
            using (var grid = Grid())
            {
                grid.BeginReorder(new Point(50, 2));
                grid.DragReorder(new Point(250, 2));

                grid.EndReorder();

                Assert.Equal(new[] { "B", "C", "A" }, Keys(grid));
            }
        }

        [Fact]
        public void Dragging_backwards_works_too()
        {
            using (var grid = Grid())
            {
                grid.BeginReorder(new Point(250, 2));
                grid.DragReorder(new Point(50, 2));
                grid.EndReorder();

                Assert.Equal(new[] { "C", "A", "B" }, Keys(grid));
            }
        }

        [Fact]
        public void Letting_go_over_the_column_itself_changes_nothing()
        {
            using (var grid = Grid())
            {
                grid.BeginReorder(new Point(50, 2));
                grid.DragReorder(new Point(60, 2));
                grid.EndReorder();

                Assert.Equal(new[] { "A", "B", "C" }, Keys(grid));
            }
        }

        [Fact]
        public void Letting_go_outside_the_header_drops_the_move()
        {
            // Herausziehen und loslassen heisst abbrechen, nicht irgendwohin
            // fallen lassen.
            using (var grid = Grid())
            {
                grid.BeginReorder(new Point(50, 2));
                grid.DragReorder(new Point(250, 2));
                grid.DragReorder(new Point(250, 200));

                grid.EndReorder();

                Assert.Equal(new[] { "A", "B", "C" }, Keys(grid));
            }
        }

        [Fact]
        public void Letting_go_without_grabbing_first_does_nothing()
        {
            using (var grid = Grid())
            {
                grid.EndReorder();

                Assert.Equal(new[] { "A", "B", "C" }, Keys(grid));
            }
        }

        [Fact]
        public void Ending_a_reorder_forgets_the_grab()
        {
            using (var grid = Grid())
            {
                grid.BeginReorder(new Point(50, 2));
                grid.DragReorder(new Point(250, 2));
                grid.EndReorder();

                Assert.Equal(-1, grid.ReorderingColumn);
                Assert.Equal(-1, grid.ReorderTargetIndex);
            }
        }

        [Fact]
        public void Reordering_does_not_disturb_the_selection()
        {
            using (var grid = Grid())
            {
                grid.Selection.Select(5);

                grid.BeginReorder(new Point(50, 2));
                grid.DragReorder(new Point(250, 2));
                grid.EndReorder();

                Assert.True(grid.Selection.IsSelected(5));
            }
        }
    }
}
