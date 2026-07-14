namespace UIFramework.Core.Skinning
{
    /// <summary>
    /// Ein Skin ist Daten, kein Code: eine Nachschlagetabelle
    /// Element × Zustand → Erscheinungsbild.
    /// Genau das macht den Skin-Editor (Teilprojekt 6) überhaupt möglich.
    /// </summary>
    public interface ISkin
    {
        string Name { get; }

        /// <summary>
        /// Liefert immer ein Erscheinungsbild, nie null und nie eine Exception —
        /// notfalls über die Rückfallkette. Ein lückenhafter Skin ist erlaubt.
        /// </summary>
        ElementAppearance GetAppearance(string elementKey, ElementState state);
    }
}
