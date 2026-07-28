using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Dropdown mit fester Liste. Zeichnet den geschlossenen Zustand selbst
    /// (Text + Pfeil-Glyph), das aufgeklappte Popup ist ein eigenes, rahmenloses
    /// Fenster (ComboPopup) statt ein zweites SkinnedControl — ein Popup, das
    /// über die Grenzen seines Elternfensters hinausragt, kann kein Kind-Control
    /// sein.
    ///
    /// Enthält bewusst keinen einzigen Farbwert — alles Sichtbare kommt aus dem Skin.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SelectedIndexChanged")]
    public class SkinComboBox : SkinnedControl
    {
        private readonly List<object> _items = new List<object>();
        private int _selectedIndex = -1;
        private PopupHost _popup;

        public SkinComboBox()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Size = new Size(120, 24);
        }

        protected override string ElementKey
        {
            get { return ElementKeys.ComboBox; }
        }

        protected override bool IsSelected
        {
            get { return _popup != null; }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public IList<object> Items
        {
            get { return _items; }
        }

        [Browsable(false)]
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (value < -1 || value >= _items.Count)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (_selectedIndex == value) return;

                _selectedIndex = value;
                Invalidate();
                OnSelectedIndexChanged(EventArgs.Empty);
            }
        }

        [Browsable(false)]
        public object SelectedItem
        {
            get { return _selectedIndex >= 0 ? _items[_selectedIndex] : null; }
            set { SelectedIndex = _items.IndexOf(value); }
        }

        public event EventHandler SelectedIndexChanged;

        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            var handler = SelectedIndexChanged;
            if (handler != null) handler(this, e);
        }

        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            var content = SkinPainter.GetContentRectangle(ClientRectangle, appearance, DeviceDpi);
            int arrowWidth = content.Height;
            var arrowRect = new Rectangle(content.Right - arrowWidth, content.Top, arrowWidth, content.Height);
            var labelRect = new Rectangle(content.Left, content.Top, content.Width - arrowWidth, content.Height);

            string text = SelectedItem != null ? SelectedItem.ToString() : "";
            if (!string.IsNullOrEmpty(text))
                SkinPainter.DrawText(g, text, labelRect, appearance, DeviceDpi, ContentAlignment.MiddleLeft);

            DrawArrowGlyph(g, arrowRect, appearance);
        }

        private static void DrawArrowGlyph(Graphics g, Rectangle bounds, ElementAppearance appearance)
        {
            int size = Math.Min(bounds.Width, bounds.Height) / 3;
            if (size < 2) return;

            int cx = bounds.Left + bounds.Width / 2;
            int cy = bounds.Top + bounds.Height / 2;

            Point[] triangle =
            {
                new Point(cx - size, cy - size / 2),
                new Point(cx + size, cy - size / 2),
                new Point(cx, cy + size / 2)
            };

            g.FillPolygon(ResourceCache.Shared.GetBrush(appearance.ForeColor), triangle);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Enabled)
            {
                if (CanFocus) Focus();
                Toggle();
            }
            base.OnMouseDown(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Down || keyData == Keys.Up || keyData == Keys.Space ||
                keyData == Keys.Enter || keyData == Keys.Escape)
                return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && _popup == null)
            {
                Open();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape && _popup != null)
            {
                Close();
                e.Handled = true;
            }
            else if ((e.KeyCode == Keys.Up || e.KeyCode == Keys.Down) && _items.Count > 0)
            {
                int next = SelectedIndex + (e.KeyCode == Keys.Down ? 1 : -1);
                if (next >= 0 && next < _items.Count) SelectedIndex = next;
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        private void Toggle()
        {
            if (_popup == null) Open(); else Close();
        }

        private void Open()
        {
            if (_items.Count == 0) return;

            var list = new ListContent(_items, () => _selectedIndex);
            list.ItemChosen += index => { SelectedIndex = index; };

            _popup = new PopupHost(list);
            _popup.FormClosed += (s, e) =>
            {
                _popup = null;
                Invalidate();
            };

            var screenLocation = Parent != null
                ? Parent.PointToScreen(new Point(Left, Bottom))
                : PointToScreen(new Point(0, Height));

            _popup.ShowPopup(FindForm(), screenLocation, Width);
            Invalidate();
        }

        private void Close()
        {
            if (_popup == null) return;

            var popup = _popup;
            _popup = null;
            popup.ClosePopup();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Close();
            base.Dispose(disposing);
        }
    }
}
