using System;

namespace UIFramework.Grid.Layout
{
    /// <summary>
    /// Das Herz der Virtualisierung: Welche Zeilen liegen im Sichtfenster, und wo?
    ///
    /// Kennt kein Graphics, kein Control, kein Fenster — nur vier Zahlen. Genau
    /// deshalb lässt sich hier kopflos beweisen, dass eine Million Zeilen nur
    /// eine Handvoll sichtbarer ergeben. Steckte diese Rechnung in OnPaint,
    /// könnte kein Test sie je prüfen (die Suite läuft ohne Fenster).
    ///
    /// Alle Werte sind physische Pixel: rowHeight kommt bereits DPI-skaliert
    /// herein. Dieser Typ rechnet keine DPI-Arithmetik.
    /// </summary>
    public struct RowViewport
    {
        private readonly int _rowHeight;
        private readonly int _viewportHeight;
        private readonly int _scrollOffset;
        private readonly int _rowCount;
        private readonly int _firstVisibleRow;
        private readonly int _visibleRowCount;

        public RowViewport(int rowHeight, int viewportHeight, int scrollOffset, int rowCount)
        {
            if (rowHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(rowHeight), "Die Zeilenhöhe muss positiv sein.");
            if (rowCount < 0)
                throw new ArgumentOutOfRangeException(nameof(rowCount), "Die Zeilenanzahl darf nicht negativ sein.");

            _rowHeight = rowHeight;
            _rowCount = rowCount;
            _viewportHeight = viewportHeight > 0 ? viewportHeight : 0;

            // Ein negativer Versatz kommt beim Überschwingen mancher Eingaben vor
            // und bedeutet schlicht "oben".
            _scrollOffset = scrollOffset > 0 ? scrollOffset : 0;

            if (rowCount == 0 || viewportHeight <= 0)
            {
                _firstVisibleRow = 0;
                _visibleRowCount = 0;
                return;
            }

            int first = _scrollOffset / rowHeight;

            if (first >= rowCount)
            {
                // Der Versatz zeigt hinter das Ende. Passiert, wenn die Quelle
                // schrumpft, bevor der Versatz nachgezogen wird — kein Wurf,
                // sondern schlicht nichts zu zeichnen.
                _firstVisibleRow = rowCount;
                _visibleRowCount = 0;
                return;
            }

            // -1, weil die letzte Pixelzeile des Sichtfensters bei
            // offset + height - 1 liegt. Ohne das zählte eine Zeile mit, die
            // genau an der Unterkante beginnt und damit unsichtbar ist.
            int last = (_scrollOffset + viewportHeight - 1) / rowHeight;
            if (last >= rowCount) last = rowCount - 1;

            _firstVisibleRow = first;
            _visibleRowCount = last - first + 1;
        }

        /// <summary>Die erste Zeile, die (auch nur angeschnitten) im Sichtfenster liegt.</summary>
        public int FirstVisibleRow
        {
            get { return _firstVisibleRow; }
        }

        /// <summary>
        /// Wie viele Zeilen zu zeichnen sind — angeschnittene oben und unten
        /// eingeschlossen. Wer sie weglässt, hinterlässt ungezeichnete Streifen.
        /// </summary>
        public int VisibleRowCount
        {
            get { return _visibleRowCount; }
        }

        /// <summary>
        /// Die Oberkante einer Zeile im Sichtfenster. Darf negativ sein — genau
        /// dann ist die Zeile oben angeschnitten.
        /// </summary>
        public int RowTop(int rowIndex)
        {
            return rowIndex * _rowHeight - _scrollOffset;
        }

        /// <summary>
        /// Die Umkehrung: Welche Zeile liegt an dieser Stelle? -1, wenn dort
        /// keine liegt (über dem Anfang oder unter der letzten Zeile).
        /// </summary>
        public int RowAt(int y)
        {
            if (y < 0) return -1;

            int row = (y + _scrollOffset) / _rowHeight;
            if (row < 0 || row >= _rowCount) return -1;

            return row;
        }

        /// <summary>Die Höhe aller Zeilen zusammen — der Wertebereich der Bildlaufleiste.</summary>
        public int TotalHeight
        {
            get { return _rowCount * _rowHeight; }
        }

        /// <summary>
        /// Der größte sinnvolle Versatz: darüber hinaus käme nur Leere.
        ///
        /// Rechnet gegen die ECHTE Sichtfensterhöhe, nicht gegen
        /// VisibleRowCount * RowHeight. Der Unterschied ist keine Feinheit: Bei
        /// 10 Zeilen à 30 in einem 100 hohen Fenster sind vier Zeilen sichtbar
        /// (die letzte angeschnitten), also ergäbe die Zeilenrechnung 120 statt
        /// 100 und damit einen um 20px zu kleinen Maximalversatz — die letzte
        /// Zeile bliebe unerreichbar. Der Test
        /// At_the_very_bottom_the_last_row_is_visible fängt genau das.
        /// </summary>
        public int MaxScrollOffset
        {
            get
            {
                int max = TotalHeight - _viewportHeight;
                return max > 0 ? max : 0;
            }
        }
    }
}
