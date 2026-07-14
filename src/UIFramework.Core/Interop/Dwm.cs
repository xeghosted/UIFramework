using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace UIFramework.Core.Interop
{
    /// <summary>
    /// Zugriff auf die Fensterattribute des Desktop Window Managers.
    ///
    /// Der Nicht-Client-Bereich eines Fensters — Titelleiste, Titeltext, Rahmen —
    /// gehört Windows, nicht WinForms; Control.OnPaint erreicht ihn nie. DWM ist
    /// der einzige Weg, ihn einzufärben.
    ///
    /// Bewusst keine Windows-Versionsabfrage: DwmSetWindowAttribute liefert für
    /// ein unbekanntes Attribut einfach einen Fehlercode zurück. Das Betriebssystem
    /// beantwortet die Frage "kennst du das?" selbst — zuverlässiger als eine
    /// Build-Nummern-Tabelle im Code, die bei jedem Windows-Release altert.
    /// </summary>
    internal static class Dwm
    {
        /// <summary>Farbe der Titelleiste. Ab Windows 11 (Build 22000).</summary>
        private const int CaptionColorAttribute = 35;

        /// <summary>Farbe des Titeltexts. Ab Windows 11 (Build 22000).</summary>
        private const int TextColorAttribute = 36;

        /// <summary>Farbe des Fensterrahmens. Ab Windows 11 (Build 22000).</summary>
        private const int BorderColorAttribute = 34;

        /// <summary>
        /// Dunkle Fensterchrome. Ab Windows 10 (Build 18985); auf 17763..18984 lag
        /// dasselbe Attribut auf 19. Diese Klasse versucht nur die 20 — auf den
        /// betroffenen alten Builds bleibt die Leiste hell, was ein Schönheitsfehler
        /// ist und kein Absturz.
        /// </summary>
        private const int ImmersiveDarkModeAttribute = 20;

        /// <summary>Rückgabewert von DwmSetWindowAttribute bei Erfolg.</summary>
        private const int Ok = 0;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        /// <summary>
        /// Rechnet eine Farbe in ein COLORREF um: 0x00BBGGRR.
        ///
        /// Blau und Rot stehen darin GENAU ANDERSHERUM als in der üblichen
        /// Schreibweise #RRGGBB. Das ist die klassische Falle an dieser Stelle —
        /// vertauscht man sie, erscheint die Titelleiste in einer plausibel
        /// aussehenden, aber falschen Farbe, was beim Draufschauen leicht
        /// durchgeht. Deshalb eine eigene, getestete Funktion.
        ///
        /// Der Alphakanal gehört nicht ins COLORREF: Das oberste Byte muss 0
        /// bleiben, sonst deutet Windows den Wert anders.
        /// </summary>
        internal static int ToColorRef(Color color)
        {
            return color.R | (color.G << 8) | (color.B << 16);
        }

        internal static bool TrySetCaptionColor(IntPtr handle, Color color)
        {
            return TrySetColor(handle, CaptionColorAttribute, color);
        }

        internal static bool TrySetCaptionTextColor(IntPtr handle, Color color)
        {
            return TrySetColor(handle, TextColorAttribute, color);
        }

        internal static bool TrySetBorderColor(IntPtr handle, Color color)
        {
            return TrySetColor(handle, BorderColorAttribute, color);
        }

        /// <summary>
        /// Schaltet die dunkle Fensterchrome. Nötig zusätzlich zu den exakten
        /// Farben: Die Glyphen der Systemknöpfe (Minimieren, Maximieren, Schließen)
        /// folgen NICHT der Titeltextfarbe, sondern diesem Schalter. Ohne ihn
        /// stünden dunkle Glyphen auf dunkler Leiste — unsichtbar.
        /// </summary>
        internal static bool TrySetDarkMode(IntPtr handle, bool dark)
        {
            int value = dark ? 1 : 0;
            return DwmSetWindowAttribute(handle, ImmersiveDarkModeAttribute, ref value, sizeof(int)) == Ok;
        }

        private static bool TrySetColor(IntPtr handle, int attribute, Color color)
        {
            int value = ToColorRef(color);
            return DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int)) == Ok;
        }
    }
}
