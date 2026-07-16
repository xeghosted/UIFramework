using System.Drawing;

namespace UIFramework.Grid.Layout
{
    /// <summary>Was liegt an einer Stelle des Grids?</summary>
    public enum GridRegion
    {
        /// <summary>Außerhalb — links oder oberhalb des Inhalts.</summary>
        Nothing,

        /// <summary>Eine Kopfzelle.</summary>
        Header,

        /// <summary>Die Greifzone einer Spaltentrennlinie im Kopf.</summary>
        HeaderDivider,

        /// <summary>Eine Datenzeile (auch rechts der letzten Spalte — die Auswahl gilt der ganzen Zeile).</summary>
        Cell,

        /// <summary>Unterhalb der letzten Zeile.</summary>
        EmptyBelowRows
    }

    /// <summary>Das Ergebnis einer Treffererkennung.</summary>
    public struct GridHit
    {
        public GridHit(GridRegion region, int rowIndex, int columnIndex)
        {
            Region = region;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
        }

        public GridRegion Region { get; }

        /// <summary>Die getroffene Zeile, oder -1.</summary>
        public int RowIndex { get; }

        /// <summary>
        /// Die getroffene Spalte, oder -1. Bei HeaderDivider ist es die Spalte
        /// LINKS der Linie — die, deren Breite sich beim Ziehen ändert.
        /// </summary>
        public int ColumnIndex { get; }
    }

    /// <summary>
    /// Punkt → Bereich. Ohne Graphics, ohne Control: dieselbe Rechnung, die das
    /// Zeichnen benutzt, nur rückwärts — und damit kopflos prüfbar.
    /// </summary>
    public static class GridHitTest
    {
        public static GridHit At(Point point, int headerHeight, RowViewport rows,
                                 ColumnLayout columns, int dividerGrip)
        {
            if (point.X < 0 || point.Y < 0)
                return new GridHit(GridRegion.Nothing, -1, -1);

            if (point.Y < headerHeight)
                return InHeader(point, columns, dividerGrip);

            int row = rows.RowAt(point.Y - headerHeight);
            if (row < 0)
                return new GridHit(GridRegion.EmptyBelowRows, -1, columns.ColumnAt(point.X));

            // Rechts der letzten Spalte bleibt es die Zeile: Die Auswahl gilt der
            // ganzen Zeile, ein Klick dort soll sie treffen. ColumnAt liefert -1.
            return new GridHit(GridRegion.Cell, row, columns.ColumnAt(point.X));
        }

        private static GridHit InHeader(Point point, ColumnLayout columns, int dividerGrip)
        {
            // Trennlinien zuerst: Sie liegen auf den Kanten und müssen den
            // Kopftreffer schlagen, sonst wäre keine je greifbar.
            for (int i = 0; i < columns.VisibleColumnCount; i++)
            {
                int index = columns.FirstVisibleColumn + i;
                int right = columns.ColumnLeft(index) + columns.ColumnWidth(index);

                if (point.X >= right - dividerGrip && point.X <= right + dividerGrip)
                    return new GridHit(GridRegion.HeaderDivider, -1, index);
            }

            return new GridHit(GridRegion.Header, -1, columns.ColumnAt(point.X));
        }
    }
}
