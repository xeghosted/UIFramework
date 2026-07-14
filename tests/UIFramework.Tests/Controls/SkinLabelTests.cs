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
        /// <summary>
        /// Anders als StubSkin: definiert eine Erscheinung ausschließlich für
        /// ElementKeys.Label. StubSkin weist Button, Panel, Label und Focus
        /// dieselbe Instanz zu, sodass ein Test darauf bestünde, egal welcher
        /// dieser Schlüssel von SkinLabel.ElementKey zurückgegeben würde. Mit
        /// diesem Skin fällt ein falscher Schlüssel auf SkinBase.FallbackAppearance
        /// zurück (Padding 4, graue Hintergrundfarbe) und der Test schlägt
        /// sichtbar fehl, weil hier bewusst ein abweichendes Padding (6) und eine
        /// unverwechselbare Hintergrundfarbe verwendet werden.
        /// </summary>
        private sealed class LabelOnlySkin : SkinBase
        {
            public static readonly Color LabelColor = Color.FromArgb(255, 12, 34, 56);

            public LabelOnlySkin()
            {
                Define(ElementKeys.Label, ElementState.Normal, new ElementAppearance
                {
                    Background = LabelColor,
                    BackgroundGradientEnd = null,
                    BorderColor = Color.Transparent,
                    BorderWidth = 0,
                    Corners = CornerRadius.None,
                    ForeColor = Color.FromArgb(255, 255, 255, 255),
                    Font = new FontSpec("Segoe UI", 9f),
                    Padding = new Padding(6)
                });
            }

            public override string Name
            {
                get { return "LabelOnly"; }
            }
        }

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
            // LabelOnlySkin: Padding(6) auf allen Seiten.
            SkinManager.Current = new LabelOnlySkin();

            using (var label = new SkinLabel())
            {
                label.Text = "X";
                var preferred = label.GetPreferredSize(Size.Empty);

                using (var bitmap = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(bitmap))
                {
                    var appearance = SkinManager.Current.GetAppearance(ElementKeys.Label, ElementState.Normal);
                    var textSize = UIFramework.Core.Rendering.SkinPainter.MeasureText(g, "X", appearance, label.DeviceDpi);

                    Assert.Equal(textSize.Width + 12, preferred.Width);
                    Assert.Equal(textSize.Height + 12, preferred.Height);
                }
            }
        }

        [Fact]
        public void It_paints_the_label_background_of_the_current_skin()
        {
            SkinManager.Current = new LabelOnlySkin();

            using (var label = new SkinLabel())
            {
                label.AutoSize = false;
                label.Size = new Size(80, 24);
                label.Text = "";

                using (var bitmap = new Bitmap(80, 24))
                {
                    label.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 24));

                    Assert.Equal(LabelOnlySkin.LabelColor.ToArgb(), bitmap.GetPixel(40, 12).ToArgb());
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
