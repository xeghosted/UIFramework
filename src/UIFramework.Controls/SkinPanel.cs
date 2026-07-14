using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Dpi;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Container. Prüfstand für das Zusammenspiel mit dem WinForms-Layout.
    ///
    /// Enthält bewusst keinen einzigen Farbwert — alles Sichtbare kommt aus dem Skin.
    /// </summary>
    [ToolboxItem(true)]
    [Designer("System.Windows.Forms.Design.ParentControlDesigner, System.Design")]
    public class SkinPanel : SkinnedControl
    {
        public SkinPanel()
        {
            SetStyle(ControlStyles.ContainerControl, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            Size = new Size(200, 150);
        }

        protected override string ElementKey
        {
            get { return ElementKeys.Panel; }
        }

        protected override bool ShowFocusRing
        {
            get { return false; }
        }

        /// <summary>
        /// WinForms platziert angedockte Kind-Controls hierin. Ohne dieses
        /// Überschreiben lägen Kinder unter dem Rahmen und im Innenabstand —
        /// der Skin wäre wirkungslos.
        /// </summary>
        public override Rectangle DisplayRectangle
        {
            get
            {
                var appearance = CurrentAppearance;
                int dpi = DeviceDpi;

                var padding = DpiScale.Scale(appearance.Padding, dpi);
                int border = DpiScale.Scale(appearance.BorderWidth, dpi);

                int left = padding.Left + border;
                int top = padding.Top + border;
                int right = padding.Right + border;
                int bottom = padding.Bottom + border;

                var rect = new Rectangle(
                    left,
                    top,
                    ClientRectangle.Width - left - right,
                    ClientRectangle.Height - top - bottom);

                if (rect.Width < 0) rect.Width = 0;
                if (rect.Height < 0) rect.Height = 0;

                return rect;
            }
        }
    }
}
