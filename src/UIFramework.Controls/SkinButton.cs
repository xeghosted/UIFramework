using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Schaltfläche. Prüfstand für die Zustandsmaschine der Basisklasse.
    ///
    /// Enthält bewusst keinen einzigen Farbwert — alles Sichtbare kommt aus dem Skin.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("Click")]
    public class SkinButton : SkinnedControl
    {
        private ContentAlignment _textAlignment = ContentAlignment.MiddleCenter;

        public SkinButton()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Size = new Size(96, 30);
        }

        protected override string ElementKey
        {
            get { return ElementKeys.Button; }
        }

        [Category("Appearance")]
        [DefaultValue(ContentAlignment.MiddleCenter)]
        public ContentAlignment TextAlignment
        {
            get { return _textAlignment; }
            set
            {
                if (_textAlignment == value) return;
                _textAlignment = value;
                Invalidate();
            }
        }

        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            if (string.IsNullOrEmpty(Text)) return;

            // Padding wird nicht hier gerechnet, sondern im Painter über DpiScale:
            // Controls dürfen selbst nicht skalieren.
            SkinPainter.DrawPaddedText(g, Text, ClientRectangle, appearance, DeviceDpi, _textAlignment);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && CanFocus) Focus();
            base.OnMouseDown(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Space || keyData == Keys.Enter) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                PerformClick();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        public void PerformClick()
        {
            if (!Enabled) return;
            OnClick(EventArgs.Empty);
        }
    }
}
