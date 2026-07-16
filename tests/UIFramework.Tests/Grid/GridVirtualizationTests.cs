using System.Drawing;
using UIFramework.Grid;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Grid
{
    [Collection(SkinManagerCollection.Name)]
    public class GridVirtualizationTests
    {
        private const int MillionRows = 1000000;

        private static GridControl GridWithThreeColumns(IGridDataSource source)
        {
            var grid = new GridControl();
            grid.Columns.Add(new GridColumn("A", "A") { Width = 100 });
            grid.Columns.Add(new GridColumn("B", "B") { Width = 100 });
            grid.Columns.Add(new GridColumn("C", "C") { Width = 100 });
            grid.DataSource = source;
            grid.Size = new Size(400, 300);
            return grid;
        }

        /// <summary>Zeichnet ohne Fenster — genau das, was OnPaint täte.</summary>
        private static void Paint(GridControl grid)
        {
            using (var bitmap = new Bitmap(grid.Width, grid.Height))
            using (var g = Graphics.FromImage(bitmap))
            {
                grid.DrawTo(g);
            }
        }

        [Fact]
        public void Painting_a_million_rows_touches_only_the_visible_ones()
        {
            // DAS ist der Test, um den herum dieses Teilprojekt entworfen ist.
            // Bricht die Virtualisierung, steht hier 1.000.000 statt ~30.
            var source = new CountingDataSource(MillionRows);
            using (var grid = GridWithThreeColumns(source))
            {
                Paint(grid);

                // 300px hoch, Kopf ab, Zeilen um die 20-30px -> weit unter 100.
                Assert.InRange(source.TouchedRowCount, 1, 100);
            }
        }

        [Fact]
        public void Painting_a_million_rows_does_not_reach_the_far_end_of_the_source()
        {
            // Fängt eine Schleife über RowCount, die zufällig frühzeitig abbricht.
            var source = new CountingDataSource(MillionRows);
            using (var grid = GridWithThreeColumns(source))
            {
                Paint(grid);

                Assert.True(source.HighestTouchedRow < 100,
                    "Es wurde bis Zeile " + source.HighestTouchedRow +
                    " gelesen — das Grid liest weit mehr als es zeigt.");
            }
        }

        [Fact]
        public void Painting_touches_each_visible_cell_once_and_not_more()
        {
            // Aufrufe pro Zeile == Spaltenzahl. Mehr hiesse doppeltes Lesen
            // (z. B. einmal fuers Messen, einmal fuers Zeichnen) — bei 60 Bildern
            // je Sekunde teuer und unnoetig.
            var source = new CountingDataSource(MillionRows);
            using (var grid = GridWithThreeColumns(source))
            {
                Paint(grid);

                Assert.Equal(source.TouchedRowCount * 3, source.GetValueCalls);
            }
        }

        [Fact]
        public void Scrolling_to_the_middle_reads_the_middle_not_the_start()
        {
            var source = new CountingDataSource(MillionRows);
            using (var grid = GridWithThreeColumns(source))
            {
                grid.VerticalOffset = grid.RowHeight * 500000;
                source.Reset();

                Paint(grid);

                Assert.True(source.LowestTouchedRow >= 499990,
                    "Es wurde ab Zeile " + source.LowestTouchedRow + " gelesen.");
                Assert.InRange(source.TouchedRowCount, 1, 100);
            }
        }

        [Fact]
        public void Scrolling_horizontally_still_reads_every_visible_column()
        {
            // Waagerechtes Scrollen darf Spalten ueberspringen, aber die
            // sichtbaren muessen kommen.
            var source = new CountingDataSource(MillionRows);
            using (var grid = GridWithThreeColumns(source))
            {
                grid.HorizontalOffset = 150;
                source.Reset();

                Paint(grid);

                Assert.True(source.GetValueCalls > 0);
                Assert.InRange(source.TouchedRowCount, 1, 100);
            }
        }

        [Fact]
        public void An_empty_source_reads_nothing_and_does_not_throw()
        {
            var source = new CountingDataSource(0);
            using (var grid = GridWithThreeColumns(source))
            {
                Paint(grid);

                Assert.Equal(0, source.GetValueCalls);
            }
        }

        [Fact]
        public void No_data_source_at_all_paints_without_throwing()
        {
            // Der Zustand im Designer und direkt nach dem Konstruktor.
            using (var grid = new GridControl())
            {
                grid.Size = new Size(400, 300);
                grid.Columns.Add(new GridColumn("A", "A"));

                Paint(grid);
            }
        }

        [Fact]
        public void No_columns_at_all_paints_without_throwing()
        {
            using (var grid = new GridControl())
            {
                grid.Size = new Size(400, 300);
                grid.DataSource = new CountingDataSource(10);

                Paint(grid);
            }
        }

        [Fact]
        public void The_row_height_comes_from_the_skin_and_is_positive()
        {
            using (var grid = new GridControl())
            {
                Assert.True(grid.RowHeight > 0);
                Assert.True(grid.HeaderHeight > 0);
            }
        }

        [Fact]
        public void A_shrinking_source_pulls_the_offset_back_instead_of_showing_emptiness()
        {
            var source = new CountingDataSource(MillionRows);
            using (var grid = GridWithThreeColumns(source))
            {
                grid.VerticalOffset = grid.RowHeight * 900000;

                grid.DataSource = new CountingDataSource(5);

                Assert.Equal(0, grid.VerticalOffset);
            }
        }
    }
}
