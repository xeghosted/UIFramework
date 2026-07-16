namespace UIFramework.Grid
{
    /// <summary>
    /// Die Naht zwischen Grid und Daten — bewusst so schmal wie möglich.
    ///
    /// Zwei Mitglieder, weil Virtualisierung genau zwei Dinge braucht: zu wissen,
    /// wie viele Zeilen es gibt, ohne sie zu sehen, und an eine einzelne Zelle zu
    /// kommen, ohne die davor zu berühren. Ein IEnumerable kann beides nicht —
    /// man müsste es materialisieren, und genau das soll nie passieren.
    ///
    /// Sie ist zugleich die Naht für Teilprojekt 2b: Sortieren und Filtern werden
    /// Dekoratoren über dieser Schnittstelle (SortedSource, FilteredSource) und
    /// fassen das Grid nicht an.
    /// </summary>
    public interface IGridDataSource
    {
        /// <summary>
        /// Die Anzahl der Zeilen. Wird bei jedem Layout gelesen — muss also
        /// billig sein und darf die Daten nicht anfassen.
        /// </summary>
        int RowCount { get; }

        /// <summary>
        /// Der Wert einer Zelle. Wird beim Zeichnen NUR für sichtbare Zellen
        /// gerufen (rund 30 Zeilen × Spaltenzahl). Ein Aufruf pro Zeile der
        /// Quelle wäre ein Fehler und wird von GridVirtualizationTests gefangen.
        /// </summary>
        object GetValue(int rowIndex, string columnKey);
    }
}
