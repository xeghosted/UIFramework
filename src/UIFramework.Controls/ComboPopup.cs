using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Das aufgeklappte Popup eines SkinComboBox. Ein rahmenloses, nicht in der
    /// Taskleiste sichtbares Form statt eines ToolStripDropDown — die Zeilen
    /// zeichnet es sich selbst über SkinPainter, dieselbe Technik wie bei jeder
    /// SkinnedControl-Zelle, nur ohne eigene Control-Instanz pro Zeile.
    /// </summary>
    internal sealed class ComboPopup : Form
    {
        private readonly IList<object> _items;
        private readonly Func<int> _getSelectedIndex;
        private int _hoverIndex = -1;

        public event Action<int> ItemChosen;

        public ComboPopup(IList<object> items, Func<int> getSelectedIndex)
        {
            _items = items;
            _getSelectedIndex = getSelectedIndex;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer,
                true);
        }

        public void ShowPopup(IWin32Window owner, Point screenLocation, int width)
        {
            int rowHeight = MeasuredRowHeight();
            int height = Math.Max(rowHeight, rowHeight * _items.Count);

            Bounds = new Rectangle(screenLocation, new Size(Math.Max(width, 40), height));

            if (owner != null) Show(owner); else Show();
            Activate();
        }

        public void ClosePopup()
        {
            Close();
        }

        private int MeasuredRowHeight()
        {
            var appearance = SkinManager.Current.GetAppearance(ElementKeys.GridCell, ElementState.Normal);
            using (var g = CreateGraphics())
            {
                var size = SkinPainter.MeasureText(g, "Xg", appearance, DeviceDpi);
                return SkinPainter.InflateByPadding(size, appearance, DeviceDpi).Height;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var container = SkinManager.Current.GetAppearance(ElementKeys.ComboBoxList, ElementState.Normal);
            SkinPainter.DrawBackground(e.Graphics, ClientRectangle, container, DeviceDpi);
            SkinPainter.DrawBorder(e.Graphics, ClientRectangle, container, DeviceDpi);

            int rowHeight = MeasuredRowHeight();
            int selectedIndex = _getSelectedIndex();

            for (int i = 0; i < _items.Count; i++)
            {
                ElementState rowState;
                if (i == _hoverIndex) rowState = ElementState.Hovered;
                else if (i == selectedIndex) rowState = ElementState.Selected;
                else rowState = ElementState.Normal;

                var rowAppearance = SkinManager.Current.GetAppearance(ElementKeys.GridCell, rowState);
                var rowBounds = new Rectangle(0, i * rowHeight, ClientSize.Width, rowHeight);

                SkinPainter.DrawBackground(e.Graphics, rowBounds, rowAppearance, DeviceDpi);
                SkinPainter.DrawPaddedText(
                    e.Graphics, _items[i].ToString(), rowBounds, rowAppearance, DeviceDpi, ContentAlignment.MiddleLeft);
            }

            base.OnPaint(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int rowHeight = MeasuredRowHeight();
            int index = rowHeight > 0 ? e.Y / rowHeight : -1;
            if (index < 0 || index >= _items.Count) index = -1;

            if (index != _hoverIndex)
            {
                _hoverIndex = index;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (_hoverIndex >= 0)
            {
                var handler = ItemChosen;
                if (handler != null) handler(_hoverIndex);
            }
            base.OnMouseClick(e);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }
    }
}
