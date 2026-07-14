using System;
using System.Windows.Forms;
using UIFramework.Core.Interop;
using UIFramework.Core.Skinning;

namespace UIFramework.Core.Controls
{
    /// <summary>
    /// Ein Fenster, dessen Titelleiste, Titeltext und Rahmen dem aktiven Skin
    /// folgen. Anwendungen erben davon und bekommen das ohne weiteres Zutun.
    ///
    /// Warum das nicht wie bei SkinnedControl über OnPaint geht: Die Titelleiste
    /// ist der Nicht-Client-Bereich und gehört Windows. OnPaint erreicht sie nie.
    /// Der einzige Weg führt über DWM-Fensterattribute (siehe Dwm).
    ///
    /// Wie sie vom Skin-Wechsel erfährt: Sie registriert sich — schwach, wie jedes
    /// Control — beim SkinManager und reagiert auf das Invalidate, das
    /// SkinManager.InvalidateAll() daraufhin auslöst. BEWUSST kein Abo auf
    /// SkinManager.SkinChanged: Ein statisches Event hielte jedes je erzeugte
    /// Fenster am Leben — genau das Leck, das die schwache Registrierung
    /// verhindert und vor dem der SkinManager selbst warnt.
    /// </summary>
    public class SkinnedForm : Form
    {
        private ElementAppearance _applied;
        private int _captionApplyCount;

        public SkinnedForm()
        {
            SkinManager.Register(this);
        }

        /// <summary>
        /// Wie oft die Titelleiste tatsächlich an Windows geschoben wurde.
        /// Nur für Tests: der Merker unten lässt sich sonst nicht nachweisen.
        /// </summary>
        internal int CaptionApplyCount
        {
            get { return _captionApplyCount; }
        }

        /// <summary>
        /// Dunkel oder hell — aus Skin-Daten abgeleitet, nicht aus einem Flag.
        /// Ist der Titeltext heller als die Leiste, ist es eine dunkle Leiste.
        /// So muss ein dritter Skin nichts zusätzlich deklarieren, und es gibt
        /// kein ISkin.IsDark, das mit den Farben aus dem Tritt geraten könnte.
        /// </summary>
        internal static bool IsDarkCaption(ElementAppearance appearance)
        {
            return appearance.ForeColor.GetBrightness() > appearance.Background.GetBrightness();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyCaptionIfChanged();
        }

        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            base.OnInvalidated(e);
            ApplyCaptionIfChanged();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) SkinManager.Unregister(this);
            base.Dispose(disposing);
        }

        private void ApplyCaptionIfChanged()
        {
            // Ohne Fenster gibt es nichts einzufärben.
            if (!IsHandleCreated) return;

            var appearance = SkinManager.Current.GetAppearance(ElementKeys.Window, ElementState.Normal);

            // Ein Skin liefert für denselben Schlüssel stets dieselbe Instanz.
            // OnInvalidated feuert bei jedem Neuzeichnen — ohne diesen Vergleich
            // ginge jedes Mal ein Schwung P/Invoke-Aufrufe raus.
            if (ReferenceEquals(appearance, _applied)) return;
            _applied = appearance;
            _captionApplyCount++;

            // Zuerst der Schalter: Die Glyphen der Systemknöpfe folgen ihm, nicht
            // der Titeltextfarbe. Ohne ihn stünden dunkle Glyphen auf dunkler Leiste.
            Dwm.TrySetDarkMode(Handle, IsDarkCaption(appearance));

            // Dann die exakten Farben. Auf Windows 10 und älter kennt DWM diese
            // Attribute nicht und lehnt sie ab — dann bleibt es beim Schalter oben.
            Dwm.TrySetCaptionColor(Handle, appearance.Background);
            Dwm.TrySetCaptionTextColor(Handle, appearance.ForeColor);
            Dwm.TrySetBorderColor(Handle, appearance.BorderColor);
        }
    }
}
