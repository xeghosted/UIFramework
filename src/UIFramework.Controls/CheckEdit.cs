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
    /// Zweizustands-Schalter: gezeichnete Box (ElementKeys.CheckBox, ihr
    /// ForeColor ist die Hakenfarbe) plus Text rechts daneben. Eigenständig
    /// neben ButtonEditBase — es gibt kein Textfeld. Umschalten per Klick und
    /// Leertaste; fokussierbar mit Fokusring.
    ///
    /// Enthält bewusst keinen einzigen Farbwert — alles Sichtbare kommt aus dem Skin.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("CheckedChanged")]
    public class CheckEdit : SkinnedControl
    {
        private bool _checked;

        public CheckEdit()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Size = new Size(120, 24);
        }

        protected override string ElementKey
        {
            get { return ElementKeys.CheckBox; }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                OnCheckedChanged(EventArgs.Empty);
            }
        }

        public event EventHandler CheckedChanged;

        protected virtual void OnCheckedChanged(EventArgs e)
        {
            var handler = CheckedChanged;
            if (handler != null) handler(this, e);
        }

        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            var content = SkinPainter.GetContentRectangle(ClientRectangle, appearance, DeviceDpi);
            int side = Math.Min(content.Height, content.Width);
            var box = new Rectangle(content.Left, content.Top + (content.Height - side) / 2, side, side);

            // Fläche und Rahmen der Box malt die Basis NICHT — sie hat den
            // Control-Hintergrund gemalt. Die Box ist hier Inhalt.
            SkinPainter.DrawBackground(g, box, appearance, DeviceDpi);
            SkinPainter.DrawBorder(g, box, appearance, DeviceDpi);

            if (_checked)
            {
                // Haken als Polylinie im inneren Drittel der Box.
                var pen = ResourceCache.Shared.GetPen(appearance.ForeColor, Math.Max(2, side / 8));
                int x0 = box.Left + side / 4;
                int y0 = box.Top + side / 2;
                int x1 = box.Left + side * 2 / 5;
                int y1 = box.Top + side * 7 / 10;
                int x2 = box.Left + side * 3 / 4;
                int y2 = box.Top + side * 3 / 10;
                g.DrawLines(pen, new[] { new Point(x0, y0), new Point(x1, y1), new Point(x2, y2) });
            }

            if (!string.IsNullOrEmpty(Text))
            {
                var textRect = new Rectangle(box.Right + box.Width / 3, content.Top,
                    Math.Max(0, content.Right - box.Right - box.Width / 3), content.Height);
                SkinPainter.DrawText(g, Text, textRect, appearance, DeviceDpi, ContentAlignment.MiddleLeft);
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Enabled)
            {
                if (CanFocus) Focus();
                Checked = !Checked;
            }
            base.OnMouseClick(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Space) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            PerformKey(e.KeyCode);
            if (e.KeyCode == Keys.Space) e.Handled = true;
            base.OnKeyDown(e);
        }

        /// <summary>Tastenlogik separat, damit Tests sie ohne Fokus-Maschinerie
        /// treiben können (Muster: GridControl.PerformKey).</summary>
        internal void PerformKey(Keys key)
        {
            if (key == Keys.Space && Enabled) Checked = !Checked;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var appearance = CurrentAppearance;

            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                var textSize = SkinPainter.MeasureText(g, string.IsNullOrEmpty(Text) ? "Xg" : Text,
                    appearance, DeviceDpi);
                // Box (quadratisch = Texthöhe) + Lücke (Drittel) + Text.
                int box = textSize.Height;
                var content = new Size(box + box / 3 + textSize.Width, Math.Max(box, textSize.Height));
                return SkinPainter.InflateByPadding(content, appearance, DeviceDpi);
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }
    }
}
