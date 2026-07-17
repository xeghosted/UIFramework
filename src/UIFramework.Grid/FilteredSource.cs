using System;
using System.Collections.Generic;

namespace UIFramework.Grid
{
    /// <summary>
    /// Zeigt nur die Zeilen einer anderen Quelle, die ein Praedikat erfuellen --
    /// haelt dafuer eine Liste innerer Zeilenindizes, keine eigenen Daten.
    ///
    /// Baut die Trefferliste einmal beim Erzeugen und bei jedem Refresh() neu
    /// auf: das ist der Preis des Filterns (jede Zeile der Quelle einmal
    /// pruefen, einmal beim Klick, nicht pro Bild), danach ist GetValue wieder
    /// eine einfache Umlenkung.
    /// </summary>
    public sealed class FilteredSource : IGridDataSource
    {
        private readonly IGridDataSource _inner;
        private readonly Func<IGridDataSource, int, bool> _predicate;
        private int[] _matching;

        /// <summary>
        /// predicate bekommt die INNERE Quelle und den inneren Zeilenindex --
        /// nicht nur einen Wert -- damit es jede beliebige Spalte der Zeile
        /// lesen kann, ohne dass FilteredSource selbst wissen muss, wonach
        /// gefiltert wird.
        /// </summary>
        public FilteredSource(IGridDataSource inner, Func<IGridDataSource, int, bool> predicate)
        {
            if (inner == null) throw new ArgumentNullException(nameof(inner));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            _inner = inner;
            _predicate = predicate;
            Refresh();
        }

        /// <summary>
        /// Wendet das Praedikat erneut auf die innere Quelle an. Nicht
        /// automatisch bei jedem Zugriff: Ob sich die Quelle geaendert hat, ist
        /// ohne erneutes volles Lesen nicht entscheidbar -- genau die Kosten,
        /// die vermieden werden sollen. Wer eine veraenderliche Quelle filtert,
        /// ruft Refresh() selbst, wenn er weiss, dass sich etwas geaendert hat.
        /// </summary>
        public void Refresh()
        {
            int count = _inner.RowCount;
            var matches = new List<int>();

            for (int i = 0; i < count; i++)
            {
                if (_predicate(_inner, i)) matches.Add(i);
            }

            _matching = matches.ToArray();
        }

        /// <summary>Die Anzahl der TREFFER, nicht die der inneren Quelle.</summary>
        public int RowCount
        {
            get { return _matching.Length; }
        }

        public object GetValue(int rowIndex, string columnKey)
        {
            return _inner.GetValue(_matching[rowIndex], columnKey);
        }
    }
}
