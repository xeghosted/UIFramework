namespace UIFramework.Grid
{
    /// <summary>
    /// Die Schreib-Naht der Zellbearbeitung — bewusst ein einziges Mitglied
    /// über IGridDataSource hinaus (Spec 3b). Dekoratoren (SortedSource,
    /// FilteredSource) implementieren sie und übersetzen den Zeilenindex mit
    /// derselben Abbildung, die sie beim Lesen benutzen; ist ihre innere
    /// Quelle nicht schreibbar, wirft SetValue InvalidOperationException —
    /// das IST ihr Nicht-schreibbar-Sein, denn ein Interface kann nicht
    /// bedingt implementiert werden.
    /// </summary>
    public interface IWritableGridDataSource : IGridDataSource
    {
        /// <summary>
        /// Schreibt einen Zellwert. Nach dem Schreiben bleibt die Zeile, wo
        /// sie ist, auch wenn der neue Wert ihre Sortier- oder Filterposition
        /// ändern würde — sortiert/gefiltert wird wie seit 2b nur auf Aufruf,
        /// nie pro Bild (bewusste Grenze, Spec 3b).
        /// </summary>
        void SetValue(int rowIndex, string columnKey, object value);
    }
}
