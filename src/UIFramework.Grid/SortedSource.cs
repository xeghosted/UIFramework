using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UIFramework.Grid
{
    /// <summary>Ob und wie eine <see cref="SortedSource"/> aktuell sortiert.</summary>
    public enum SortDirection
    {
        None,
        Ascending,
        Descending
    }

    /// <summary>
    /// Sortiert eine andere Quelle, ohne sie zu kopieren -- nur eine Permutation
    /// der Zeilenindizes wird gehalten.
    ///
    /// Sort(...) liest dabei JEDE Zeile der inneren Quelle genau einmal -- das
    /// ist der unvermeidliche Preis des Sortierens, kein Fehler in der
    /// Virtualisierung: Es passiert einmal beim Klick, nicht pro Bild. Danach
    /// ist GetValue wieder eine einfache Umlenkung über die Permutation, genauso
    /// virtualisiert wie zuvor.
    /// </summary>
    public sealed class SortedSource : IWritableGridDataSource
    {
        private readonly IGridDataSource _inner;
        private int[] _order;
        private string _sortColumnKey;
        private SortDirection _direction = SortDirection.None;

        public SortedSource(IGridDataSource inner)
        {
            if (inner == null) throw new ArgumentNullException(nameof(inner));

            _inner = inner;
            _order = Identity(inner.RowCount);
        }

        /// <summary>Die zuletzt sortierte Spalte -- null, solange nie sortiert wurde.</summary>
        public string SortColumnKey
        {
            get { return _sortColumnKey; }
        }

        public SortDirection Direction
        {
            get { return _direction; }
        }

        /// <summary>
        /// Sortiert neu. Stabil: gleiche Schluessel behalten ihre
        /// Ausgangsreihenfolge (LINQ OrderBy/OrderByDescending garantieren das) --
        /// sonst geraeten Zeilen mit gleichem Wert bei jedem erneuten Sortieren
        /// unvorhersehbar durcheinander.
        /// </summary>
        public void Sort(string columnKey, SortDirection direction)
        {
            if (direction != SortDirection.None && columnKey == null)
                throw new ArgumentNullException(nameof(columnKey));

            _sortColumnKey = columnKey;
            _direction = direction;

            Rebuild();
        }

        public int RowCount
        {
            get
            {
                EnsureCurrent();
                return _inner.RowCount;
            }
        }

        public object GetValue(int rowIndex, string columnKey)
        {
            EnsureCurrent();
            return _inner.GetValue(_order[rowIndex], columnKey);
        }

        public void SetValue(int rowIndex, string columnKey, object value)
        {
            EnsureCurrent();   // dieselbe Permutations-Pflege wie beim Lesen

            var writable = _inner as IWritableGridDataSource;
            if (writable == null)
                throw new InvalidOperationException(
                    "Die innere Quelle ist nicht schreibbar — SetValue hat kein Ziel.");

            try
            {
                writable.SetValue(_order[rowIndex], columnKey, value);
            }
            catch (ArgumentException ex) when (ex.Message.Contains("MapSet"))
            {
                // Die innere Quelle hat keinen Setter — interpretieren als "nicht schreibbar"
                throw new InvalidOperationException(
                    "Die innere Quelle ist nicht schreibbar — SetValue hat kein Ziel.", ex);
            }
        }

        /// <summary>
        /// Baut die Permutation neu auf, falls die innere Quelle seit dem
        /// letzten Sort(...) gewachsen oder geschrumpft ist. ListDataSource
        /// haelt ihre Liste, statt sie zu kopieren (2a) -- eine veraltete
        /// Permutation waere sonst eine stille Falle statt eines Wurfs.
        /// </summary>
        private void EnsureCurrent()
        {
            if (_order.Length != _inner.RowCount) Rebuild();
        }

        private void Rebuild()
        {
            int count = _inner.RowCount;

            if (_direction == SortDirection.None)
            {
                _order = Identity(count);
                return;
            }

            var comparer = Comparer<object>.Create((a, b) => Comparer.Default.Compare(a, b));
            var indices = Enumerable.Range(0, count);

            var sorted = _direction == SortDirection.Ascending
                ? indices.OrderBy(i => _inner.GetValue(i, _sortColumnKey), comparer)
                : indices.OrderByDescending(i => _inner.GetValue(i, _sortColumnKey), comparer);

            _order = sorted.ToArray();
        }

        private static int[] Identity(int count)
        {
            var order = new int[count];
            for (int i = 0; i < count; i++) order[i] = i;
            return order;
        }
    }
}
