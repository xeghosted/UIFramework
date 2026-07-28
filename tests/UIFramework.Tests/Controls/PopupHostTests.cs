using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class PopupHostTests : IDisposable
    {
        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        /// <summary>Minimaler Gast — nur so viel, wie PopupHost zum Anzeigen braucht.</summary>
        private sealed class StubPopupContent : IPopupContent
        {
            public Size Measure(Graphics g, int dpi, int anchorWidth)
            {
                return new Size(anchorWidth, 40);
            }

            public void Paint(Graphics g, Rectangle bounds, int dpi)
            {
            }

            public void HandleMouseMove(Point location)
            {
            }

            public void HandleMouseClick(Point location)
            {
            }

            public bool HandleKey(Keys key)
            {
                return false;
            }

            public event EventHandler VisualChanged { add { } remove { } }
            public event EventHandler CloseRequested { add { } remove { } }
        }

        [Fact]
        public void Deactivating_defers_the_close_past_the_activation_handshake()
        {
            using (var popup = new PopupHost(new StubPopupContent()))
            {
                popup.ShowPopup(null, new Point(0, 0), 100);

                popup.RaiseDeactivateForTests();

                // Synchrones Close() hier würde die WM_ACTIVATE-Handshake-Sequenz
                // zerreißen (Task-12-Befund F1) — direkt nach OnDeactivate muss
                // das Popup darum noch leben.
                Assert.False(popup.IsDisposed);

                Application.DoEvents();

                Assert.True(popup.IsDisposed);
            }
        }

        [Fact]
        public void Deactivating_twice_does_not_double_close()
        {
            var popup = new PopupHost(new StubPopupContent());
            popup.ShowPopup(null, new Point(0, 0), 100);

            popup.RaiseDeactivateForTests();
            popup.RaiseDeactivateForTests();

            // Beide aufgeschobenen Schließ-Aufrufe laufen hier ab; der zweite
            // darf nicht auf ein bereits entsorgtes Fenster losgehen.
            var ex = Record.Exception(() => Application.DoEvents());

            Assert.Null(ex);
            Assert.True(popup.IsDisposed);
        }
    }
}
