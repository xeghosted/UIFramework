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

            // Close() HIER synchron aufzurufen zerreißt den WM_ACTIVATE-Handshake:
            // Windows aktiviert danach ein "falsches" Fenster (beobachtet: die
            // Besitzerform statt der tatsächlich angeklickten) und der auslösende
            // Klick geht verloren. Darum erst NACH dem Handshake schließen —
            // BeginInvoke braucht ein lebendes Handle.
            if (IsHandleCreated)
                BeginInvoke((MethodInvoker)DeferredClose);
            else
                DeferredClose();
        }

        private void DeferredClose()
        {
            // Zwischen dem Aufschieben und seiner Ausführung kann das Popup
            // längst anderweitig geschlossen/entsorgt worden sein (CloseRequested,
            // ClosePopup(), oder ein zweiter Deaktivierungs-Aufschub) — dann nicht
            // nochmal zugreifen.
            if (IsDisposed || Disposing) return;
            Close();
        }

        // ---- Nur für Tests --------------------------------------------------

        internal void RaiseDeactivateForTests()
        {
            OnDeactivate(EventArgs.Empty);
        }
    }
}
