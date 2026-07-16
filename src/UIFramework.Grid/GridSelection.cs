using System;
using System.Collections.Generic;
using System.Linq;

namespace UIFramework.Grid
{
    /// <summary>
    /// Wer ist ausgewählt, und von wo aus spannt Umschalt?
    ///
    /// Der Anker ist der Punkt, von dem ein Umschalt-Klick spannt; die aktuelle
    /// Zeile ist, wo der Anwender zuletzt war. Beide sind nicht dasselbe: Zweimal
    /// Umschalt-Klick spannt beide Male vom selben Anker, aber die aktuelle Zeile
    /// wandert mit.
    ///
    /// Hält nur Indizes, keine Daten — kopflos prüfbar, und beim Wechsel der
    /// Datenquelle ist nichts freizugeben.
    /// </summary>
    public sealed class GridSelection
    {
        private readonly HashSet<int> _selected = new HashSet<int>();
        private int _anchor = -1;
        private int _current = -1;

        public event EventHandler Changed;

        /// <summary>Wo der Anwender zuletzt war. -1, wenn nirgends.</summary>
        public int CurrentRow
        {
            get { return _current; }
        }

        /// <summary>Der Punkt, von dem ein Umschalt-Klick spannt. -1, wenn keiner.</summary>
        public int AnchorRow
        {
            get { return _anchor; }
        }

        public int Count
        {
            get { return _selected.Count; }
        }

        public bool IsSelected(int rowIndex)
        {
            return _selected.Contains(rowIndex);
        }

        /// <summary>Schlichter Klick: alles andere fällt weg, der Anker rückt hierher.</summary>
        public void Select(int rowIndex)
        {
            // GridHitTest liefert -1 für "dort liegt keine Zeile". Ungeprüft
            // weitergereicht wählte das Zeile -1 aus.
            if (rowIndex < 0) return;

            bool alreadyExactlyThis = _selected.Count == 1 && _selected.Contains(rowIndex);
            if (alreadyExactlyThis && _anchor == rowIndex && _current == rowIndex) return;

            _selected.Clear();
            _selected.Add(rowIndex);
            _anchor = rowIndex;
            _current = rowIndex;
            OnChanged();
        }

        /// <summary>Strg-Klick: dazu oder weg, der Rest bleibt. Der Anker rückt hierher.</summary>
        public void Toggle(int rowIndex)
        {
            if (rowIndex < 0) return;

            if (!_selected.Remove(rowIndex)) _selected.Add(rowIndex);

            _anchor = rowIndex;
            _current = rowIndex;
            OnChanged();
        }

        /// <summary>
        /// Umschalt-Klick: vom Anker bis hierher. Ersetzt die Spanne, statt sie
        /// zu ergänzen — sonst bliebe die vorige stehen und zweimal Umschalt
        /// ergäbe ein Sammelsurium. Der Anker bleibt, wo er war.
        /// </summary>
        public void ExtendTo(int rowIndex)
        {
            if (rowIndex < 0) return;

            if (_anchor < 0)
            {
                // Ohne Anker gibt es nichts zu spannen.
                Select(rowIndex);
                return;
            }

            int from = Math.Min(_anchor, rowIndex);
            int to = Math.Max(_anchor, rowIndex);

            _selected.Clear();
            for (int i = from; i <= to; i++) _selected.Add(i);

            _current = rowIndex;
            OnChanged();
        }

        public void Clear()
        {
            if (_selected.Count == 0 && _anchor < 0 && _current < 0) return;

            _selected.Clear();
            _anchor = -1;
            _current = -1;
            OnChanged();
        }

        /// <summary>
        /// Die Quelle hat weniger Zeilen als vorher — alles dahinter fällt weg.
        /// Ohne das zeigte das Grid nach einem Filter (Teilprojekt 2b) eine
        /// Auswahl auf Zeilen, die es nicht mehr gibt.
        /// </summary>
        public void TrimTo(int rowCount)
        {
            var gone = _selected.Where(i => i >= rowCount).ToList();

            bool anchorGone = _anchor >= rowCount;
            bool currentGone = _current >= rowCount;

            if (gone.Count == 0 && !anchorGone && !currentGone) return;

            foreach (int i in gone) _selected.Remove(i);
            if (anchorGone) _anchor = -1;
            if (currentGone) _current = -1;

            OnChanged();
        }

        private void OnChanged()
        {
            var handler = Changed;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
