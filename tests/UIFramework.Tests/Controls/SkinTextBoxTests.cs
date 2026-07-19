using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Core.Skinning.Skins;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class SkinTextBoxTests : IDisposable
    {
        private sealed class PerStateTextBoxSkin : SkinBase
        {
            public static readonly Color NormalBorder = Color.FromArgb(255, 10, 20, 30);
            public static readonly Color HoveredBorder = Color.FromArgb(255, 40, 50, 60);
            public static readonly Color SelectedBorder = Color.FromArgb(255, 70, 80, 90);
            public static readonly Color DisabledBorder = Color.FromArgb(255, 100, 110, 120);

            public PerStateTextBoxSkin()
            {
                Define(ElementKeys.TextBox, ElementState.Normal, Appearance(NormalBorder));
                Define(ElementKeys.TextBox, ElementState.Hovered, Appearance(HoveredBorder));
                Define(ElementKeys.TextBox, ElementState.Selected, Appearance(SelectedBorder));
                Define(ElementKeys.TextBox, ElementState.Disabled, Appearance(DisabledBorder));
            }

            public override string Name
            {
                get { return "PerStateTextBox"; }
            }

            private static ElementAppearance Appearance(Color borderColor)
            {
                return new ElementAppearance
                {
                    Background = Color.FromArgb(255, 250, 250, 250),
                    BorderColor = borderColor,
                    BorderWidth = 2,
                    Corners = CornerRadius.None,
                    ForeColor = Color.FromArgb(255, 0, 0, 0),
                    Font = new FontSpec("Segoe UI", 9f),
                    Padding = new Padding(6, 4, 6, 4)
                };
            }
        }

        public SkinTextBoxTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void It_paints_the_normal_border_colour_of_the_current_skin()
        {
            SkinManager.Current = new PerStateTextBoxSkin();

            using (var box = new SkinTextBox())
            {
                box.Size = new Size(80, 24);

                using (var bitmap = new Bitmap(80, 24))
                {
                    box.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 24));

                    Assert.Equal(PerStateTextBoxSkin.NormalBorder.ToArgb(), bitmap.GetPixel(0, 12).ToArgb());
                }
            }
        }

        [Fact]
        public void Hovered_paints_the_skins_hovered_border_colour()
        {
            SkinManager.Current = new PerStateTextBoxSkin();

            using (var box = new ProbeSkinTextBox())
            {
                box.Size = new Size(80, 24);
                box.RaiseMouseEnter();

                using (var bitmap = new Bitmap(80, 24))
                {
                    box.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 24));

                    Assert.Equal(PerStateTextBoxSkin.HoveredBorder.ToArgb(), bitmap.GetPixel(0, 12).ToArgb());
                }
            }
        }

        [Fact]
        public void A_focused_textbox_paints_the_skins_selected_border_colour()
        {
            SkinManager.Current = new PerStateTextBoxSkin();

            using (var box = new ProbeSkinTextBox())
            {
                box.Size = new Size(80, 24);
                box.FocusedOverride = true;

                using (var bitmap = new Bitmap(80, 24))
                {
                    box.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 24));

                    Assert.Equal(PerStateTextBoxSkin.SelectedBorder.ToArgb(), bitmap.GetPixel(0, 12).ToArgb());
                }
            }
        }

        [Fact]
        public void Disabled_paints_the_skins_disabled_border_colour()
        {
            SkinManager.Current = new PerStateTextBoxSkin();

            using (var box = new SkinTextBox())
            {
                box.Size = new Size(80, 24);
                box.Enabled = false;

                using (var bitmap = new Bitmap(80, 24))
                {
                    box.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 24));

                    Assert.Equal(PerStateTextBoxSkin.DisabledBorder.ToArgb(), bitmap.GetPixel(0, 12).ToArgb());
                }
            }
        }

        [Fact]
        public void Text_round_trips_through_the_inner_native_textbox()
        {
            using (var box = new SkinTextBox())
            {
                box.Text = "hello frame4";

                Assert.Equal("hello frame4", box.Text);
                Assert.Equal("hello frame4", box.InnerTextBoxForTests.Text);
            }
        }

        [Fact]
        public void Setting_Text_raises_TextChanged_once()
        {
            using (var box = new SkinTextBox())
            {
                int count = 0;
                box.TextChanged += (s, e) => count++;

                box.Text = "x";

                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void ReadOnly_and_Multiline_proxy_to_the_inner_textbox()
        {
            using (var box = new SkinTextBox())
            {
                box.ReadOnly = true;
                box.Multiline = true;

                Assert.True(box.InnerTextBoxForTests.ReadOnly);
                Assert.True(box.InnerTextBoxForTests.Multiline);
            }
        }

        [Fact]
        public void The_inner_textbox_is_disabled_together_with_the_control()
        {
            using (var box = new SkinTextBox())
            {
                box.Enabled = false;

                Assert.False(box.InnerTextBoxForTests.Enabled);
            }
        }

        [Fact]
        public void The_inner_textbox_is_inset_by_padding_and_border()
        {
            SkinManager.Current = new LightSkin();

            using (var box = new SkinTextBox())
            {
                box.Size = new Size(100, 30);
                var forceHandle = box.Handle;

                var appearance = SkinManager.Current.GetAppearance(ElementKeys.TextBox, ElementState.Normal);
                var expected = UIFramework.Core.Rendering.SkinPainter.GetContentRectangle(
                    box.ClientRectangle, appearance, box.DeviceDpi);

                Assert.Equal(expected, box.InnerTextBoxForTests.Bounds);
            }
        }
    }
}
