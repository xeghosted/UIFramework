using System;
using UIFramework.Controls;

namespace UIFramework.Grid
{
    /// <summary>
    /// Eine Spalte: ihr Schlüssel zur Datenquelle, ihr Kopftext, ihre Breite.
    ///
    /// Die Breite ist in LOGISCHEN Einheiten (96-DPI-Basis) formuliert, wie alles
    /// Maßhafte in diesem Framework — skaliert wird erst beim Zeichnen. Damit
    /// überlebt ein gespeichertes Spaltenlayout einen Monitorwechsel.
    /// </summary>
    public sealed class GridColumn
    {
        private string _header;
        private int _width = 100;
        private int _minWidth = 24;

        public GridColumn(string key, string header)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            Key = key;
            _header = header;
        }

        /// <summary>
        /// Änderung an dieser Spalte — Breite oder Kopftext. Die Sammlung hängt
        /// sich hier ein und reicht es weiter, damit GridControl nur eine
        /// Anmeldung führen muss statt einer pro Spalte.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Adressiert die Zelle bei der Datenquelle. Unveränderlich: Ein Wechsel
        /// zur Laufzeit würde die Spalte still auf andere Daten zeigen lassen.
        /// </summary>
        public string Key { get; }

        public string Header
        {
            get { return _header; }
            set
            {
                if (string.Equals(_header, value, StringComparison.Ordinal)) return;
                _header = value;
                OnChanged();
            }
        }

        /// <summary>Breite in logischen Einheiten. Nie unter <see cref="MinWidth"/>.</summary>
        public int Width
        {
            get { return _width; }
            set
            {
                int clamped = value < _minWidth ? _minWidth : value;

                // Auf den GEKLEMMTEN Wert vergleichen: Beim Ziehen unter die
                // Mindestbreite kämen sonst Dutzende Ereignisse für dieselbe Breite.
                if (_width == clamped) return;

                _width = clamped;
                OnChanged();
            }
        }

        /// <summary>
        /// Mindestbreite in logischen Einheiten. Ohne sie zöge der Anwender eine
        /// Spalte auf 0 und fände sie nie wieder.
        /// </summary>
        public int MinWidth
        {
            get { return _minWidth; }
            set
            {
                int wanted = value < 1 ? 1 : value;
                if (_minWidth == wanted) return;

                _minWidth = wanted;
                Width = _width;   // klemmt neu und meldet, falls es wirklich schiebt
            }
        }

        /// <summary>
        /// Sperrt die Spalte für die Zellbearbeitung, auch wenn Quelle und Fabrik
        /// vorhanden sind. Kein Changed-Ereignis: ReadOnly ändert nichts Sichtbares
        /// am Blatt, nur das Verhalten beim Aktivieren.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Erzeugt den Zelleditor dieser Spalte — eine Fabrik statt einer
        /// Aufzählung, weil die App den Editor KONFIGURIERT zurückgeben muss
        /// (SpinEdit mit Min/Max, Combo mit Items; Spec 3b). Null = Spalte nicht
        /// bearbeitbar. Das Grid ruft sie je Aktivierung und entsorgt das Control
        /// beim Schließen.
        /// </summary>
        public Func<IGridCellEditor> EditorFactory { get; set; }

        private void OnChanged()
        {
            var handler = Changed;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
