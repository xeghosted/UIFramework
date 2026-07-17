using System.Drawing;
using UIFramework.Grid;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Grid
{
    [Collection(SkinManagerCollection.Name)]
    public class GridHeaderClickTests
    {
        private static GridControl Grid()
        {
            var grid = new GridControl();
            grid.Columns.Add(new GridColumn("A", "A") { Width = 100 });
            grid.Columns.Add(new GridColumn("B", "B") { Width = 100 });
            grid.DataSource = new CountingDataSource(1000);
            grid.Size = new Size(400, 300);
            return grid;
        }

        [Fact]
        public void Clicking_a_header_without_moving_fires_header_click()
        {
            using (var grid = Grid())
            {
                int? clicked = null;
                grid.HeaderClick += (s, columnIndex) => clicked = columnIndex;

                grid.BeginReorder(new Point(50, 2));
                grid.EndReorder();

                Assert.Equal(0, clicked);
            }
        }

        [Fact]
        public void Clicking_a_cell_does_not_fire_it()
        {
            using (var grid = Grid())
            {
                int fired = 0;
                grid.HeaderClick += (s, columnIndex) => fired++;

                grid.PerformClick(new Point(50, 100), System.Windows.Forms.Keys.None);

                Assert.Equal(0, fired);
            }
        }

        [Fact]
        public void A_real_drag_that_moves_the_column_does_not_fire_it()
        {
            using (var grid = Grid())
            {
                int fired = 0;
                grid.HeaderClick += (s, columnIndex) => fired++;

                // Nur zwei Spalten von je 100 -- Punkt 250 liegt hinter beiden
                // (ColumnAt liefert dort -1, ununterscheidbar von "kein Ziel").
                // 150 liegt sauber auf Spalte B und bewegt tatsaechlich etwas.
                grid.BeginReorder(new Point(50, 2));
                grid.DragReorder(new Point(150, 2));
                grid.EndReorder();

                Assert.Equal(0, fired);
            }
        }

        [Fact]
        public void Releasing_over_the_grabbed_column_itself_fires_it()
        {
            // Kein Zug fand statt -- das zaehlt als Klick, nicht als
            // fehlgeschlagenes Umordnen.
            using (var grid = Grid())
            {
                int? clicked = null;
                grid.HeaderClick += (s, columnIndex) => clicked = columnIndex;

                grid.BeginReorder(new Point(50, 2));
                grid.DragReorder(new Point(60, 2));
                grid.EndReorder();

                Assert.Equal(0, clicked);
            }
        }

        [Fact]
        public void Grabbing_a_divider_to_resize_does_not_fire_it()
        {
            using (var grid = Grid())
            {
                int fired = 0;
                grid.HeaderClick += (s, columnIndex) => fired++;

                grid.BeginResize(new Point(100, 2));
                grid.DragResize(new Point(160, 2));
                grid.EndResize();

                Assert.Equal(0, fired);
            }
        }

        [Fact]
        public void Ending_a_reorder_that_never_started_fires_nothing()
        {
            using (var grid = Grid())
            {
                int fired = 0;
                grid.HeaderClick += (s, columnIndex) => fired++;

                grid.EndReorder();

                Assert.Equal(0, fired);
            }
        }

        [Fact]
        public void Assigning_a_new_data_source_clears_the_selection()
        {
            using (var grid = Grid())
            {
                grid.Selection.Select(5);

                // Dieselbe Zeilenzahl -- TrimTo(RowCount) liesse Zeile 5
                // faelschlich ausgewaehlt stehen, obwohl eine sortierte oder
                // gefilterte Ansicht Zeile 5 etwas ganz anderes bedeuten kann.
                grid.DataSource = new CountingDataSource(1000);

                Assert.Equal(0, grid.Selection.Count);
                Assert.Equal(-1, grid.Selection.CurrentRow);
            }
        }

        [Fact]
        public void Assigning_the_same_data_source_instance_changes_nothing()
        {
            using (var grid = Grid())
            {
                grid.Selection.Select(5);
                var same = grid.DataSource;

                grid.DataSource = same;

                Assert.True(grid.Selection.IsSelected(5));
            }
        }
    }
}
