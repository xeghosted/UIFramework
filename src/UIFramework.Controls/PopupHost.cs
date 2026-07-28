using System;
using System.Drawing;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    /// <summary>
    /// Der Gast eines PopupHost: misst sich, zeichnet sich und nimmt Eingaben
    /// entgegen — mehr weiß der Host nicht über seinen Inhalt. Combo-Liste und
    /// Monatskalender sind die ersten Gäste; Teilprojekt 4 (Menüs, Ribbons)
    /// erbt den Mechanismus.
    /// </summary>
    public interface IPopupContent
    {
        Size Measure(Graphics g, int dpi, int anchorWidth);
        void Paint(Graphics g, Rectangle bounds, int dpi);
        void HandleMouseMove(Point location);
        void HandleMouseClick(Point location);
        bool HandleKey(Keys key);
        event EventHandler VisualChanged;
        event EventHandler CloseRequested;
    }

    /// <summary>
    /// Das rahmenlose, nicht in der Taskleiste sichtbare Popup-Fenster unter
    /// einem Editor. Aus ComboPopup verallgemeinert: Positionierung, Öffnen,
    /// Schließen bei Deaktivierung — der Inhalt ist ein zeichnender Gast.
    /// Ein Popup, das über sein Elternfenster hinausragt, kann kein
    /// Kind-Control sein — deshalb ein eigenes Form.
    /// </summary>
    public sealed class PopupHost : Form
    {
        private readonly IPopupContent _content;

        public PopupHost(IPopupContent content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            _content = content;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer,
                true);

            _content.VisualChanged += (s, e) => Invalidate();
            _content.CloseRequested += (s, e) => Close();
        }

        public void ShowPopup(IWin32Window owner, Point screenLocation, int minWidth)
        {
            Size size;
            using (var g = CreateGraphics())
                size = _content.Measure(g, DeviceDpi, minWidth);

            Bounds = new Rectangle(screenLocation, size);

            if (owner != null) Show(owner); else Show();
            Activate();
        }

        public void ClosePopup()
        {
            Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            _content.Paint(e.Graphics, ClientRectangle, DeviceDpi);
            base.OnPaint(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            _content.HandleMouseMove(e.Location);
            base.OnMouseMove(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            _content.HandleMouseClick(e.Location);
            base.OnMouseClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_content.HandleKey(e.KeyCode)) e.Handled = true;
            base.OnKeyDown(e);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }
    }
}
