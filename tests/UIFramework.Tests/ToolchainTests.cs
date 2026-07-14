using System.Drawing;
using System.Windows.Forms;
using Xunit;

namespace UIFramework.Tests
{
    public class ToolchainTests
    {
        // Beweist, dass Pixelprüfungen ohne echtes Fenster funktionieren.
        // Die gesamte Teststrategie hängt daran.
        [Fact]
        public void DrawToBitmap_works_without_a_window_handle()
        {
            using (var control = new Control())
            {
                control.Size = new Size(10, 10);
                control.BackColor = Color.FromArgb(255, 1, 2, 3);
                using (var bitmap = new Bitmap(10, 10))
                {
                    control.DrawToBitmap(bitmap, new Rectangle(0, 0, 10, 10));
                    Assert.Equal(Color.FromArgb(255, 1, 2, 3).ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
                }
            }
        }
    }
}
