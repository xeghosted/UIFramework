using System.Drawing;
using System.Windows.Forms;

namespace UIFramework.Core.Skinning
{
    /// <summary>
    /// Eine Zelle der Skin-Tabelle: das Erscheinungsbild eines Elements in einem Zustand.
    /// Alle Maße sind logisch (96-DPI-Basis); die Skalierung passiert im Painter.
    /// Wird von den Skin-Klassen einmal aufgebaut und danach nur noch gelesen.
    /// </summary>
    public sealed class ElementAppearance
    {
        public Color Background { get; set; }

        /// <summary>Endfarbe eines senkrechten Verlaufs. Null bedeutet einfarbig.</summary>
        public Color? BackgroundGradientEnd { get; set; }

        public Color BorderColor { get; set; }

        /// <summary>Rahmenbreite in logischen Einheiten. 0 bedeutet kein Rahmen.</summary>
        public int BorderWidth { get; set; }

        /// <summary>Eckradien in logischen Einheiten.</summary>
        public CornerRadius Corners { get; set; }

        public Color ForeColor { get; set; }

        public FontSpec Font { get; set; }

        /// <summary>Innenabstand in logischen Einheiten.</summary>
        public Padding Padding { get; set; }

        public bool HasGradient
        {
            get { return BackgroundGradientEnd.HasValue; }
        }
    }
}
