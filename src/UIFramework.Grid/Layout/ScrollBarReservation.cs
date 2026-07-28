using System;

namespace UIFramework.Grid.Layout
{
    /// <summary>
    /// Welche Bildlaufleisten sichtbar sind und wie viel Fläche dem Inhalt
    /// bleibt — die Antwort auf die Henne-Ei-Frage der Leisten: Eine sichtbare
    /// senkrechte Leiste macht das Sichtfenster schmaler, wodurch die
    /// waagerechte nötig werden kann, und umgekehrt.
    ///
    /// Wie RowViewport und ColumnLayout: kein Graphics, kein Control, nur
    /// Zahlen — kopflos prüfbar. Alle Maße sind physische Pixel; contentWidth
    /// und contentHeight sind die Gesamtmaße des Inhalts (TotalWidth der
    /// Spalten, TotalHeight der Zeilen), headerHeight liegt dauerhaft über dem
    /// Zeilenbereich und steht nie zur Verfügung.
    /// </summary>
    public struct ScrollBarReservation
    {
        public ScrollBarReservation(int clientWidth, int clientHeight, int headerHeight,
                                    int contentWidth, int contentHeight, int barThickness)
        {
            if (barThickness <= 0)
                throw new ArgumentOutOfRangeException(nameof(barThickness), "Die Leistendicke muss positiv sein.");

            int width = Max0(clientWidth);
            int height = Max0(clientHeight - headerHeight);

            // Erst gegen die volle Fläche prüfen, dann die Kaskade: Eine Leiste,
            // die Platz nimmt, kann die jeweils andere erst nötig machen. Mehr
            // als ein Nachziehen braucht es nie — eine einmal sichtbare Leiste
            // verschwindet durch die andere nicht wieder, und eine waagerechte,
            // die schon gegen die verschmälerte Breite nötig war, bleibt es
            // gegen jede noch schmalere erst recht.
            bool vertical = contentHeight > height;
            bool horizontal = contentWidth > (vertical ? Max0(width - barThickness) : width);
            if (!vertical && horizontal)
                vertical = contentHeight > Max0(height - barThickness);

            VerticalVisible = vertical;
            HorizontalVisible = horizontal;
            ViewportWidth = Max0(width - (vertical ? barThickness : 0));
            ViewportHeight = Max0(height - (horizontal ? barThickness : 0));
        }

        /// <summary>Ob die senkrechte Leiste sichtbar ist.</summary>
        public bool VerticalVisible { get; }

        /// <summary>Ob die waagerechte Leiste sichtbar ist.</summary>
        public bool HorizontalVisible { get; }

        /// <summary>Für Inhalt nutzbare Breite — Clientbreite abzüglich sichtbarer senkrechter Leiste.</summary>
        public int ViewportWidth { get; }

        /// <summary>Für Inhalt nutzbare Höhe UNTER dem Kopf — abzüglich sichtbarer waagerechter Leiste.</summary>
        public int ViewportHeight { get; }

        private static int Max0(int value)
        {
            return value > 0 ? value : 0;
        }
    }
}
