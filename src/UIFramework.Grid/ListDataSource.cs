using System;
using System.Collections.Generic;

namespace UIFramework.Grid
{
    /// <summary>
    /// Adapter über eine IList&lt;T&gt; — der Normalfall.
    ///
    /// Die Generik sitzt hier und nicht am Grid: Ein GridControl&lt;T&gt; wäre für
    /// den WinForms-Designer nicht instanziierbar und färbte auf Teilprojekt 2b
    /// und 2c ab. Hier stört sie niemanden.
    ///
    /// Hält die Liste, kopiert sie nicht: Wächst sie, wächst das Grid mit.
    /// </summary>
    public sealed class ListDataSource<T> : IGridDataSource
    {
        private readonly IList<T> _items;

        private readonly Dictionary<string, Func<T, object>> _accessors =
            new Dictionary<string, Func<T, object>>(StringComparer.Ordinal);

        public ListDataSource(IList<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            _items = items;
        }

        /// <summary>
        /// Verbindet einen Spaltenschlüssel mit dem Weg zu seinem Wert.
        /// Zweimal derselbe Schlüssel ersetzt den vorherigen Zugriff.
        /// </summary>
        public void Map(string columnKey, Func<T, object> accessor)
        {
            if (columnKey == null) throw new ArgumentNullException(nameof(columnKey));
            if (accessor == null) throw new ArgumentNullException(nameof(accessor));

            _accessors[columnKey] = accessor;
        }

        public int RowCount
        {
            get { return _items.Count; }
        }

        public object GetValue(int rowIndex, string columnKey)
        {
            if (columnKey == null) throw new ArgumentNullException(nameof(columnKey));
            if (rowIndex < 0 || rowIndex >= _items.Count)
                throw new ArgumentOutOfRangeException(nameof(rowIndex),
                    "Zeile " + rowIndex + " liegt außerhalb der Quelle (" + _items.Count + " Zeilen).");

            Func<T, object> accessor;
            if (!_accessors.TryGetValue(columnKey, out accessor))
                throw new ArgumentException(
                    "Für die Spalte \"" + columnKey + "\" wurde kein Zugriff eingerichtet. " +
                    "Ohne Map(...) bliebe sie leer, und das sähe aus wie fehlende Daten.",
                    nameof(columnKey));

            return accessor(_items[rowIndex]);
        }
    }
}
