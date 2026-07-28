using System;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    /// <summary>
    /// Der schmale Vertrag zwischen Grid und Zelleditor (Spec 3b): das Control
    /// selbst (positionieren, fokussieren), Wert laden/auslesen, Tipp-Start,
    /// und die beiden Bitten des Editors an seinen Wirt. Er liegt in
    /// UIFramework.Controls, nicht im Grid — das Grid referenziert die
    /// Controls, nie umgekehrt.
    /// </summary>
    public interface IGridCellEditor
    {
        /// <summary>Das Control, das der Wirt über die Zelle legt — bei allen
        /// fünf Editoren das Control selbst.</summary>
        Control EditorControl { get; }

        /// <summary>Liest/schreibt den Wert. Das Lesen liefert IMMER den
        /// aktuellen Stand inklusive noch unbestätigten Texts, ohne den
        /// Control-Zustand zu verändern — der Zwangs-Commit des Grids braucht
        /// deshalb keinen Bestätigungs-Handshake vor dem Auslesen.</summary>
        object EditValue { get; set; }

        /// <summary>Lostippen: ersetzt den Text des Kerns und stellt das Caret
        /// ans Ende. Editoren ohne Textkern ignorieren den Aufruf.</summary>
        void BeginWith(string text);

        /// <summary>Der Editor bittet um Bestätigen (Enter im Kern,
        /// Fokusverlust des Kerns ohne offenes eigenes Popup).</summary>
        event EventHandler ConfirmRequested;

        /// <summary>Der Editor bittet um Verwerfen (Escape).</summary>
        event EventHandler CancelRequested;
    }
}
