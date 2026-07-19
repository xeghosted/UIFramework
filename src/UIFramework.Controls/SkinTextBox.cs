using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Einzeilige (optional mehrzeilige) Texteingabe. Zeichnet Rahmen und
    /// Hintergrund wie jedes SkinnedControl, überlässt Caret/Selektion/IME aber
    /// einem eingebetteten nativen TextBox statt sie neu zu bauen — dessen
    /// Farben werden bei jedem Zeichnen mit dem aktiven Skin synchronisiert.
    ///
    /// Enthält bewusst keinen einzigen Farbwert — alles Sichtbare kommt aus dem Skin.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("TextChanged")]
    public class SkinTextBox : SkinnedControl
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        private const int EM_SETCUEBANNER = 0x1501;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        private readonly TextBox _inner = new TextBox();
        private string _placeholderText = "";

        public SkinTextBox()
        {
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            Size = new Size(120, 24);

            _inner.BorderStyle = BorderStyle.None;
            _inner.GotFocus += (s, e) => Invalidate();
            _inner.LostFocus += (s, e) => Invalidate();
            _inner.MouseEnter += (s, e) => OnMouseEnter(EventArgs.Empty);
            _inner.MouseLeave += (s, e) => OnMouseLeave(EventArgs.Empty);
            _inner.TextChanged += (s, e) => OnTextChanged(EventArgs.Empty);
            _inner.HandleCreated += (s, e) =>
            {
                ApplyPlaceholder();

                // Das native Fenster existiert jetzt erst — nur ab hier lässt sich
                // die per SetBoundsCore erzwungene Einzeilen-Höhe per SetWindowPos
                // überschreiben (siehe LayoutInner). Ein bereits vor der
                // Handle-Erzeugung gelaufener Layout-Durchlauf hätte diese
                // Korrektur sonst verpasst.
                LayoutInner(CurrentAppearance);
            };

            Controls.Add(_inner);
        }

        protected override string ElementKey
        {
            get { return ElementKeys.TextBox; }
        }

        protected override bool IsSelected
        {
            get { return _inner.Focused; }
        }

        protected override bool ShowFocusRing
        {
            get { return false; }
        }

        /// <summary>Nur für Tests: pixelgenaue Prüfung der inneren Textbox
        /// ohne die Sichtbarkeit über DrawToBitmap zu erzwingen (deren Fläche
        /// wird vom nativen Kind-Fenster verdeckt und ist so nicht prüfbar).</summary>
        internal TextBox InnerTextBoxForTests
        {
            get { return _inner; }
        }

        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get { return _inner.Text; }
            set { _inner.Text = value; }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get { return _inner.ReadOnly; }
            set { _inner.ReadOnly = value; }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool Multiline
        {
            get { return _inner.Multiline; }
            set { _inner.Multiline = value; }
        }

        [Category("Appearance")]
        [DefaultValue("")]
        public string PlaceholderText
        {
            get { return _placeholderText; }
            set
            {
                string next = value ?? "";
                if (_placeholderText == next) return;
                _placeholderText = next;
                ApplyPlaceholder();
            }
        }

        private void ApplyPlaceholder()
        {
            if (_inner.IsHandleCreated)
                SendMessage(_inner.Handle, EM_SETCUEBANNER, IntPtr.Zero, _placeholderText);
        }

        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            _inner.BackColor = appearance.Background;
            _inner.ForeColor = appearance.ForeColor;
            _inner.Font = ResourceCache.Shared.GetFont(appearance.Font, DeviceDpi);

            LayoutInner(appearance);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            LayoutInner(CurrentAppearance);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            LayoutInner(CurrentAppearance);
        }

        private void LayoutInner(ElementAppearance appearance)
        {
            var content = SkinPainter.GetContentRectangle(ClientRectangle, appearance, DeviceDpi);
            _inner.SetBounds(content.X, content.Y, content.Width, content.Height);

            // Ein einzeiliges natives TextBox zwingt seine Höhe in SetBoundsCore
            // immer auf PreferredHeight (dokumentiertes WinForms-Verhalten,
            // Multiline == false) — SetBounds allein reicht darum nicht, um es in
            // den Inhaltsbereich einzupassen. SetWindowPos setzt die native
            // Fenstergröße direkt und umgeht diese Zwangsanpassung; WinForms
            // übernimmt die neue Größe anschließend selbst über WM_WINDOWPOSCHANGED
            // in sein eigenes Bounds-Feld, ohne SetBoundsCore erneut zu durchlaufen.
            if (_inner.IsHandleCreated && _inner.Height != content.Height)
            {
                SetWindowPos(
                    _inner.Handle, IntPtr.Zero, content.X, content.Y, content.Width, content.Height,
                    SWP_NOZORDER | SWP_NOACTIVATE);
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            _inner.Enabled = Enabled;
            base.OnEnabledChanged(e);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var appearance = CurrentAppearance;

            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                var textSize = SkinPainter.MeasureText(g, "Xg", appearance, DeviceDpi);
                return SkinPainter.InflateByPadding(textSize, appearance, DeviceDpi);
            }
        }
    }
}
