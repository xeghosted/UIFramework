using System.Drawing;
using UIFramework.Grid;
using UIFramework.Grid.Layout;
using Xunit;

namespace UIFramework.Tests.Grid
{
    public class GridHitTestTests
    {
        // Aufbau: Kopf 20 hoch. Drei Spalten a 100 (96 dpi). Zeilen 30 hoch,
        // Sichtfenster 100 -> Zeilen 0..3 sichtbar. Greifbreite 4.
        private const int HeaderHeight = 20;
        private const int Grip = 4;

        private static GridColumnCollection Columns()
        {
            var columns = new GridColumnCollection();
            columns.Add(new GridColumn("A", "A") { Width = 100 });
            columns.Add(new GridColumn("B", "B") { Width = 100 });
            columns.Add(new GridColumn("C", "C") { Width = 100 });
            return columns;
        }

        private static GridHit At(int x, int y, int rowCount = 1000)
        {
            var rows = new RowViewport(30, 100, 0, rowCount);
            var layout = new ColumnLayout(Columns(), 0, 400, 96);
            return GridHitTest.At(new Point(x, y), HeaderHeight, rows, layout, Grip);
        }

        [Fact]
        public void A_point_in_the_header_is_a_header_hit()
        {
            var hit = At(50, 10);

            Assert.Equal(GridRegion.Header, hit.Region);
            Assert.Equal(0, hit.ColumnIndex);
            Assert.Equal(-1, hit.RowIndex);
        }

        [Fact]
        public void The_header_knows_which_column_was_hit()
        {
            var hit = At(150, 10);

            Assert.Equal(1, hit.ColumnIndex);
        }

        [Fact]
        public void A_point_on_a_header_divider_is_a_divider_hit()
        {
            // Trennlinie zwischen A und B liegt bei x=100, Greifbreite 4.
            var hit = At(100, 10);

            Assert.Equal(GridRegion.HeaderDivider, hit.Region);
        }

        [Fact]
        public void The_divider_reports_the_column_to_its_left()
        {
            // Beim Ziehen aendert sich die Breite der LINKEN Spalte. Meldete der
            // Treffer die rechte, zoege der Anwender die falsche.
            var hit = At(100, 10);

            Assert.Equal(0, hit.ColumnIndex);
        }

        [Fact]
        public void The_grip_reaches_to_both_sides_of_the_divider()
        {
            Assert.Equal(GridRegion.HeaderDivider, At(97, 10).Region);
            Assert.Equal(GridRegion.HeaderDivider, At(103, 10).Region);
        }

        [Fact]
        public void Just_outside_the_grip_is_an_ordinary_header_hit()
        {
            Assert.Equal(GridRegion.Header, At(95, 10).Region);
            Assert.Equal(GridRegion.Header, At(105, 10).Region);
        }

        [Fact]
        public void The_right_edge_of_the_last_column_is_a_divider_too()
        {
            // Die letzte Spalte muss sich ebenso ziehen lassen wie die anderen.
            var hit = At(300, 10);

            Assert.Equal(GridRegion.HeaderDivider, hit.Region);
            Assert.Equal(2, hit.ColumnIndex);
        }

        [Fact]
        public void The_left_edge_of_the_first_column_is_not_a_divider()
        {
            // Links von A ist nichts, was man breiter ziehen koennte.
            var hit = At(0, 10);

            Assert.Equal(GridRegion.Header, hit.Region);
        }

        [Fact]
        public void A_point_below_the_header_is_a_cell()
        {
            var hit = At(50, 25);

            Assert.Equal(GridRegion.Cell, hit.Region);
            Assert.Equal(0, hit.RowIndex);
            Assert.Equal(0, hit.ColumnIndex);
        }

        [Fact]
        public void The_cell_knows_its_row_and_column()
        {
            // y=25 -> 5 unterhalb des Kopfs -> Zeile 0. y=55 -> 35 -> Zeile 1.
            var hit = At(150, 55);

            Assert.Equal(1, hit.RowIndex);
            Assert.Equal(1, hit.ColumnIndex);
        }

        [Fact]
        public void A_divider_only_counts_in_the_header_not_over_the_cells()
        {
            // Sonst zoege ein Klick mitten in die Daten eine Spaltenbreite.
            var hit = At(100, 55);

            Assert.Equal(GridRegion.Cell, hit.Region);
        }

        [Fact]
        public void A_point_below_the_last_row_is_empty_space()
        {
            // Drei Zeilen: y = 20 + 90 = 110 liegt darunter.
            var hit = At(50, 115, rowCount: 3);

            Assert.Equal(GridRegion.EmptyBelowRows, hit.Region);
            Assert.Equal(-1, hit.RowIndex);
        }

        [Fact]
        public void A_point_right_of_the_last_column_hits_no_column()
        {
            var hit = At(350, 55);

            Assert.Equal(-1, hit.ColumnIndex);
        }

        [Fact]
        public void A_point_right_of_the_last_column_but_on_a_row_is_still_that_row()
        {
            // Die Auswahl gilt der ganzen Zeile — ein Klick rechts der letzten
            // Spalte soll sie trotzdem waehlen.
            var hit = At(350, 55);

            Assert.Equal(GridRegion.Cell, hit.Region);
            Assert.Equal(1, hit.RowIndex);
        }

        [Fact]
        public void A_negative_point_hits_nothing()
        {
            Assert.Equal(GridRegion.Nothing, At(-5, 10).Region);
            Assert.Equal(GridRegion.Nothing, At(50, -5).Region);
        }

        [Fact]
        public void An_empty_source_makes_everything_below_the_header_empty_space()
        {
            var hit = At(50, 55, rowCount: 0);

            Assert.Equal(GridRegion.EmptyBelowRows, hit.Region);
        }
    }
}
