using System.Drawing;
using System.Windows.Forms;
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

        [Fact]
        public void Columns_reaching_the_right_edge_with_a_vertical_bar_get_a_horizontal_bar()
        {
            // Der 2a-Befund: 2x200 logisch = 400 physisch füllen die volle Breite.
            // Alt: kein waagerechtes Scrollen möglich, die letzten ~12px lagen
            // dauerhaft unter der senkrechten Leiste. Neu: Die Reservierung macht
            // das Sichtfenster schmaler, also erscheint die waagerechte Leiste.
            using (var grid = Grid(1000, columnWidth: 200))
            {
                Assert.True(grid.VerticalScrollBar.Visible);
                Assert.True(grid.HorizontalScrollBar.Visible);
            }
        }

        [Fact]
        public void At_max_offset_the_last_column_edge_lies_inside_the_viewport()
        {
            using (var grid = Grid(1000, columnWidth: 200))
            {
                grid.HorizontalOffset = int.MaxValue;   // klemmt auf MaxScrollOffset

                var columns = grid.CurrentColumnLayout;
                int last = grid.Columns.Count - 1;
                int rightEdge = columns.ColumnLeft(last) + columns.ColumnWidth(last);

                Assert.Equal(grid.CurrentReservation.ViewportWidth, rightEdge);
            }
        }

        [Fact]
        public void The_two_bars_do_not_overlap_in_the_corner()
        {
            using (var grid = Grid(1000, columnWidth: 200))
            {
                Assert.True(grid.VerticalScrollBar.Visible);
                Assert.True(grid.HorizontalScrollBar.Visible);

                // Rectangle.IsEmpty prüft NICHT "keine Fläche", sondern
                // Gleichheit mit Rectangle.Empty (X=0,Y=0 eingeschlossen) — ein
                // dokumentiertes .NET-Kuriosum. Die Leisten berühren sich exakt
                // in der Ecke (Schnittfläche 0×0, aber nicht bei X=0,Y=0), darum
                // IntersectsWith statt Intersect(...).IsEmpty.
                Assert.False(grid.VerticalScrollBar.Bounds.IntersectsWith(grid.HorizontalScrollBar.Bounds));
            }
        }

        [Fact]
        public void End_scrolls_the_last_row_fully_above_the_horizontal_bar()
        {
            using (var grid = Grid(1000, columnWidth: 200))
            {
                grid.PerformKey(Keys.End);

                var rows = grid.CurrentRowViewport;
                int bottomInView = rows.RowTop(999) + grid.RowHeight;

                // Die Unterkante der letzten Zeile schließt mit der nutzbaren Höhe ab —
                // läge sie tiefer, verschwände sie unter der waagerechten Leiste.
                Assert.Equal(grid.CurrentReservation.ViewportHeight, bottomInView);
            }
        }

        [Fact]
        public void Form_scaling_leaves_the_bars_where_the_grid_put_them()
        {
            // Die DPI-Autoskalierung einer Form (AutoScaleMode.Dpi) skaliert die
            // Bounds ALLER Kinder — auch die der Leisten, die SyncScrollBars
            // bereits in physischen Pixeln gesetzt hat. Diese zweite Skalierung
            // schob die senkrechte Leiste bei 125 % aus dem Client hinaus
            // (x=1169 bei 950 Breite): am echten Fenster war sie unsichtbar,
            // bis der erste Resize sie zurückholte. Scale() ist derselbe
            // Codepfad wie die Autoskalierung, aber DPI-unabhängig aufrufbar —
            // so sieht der 96-dpi-Testlauf den 120-dpi-Fehler.
            using (var grid = Grid(1000, columnWidth: 500))
            {
                grid.Scale(new SizeF(1.25f, 1.25f));

                Assert.Equal(grid.ClientSize.Width, grid.VerticalScrollBar.Bounds.Right);
                Assert.Equal(grid.ClientSize.Height, grid.HorizontalScrollBar.Bounds.Bottom);
            }
        }
    }
}
