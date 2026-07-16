using System;
using UIFramework.Core.Dpi;

namespace UIFramework.Grid.Layout
{
    /// <summary>
    /// Wo liegen die Spalten, und welche sind sichtbar?
    ///
    /// Die eine Stelle, an der aus der logischen Spaltenbreite (GridColumn.Width,
    /// 96-DPI-Basis) ein physisches Maß wird. Das Control rechnet weiterhin keine
    /// DPI-Arithmetik — es reicht seine DeviceDpi hierher.
    ///
    /// Summiert linear statt über einen Präfixsummen-Index: Man hat Dutzende
    /// Spalten, nicht Millionen. Millionen hat die Zeilenrichtung — deshalb
    /// rechnet RowViewport mit einer Division und diese Klasse darf zählen.
    ///
    /// Wie RowViewport: kein Graphics, kein Control, kopflos prüfbar.
    /// </summary>
    public sealed class ColumnLayout
    {
        private readonly int[] _lefts;    // physisch, kumulativ, ohne Scrollversatz
        private readonly int[] _widths;   // physisch
        private readonly int _scrollOffset;
        private readonly int _totalWidth;
        private readonly int _viewportWidth;
        private readonly int _firstVisibleColumn;
        private readonly int _visibleColumnCount;

        public ColumnLayout(GridColumnCollection columns, int scrollOffset, int viewportWidth, int dpi)
        {
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            if (dpi <= 0)
                throw new ArgumentOutOfRangeException(nameof(dpi), "DPI muss positiv sein.");

            _scrollOffset = scrollOffset > 0 ? scrollOffset : 0;
            _viewportWidth = viewportWidth > 0 ? viewportWidth : 0;

            int count = columns.Count;
            _lefts = new int[count];
            _widths = new int[count];

            int running = 0;
            for (int i = 0; i < count; i++)
            {
                _lefts[i] = running;
                _widths[i] = DpiScale.Scale(columns[i].Width, dpi);
                running += _widths[i];
            }

            _totalWidth = running;

            if (count == 0 || _viewportWidth == 0)
            {
                _firstVisibleColumn = 0;
                _visibleColumnCount = 0;
                return;
            }

            int first = -1;
            int last = -1;
            int right = _scrollOffset + _viewportWidth;

            for (int i = 0; i < count; i++)
            {
                int columnRight = _lefts[i] + _widths[i];

                // Berührt die Spalte das Sichtfenster überhaupt? Eine Spalte der
                // Breite 0 kann es nicht — die halbeoffene Prüfung schließt sie
                // korrekt aus.
                if (columnRight <= _scrollOffset) continue;
                if (_lefts[i] >= right) break;

                if (first < 0) first = i;
                last = i;
            }

            if (first < 0)
            {
                // Der Versatz zeigt hinter die letzte Spalte — kein Wurf, nur
                // nichts zu zeichnen.
                _firstVisibleColumn = 0;
                _visibleColumnCount = 0;
                return;
            }

            _firstVisibleColumn = first;
            _visibleColumnCount = last - first + 1;
        }

        public int FirstVisibleColumn
        {
            get { return _firstVisibleColumn; }
        }

        public int VisibleColumnCount
        {
            get { return _visibleColumnCount; }
        }

        /// <summary>Die linke Kante im Sichtfenster. Negativ, wenn die Spalte links angeschnitten ist.</summary>
        public int ColumnLeft(int columnIndex)
        {
            return _lefts[columnIndex] - _scrollOffset;
        }

        /// <summary>Die Breite in physischen Pixeln — bereits DPI-skaliert.</summary>
        public int ColumnWidth(int columnIndex)
        {
            return _widths[columnIndex];
        }

        /// <summary>Welche Spalte liegt an dieser Stelle? -1, wenn dort keine liegt.</summary>
        public int ColumnAt(int x)
        {
            if (x < 0) return -1;

            int content = x + _scrollOffset;

            for (int i = 0; i < _lefts.Length; i++)
            {
                if (content >= _lefts[i] && content < _lefts[i] + _widths[i]) return i;
            }

            return -1;
        }

        /// <summary>Die Breite aller Spalten zusammen — der Wertebereich der waagerechten Leiste.</summary>
        public int TotalWidth
        {
            get { return _totalWidth; }
        }

        public int MaxScrollOffset
        {
            get
            {
                int max = _totalWidth - _viewportWidth;
                return max > 0 ? max : 0;
            }
        }
    }
}
