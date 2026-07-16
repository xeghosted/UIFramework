using System;
using System.Collections;
using System.Collections.Generic;

namespace UIFramework.Grid
{
    /// <summary>
    /// Die Spalten eines Grids, in ihrer Anzeigereihenfolge.
    ///
    /// Bündelt die Änderungsmeldungen: Sie hängt sich an jede enthaltene Spalte
    /// und reicht deren Changed als eigenes weiter. GridControl führt damit genau
    /// eine Anmeldung statt einer pro Spalte — und hat nichts zu lösen, wenn sich
    /// die Spalten ändern.
    /// </summary>
    public sealed class GridColumnCollection : IEnumerable<GridColumn>
    {
        private readonly List<GridColumn> _columns = new List<GridColumn>();

        private readonly HashSet<string> _keys = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Irgendetwas an den Spalten ist anders — Bestand, Reihenfolge oder eine einzelne Spalte.</summary>
        public event EventHandler Changed;

        public int Count
        {
            get { return _columns.Count; }
        }

        public GridColumn this[int index]
        {
            get { return _columns[index]; }
        }

        public void Add(GridColumn column)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));

            if (!_keys.Add(column.Key))
                throw new ArgumentException(
                    "Der Spaltenschlüssel \"" + column.Key + "\" ist bereits vergeben. " +
                    "Er adressiert die Zelle bei der Datenquelle — doppelt vergeben " +
                    "träfe GetValue stumm die falsche Spalte.",
                    nameof(column));

            _columns.Add(column);
            column.Changed += OnColumnChanged;
            OnChanged();
        }

        public int IndexOf(GridColumn column)
        {
            return _columns.IndexOf(column);
        }

        /// <summary>Verschiebt eine Spalte — das Umordnen per Ziehen hängt daran.</summary>
        public void Move(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _columns.Count)
                throw new ArgumentOutOfRangeException(nameof(fromIndex));
            if (toIndex < 0 || toIndex >= _columns.Count)
                throw new ArgumentOutOfRangeException(nameof(toIndex));

            if (fromIndex == toIndex) return;

            var column = _columns[fromIndex];
            _columns.RemoveAt(fromIndex);
            _columns.Insert(toIndex, column);
            OnChanged();
        }

        public IEnumerator<GridColumn> GetEnumerator()
        {
            return _columns.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void OnColumnChanged(object sender, EventArgs e)
        {
            OnChanged();
        }

        private void OnChanged()
        {
            var handler = Changed;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
