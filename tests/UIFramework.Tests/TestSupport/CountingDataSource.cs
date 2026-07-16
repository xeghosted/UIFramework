using System.Collections.Generic;
using UIFramework.Grid;

namespace UIFramework.Tests.TestSupport
{
    /// <summary>
    /// Eine Datenquelle, die mitzählt, wie oft man sie anfasst — und dabei
    /// beliebig viele Zeilen behauptet, ohne eine einzige zu halten.
    ///
    /// Das Beweismittel für Virtualisierung. "Flüssig" misst auf einer
    /// Testmaschine niemand reproduzierbar, aber "berührt 300 Zellen statt
    /// 1.000.000" ist deterministisch und braucht keine Uhr.
    /// </summary>
    public sealed class CountingDataSource : IGridDataSource
    {
        private readonly HashSet<int> _touchedRows = new HashSet<int>();

        public CountingDataSource(int rowCount)
        {
            RowCount = rowCount;
        }

        public int RowCount { get; }

        /// <summary>Wie oft GetValue insgesamt gerufen wurde.</summary>
        public int GetValueCalls { get; private set; }

        /// <summary>Wie viele VERSCHIEDENE Zeilen angefasst wurden.</summary>
        public int TouchedRowCount
        {
            get { return _touchedRows.Count; }
        }

        /// <summary>Die kleinste angefasste Zeile — -1, wenn keine.</summary>
        public int LowestTouchedRow { get; private set; } = -1;

        /// <summary>Die größte angefasste Zeile — -1, wenn keine.</summary>
        public int HighestTouchedRow { get; private set; } = -1;

        public void Reset()
        {
            _touchedRows.Clear();
            GetValueCalls = 0;
            LowestTouchedRow = -1;
            HighestTouchedRow = -1;
        }

        public object GetValue(int rowIndex, string columnKey)
        {
            GetValueCalls++;
            _touchedRows.Add(rowIndex);

            if (LowestTouchedRow < 0 || rowIndex < LowestTouchedRow) LowestTouchedRow = rowIndex;
            if (rowIndex > HighestTouchedRow) HighestTouchedRow = rowIndex;

            // Kurz und stabil: Der Text soll die Messung nicht dominieren.
            return "R" + rowIndex + columnKey;
        }
    }
}
