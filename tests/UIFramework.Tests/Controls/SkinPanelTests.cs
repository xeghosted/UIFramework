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
        /// <summary>
        /// Anders als StubSkin: definiert eine Erscheinung ausschließlich für
        /// ElementKeys.Panel. StubSkin weist Button, Panel, Label und Focus
        /// dieselbe Instanz zu, sodass ein Test darauf bestünde, egal welcher
        /// dieser Schlüssel von SkinPanel.ElementKey zurückgegeben würde. Mit
        /// diesem Skin fällt ein falscher Schlüssel auf SkinBase.FallbackAppearance
        /// zurück und der Test schlägt sichtbar fehl.
        /// </summary>
        private sealed class PanelOnlySkin : SkinBase
        {
            public static readonly Color PanelColor = Color.FromArgb(255, 44, 55, 66);

            public PanelOnlySkin()
            {
                Define(ElementKeys.Panel, ElementState.Normal, new ElementAppearance
                {
                    Background = PanelColor,
                    BackgroundGradientEnd = null,
                    BorderColor = Color.Transparent,
                    BorderWidth = 0,
                    Corners = CornerRadius.None,
                    ForeColor = Color.FromArgb(255, 255, 255, 255),
                    Font = new FontSpec("Segoe UI", 9f),
                    Padding = new Padding(4)
                });
            }

            public override string Name
            {
                get { return "PanelOnly"; }
            }
        }

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
            // PanelOnlySkin definiert Padding(4) auf allen Seiten, kein Rahmen.
            SkinManager.Current = new PanelOnlySkin();

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
            SkinManager.Current = new PanelOnlySkin();

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
            SkinManager.Current = new PanelOnlySkin();

            using (var panel = new SkinPanel())
            {
                panel.Size = new Size(60, 60);

                using (var bitmap = new Bitmap(60, 60))
                {
                    panel.DrawToBitmap(bitmap, new Rectangle(0, 0, 60, 60));

                    Assert.Equal(PanelOnlySkin.PanelColor.ToArgb(), bitmap.GetPixel(30, 30).ToArgb());
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
