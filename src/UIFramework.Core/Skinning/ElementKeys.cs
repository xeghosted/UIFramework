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

        /// <summary>
        /// Die Fläche und der Rahmen des Grid-Controls selbst — sichtbar nur dort,
        /// wo keine Zellen liegen: unter der letzten Zeile und rechts der letzten
        /// Spalte. Sie muss GridCell bewusst NICHT gleichen; ein Unterschied zeigt,
        /// wo die Daten enden.
        /// </summary>
        public const string Grid = "Grid";

        /// <summary>Eine Kopfzelle des Grids.</summary>
        public const string GridHeader = "GridHeader";

        /// <summary>
        /// Eine Datenzelle. Ihr Hintergrund IST zugleich der Zeilenhintergrund —
        /// er wird über die volle Zeilenbreite gezogen. Deshalb gibt es keinen
        /// eigenen GridRow-Schlüssel: Fläche und Text wechseln über die Zustände
        /// dieser einen Erscheinung gemeinsam und können nie auseinanderlaufen.
        /// </summary>
        public const string GridCell = "GridCell";

        /// <summary>Rinne und Pfeilflächen einer Bildlaufleiste.</summary>
        public const string ScrollBar = "ScrollBar";

        /// <summary>Der Daumen einer Bildlaufleiste.</summary>
        public const string ScrollBarThumb = "ScrollBarThumb";

        /// <summary>Eine einzeilige Texteingabe.</summary>
        public const string TextBox = "TextBox";

        /// <summary>Der geschlossene Zustand eines Dropdowns (Feld + Pfeil).</summary>
        public const string ComboBox = "ComboBox";

        /// <summary>Der Rahmen des aufgeklappten Dropdown-Popups. Die Zeilen darin
        /// nutzen ElementKeys.GridCell — kein eigener Zeilenschlüssel.</summary>
        public const string ComboBoxList = "ComboBoxList";

        /// <summary>Ein Reiter in der Kopfleiste von SkinTabControl.</summary>
        public const string Tab = "Tab";

        /// <summary>Ein Knopf in der Knopfzone eines Editors (Spin-Pfeile, Kalender,
        /// Dropdown) — elementweise gemalt, kein eigenes Control.</summary>
        public const string EditorButton = "EditorButton";

        /// <summary>Das CheckEdit-Control als Ganzes: Fläche wie ein Label,
        /// ForeColor ist die Textfarbe.</summary>
        public const string CheckBox = "CheckBox";

        /// <summary>Die gezeichnete Box eines CheckEdit. Der Haken nutzt ForeColor.</summary>
        public const string CheckBoxIndicator = "CheckBoxIndicator";

        /// <summary>Ein Tag im Monatsblatt. Disabled = Tag des Nachbarmonats —
        /// die ersten Schlüssel mit dieser Semantik (fürs Skin-Format dokumentieren,
        /// Teilprojekt 6).</summary>
        public const string CalendarDay = "CalendarDay";

        /// <summary>Kopf des Monatsblatts: Monat/Jahr-Zeile und Wochentagszeile.
        /// Hovered gilt für die Vor/Zurück-Pfeilflächen.</summary>
        public const string CalendarHeader = "CalendarHeader";

        /// <summary>Die Heute-Zeile unter dem Monatsblatt.</summary>
        public const string CalendarToday = "CalendarToday";

        /// <summary>Die Fläche der Menüleiste (der Streifen unter der Titelleiste).</summary>
        public const string MenuBar = "MenuBar";

        /// <summary>Ein Top-Level-Eintrag in der Menüleiste. Selected = sein Dropdown
        /// ist offen (nicht "angehakt" — Haken gibt es nur bei MenuItem).</summary>
        public const string MenuBarItem = "MenuBarItem";

        /// <summary>Rahmen und Fläche eines aufgeklappten Menü-Popups. Die Einträge
        /// darin nutzen ElementKeys.MenuItem.</summary>
        public const string MenuPopup = "MenuPopup";

        /// <summary>Ein Eintrag in einem Menü-Popup. Hovered ist zugleich die
        /// Tastatur-Auswahl — Menüs kennen nur EINE Hervorhebung. Haken und
        /// Untermenü-Pfeil nutzen ForeColor, der Shortcut-Text ebenfalls.</summary>
        public const string MenuItem = "MenuItem";

        /// <summary>Die Trennlinie in einem Menü-Popup: BorderColor/BorderWidth ist
        /// die Linie, Padding der horizontale Einzug, die Höhe der Zeile folgt aus
        /// dem vertikalen Padding (InflateByPadding über einer leeren Größe).</summary>
        public const string MenuSeparator = "MenuSeparator";
    }
}
