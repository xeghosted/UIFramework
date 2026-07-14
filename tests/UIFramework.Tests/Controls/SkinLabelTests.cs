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
    public class SkinLabelTests : IDisposable
    {
        public SkinLabelTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void Longer_text_prefers_a_wider_size()
        {
            using (var label = new SkinLabel())
            {
                label.Text = "kurz";
                var narrow = label.GetPreferredSize(Size.Empty);

                label.Text = "erheblich laengerer Text";
                var wide = label.GetPreferredSize(Size.Empty);

                Assert.True(wide.Width > narrow.Width);
            }
        }

        [Fact]
        public void Empty_text_still_prefers_a_positive_height()
        {
            using (var label = new SkinLabel())
            {
                label.Text = "";

                // Sonst kollabiert ein leeres Label auf null Höhe und das Layout springt,
                // sobald Text hineinkommt.
                Assert.True(label.GetPreferredSize(Size.Empty).Height > 0);
            }
        }

        [Fact]
        public void The_preferred_size_includes_the_skin_padding()
        {
            // StubSkin: Padding(4) auf allen Seiten.
            SkinManager.Current = new StubSkin(Color.FromArgb(255, 5, 5, 5));

            using (var label = new SkinLabel())
            {
                label.Text = "X";
                var preferred = label.GetPreferredSize(Size.Empty);

                using (var bitmap = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(bitmap))
                {
                    var appearance = SkinManager.Current.GetAppearance(ElementKeys.Label, ElementState.Normal);
                    var textSize = UIFramework.Core.Rendering.SkinPainter.MeasureText(g, "X", appearance, label.DeviceDpi);

                    Assert.Equal(textSize.Width + 8, preferred.Width);
                    Assert.Equal(textSize.Height + 8, preferred.Height);
                }
            }
        }

        [Fact]
        public void It_paints_the_label_background_of_the_current_skin()
        {
            SkinManager.Current = new StubSkin(Color.FromArgb(255, 60, 70, 80));

            using (var label = new SkinLabel())
            {
                label.AutoSize = false;
                label.Size = new Size(80, 24);
                label.Text = "";

                using (var bitmap = new Bitmap(80, 24))
                {
                    label.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 24));

                    Assert.Equal(Color.FromArgb(255, 60, 70, 80).ToArgb(), bitmap.GetPixel(40, 12).ToArgb());
                }
            }
        }

        [Fact]
        public void The_default_alignment_is_middle_left()
        {
            using (var label = new SkinLabel())
            {
                Assert.Equal(ContentAlignment.MiddleLeft, label.TextAlignment);
            }
        }

        [Fact]
        public void A_label_is_not_a_tab_stop()
        {
            using (var label = new SkinLabel())
            {
                Assert.False(label.TabStop);
            }
        }
    }
}
