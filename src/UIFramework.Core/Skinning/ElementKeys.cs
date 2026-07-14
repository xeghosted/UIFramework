namespace UIFramework.Core.Skinning
{
    /// <summary>
    /// Elementschlüssel sind Strings, damit ein Skin serialisierbar bleibt und
    /// der Skin-Editor (Teilprojekt 6) damit umgehen kann. Diese Konstanten sorgen
    /// dafür, dass Tippfehler beim Compiler auffallen statt zur Laufzeit.
    /// </summary>
    public static class ElementKeys
    {
        public const string Button = "Button";
        public const string Panel = "Panel";
        public const string Label = "Label";

        /// <summary>Fokusring — als Überlagerung über jedem Control gezeichnet.</summary>
        public const string Focus = "Focus";

        /// <summary>
        /// Das Fenster selbst — Titelleiste, Titeltext, Rahmen. Anders als bei allen
        /// übrigen Elementen zeichnet das Framework hier nichts: der Nicht-Client-Bereich
        /// gehört Windows, OnPaint erreicht ihn nie. Die Farben gehen über
        /// DWM-Fensterattribute an das Betriebssystem (siehe SkinnedForm).
        /// Deshalb sind Corners, BorderWidth und Padding für dieses Element
        /// bedeutungslos — die Geometrie der Titelleiste bestimmt Windows.
        /// </summary>
        public const string Window = "Window";
    }
}
