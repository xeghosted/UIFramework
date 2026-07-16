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
        /// Corners, BorderWidth und Padding steuern für dieses Element nichts — die
        /// Geometrie der Titelleiste bestimmt Windows. Bedeutungslos heißt aber nicht
        /// beliebig: Der Skin-Editor (Teilprojekt 6) zeigt die Werte an, also dürfen
        /// sie nicht lügen. Deshalb BorderWidth = 1 — Windows zeichnet aufgrund von
        /// BorderColor sehr wohl einen Rahmen.
        /// </summary>
        public const string Window = "Window";
    }
}
