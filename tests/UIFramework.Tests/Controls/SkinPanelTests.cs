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
    public class SkinPanelTests : IDisposable
    {
        public SkinPanelTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void The_display_rectangle_is_inset_by_the_skin_padding()
        {
            // StubSkin definiert Padding(4) auf allen Seiten, kein Rahmen.
            SkinManager.Current = new StubSkin(Color.FromArgb(255, 5, 5, 5));

            using (var panel = new SkinPanel())
            {
                panel.Size = new Size(100, 100);

                var display = panel.DisplayRectangle;

                Assert.Equal(4, display.Left);
                Assert.Equal(4, display.Top);
                Assert.Equal(92, display.Width);
                Assert.Equal(92, display.Height);
            }
        }

        [Fact]
        public void A_docked_child_lands_inside_the_padding()
        {
            SkinManager.Current = new StubSkin(Color.FromArgb(255, 5, 5, 5));

            using (var panel = new SkinPanel())
            {
                panel.Size = new Size(100, 100);

                var child = new Control { Dock = DockStyle.Fill };
                panel.Controls.Add(child);
                panel.PerformLayout();

                Assert.Equal(4, child.Left);
                Assert.Equal(4, child.Top);
                Assert.Equal(92, child.Width);
            }
        }

        [Fact]
        public void It_paints_the_panel_background_of_the_current_skin()
        {
            SkinManager.Current = new StubSkin(Color.FromArgb(255, 44, 55, 66));

            using (var panel = new SkinPanel())
            {
                panel.Size = new Size(60, 60);

                using (var bitmap = new Bitmap(60, 60))
                {
                    panel.DrawToBitmap(bitmap, new Rectangle(0, 0, 60, 60));

                    Assert.Equal(Color.FromArgb(255, 44, 55, 66).ToArgb(), bitmap.GetPixel(30, 30).ToArgb());
                }
            }
        }

        [Fact]
        public void A_panel_is_not_a_tab_stop()
        {
            using (var panel = new SkinPanel())
            {
                // Ein Container ist kein Bedienelement.
                Assert.False(panel.TabStop);
            }
        }
    }
}
