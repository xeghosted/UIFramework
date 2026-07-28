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
    public class SkinComboBoxTests : IDisposable
    {
        public SkinComboBoxTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void It_starts_with_no_selection()
        {
            using (var combo = new SkinComboBox())
            {
                Assert.Equal(-1, combo.SelectedIndex);
                Assert.Null(combo.SelectedItem);
            }
        }

        [Fact]
        public void Adding_items_and_setting_SelectedIndex_updates_SelectedItem()
        {
            using (var combo = new SkinComboBox())
            {
                combo.Items.Add("alpha");
                combo.Items.Add("beta");

                combo.SelectedIndex = 1;

                Assert.Equal("beta", combo.SelectedItem);
            }
        }

        [Fact]
        public void Setting_SelectedItem_looks_up_its_index()
        {
            using (var combo = new SkinComboBox())
            {
                combo.Items.Add("alpha");
                combo.Items.Add("beta");

                combo.SelectedItem = "alpha";

                Assert.Equal(0, combo.SelectedIndex);
            }
        }

        [Fact]
        public void Setting_SelectedIndex_out_of_range_throws()
        {
            using (var combo = new SkinComboBox())
            {
                combo.Items.Add("alpha");

                Assert.Throws<ArgumentOutOfRangeException>(() => combo.SelectedIndex = 5);
            }
        }

        [Fact]
        public void Changing_SelectedIndex_raises_SelectedIndexChanged_once()
        {
            using (var combo = new SkinComboBox())
            {
                combo.Items.Add("alpha");
                combo.Items.Add("beta");

                int count = 0;
                combo.SelectedIndexChanged += (s, e) => count++;

                combo.SelectedIndex = 1;

                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void Setting_SelectedIndex_to_its_current_value_does_not_raise_the_event()
        {
            using (var combo = new SkinComboBox())
            {
                combo.Items.Add("alpha");
                combo.SelectedIndex = 0;

                int count = 0;
                combo.SelectedIndexChanged += (s, e) => count++;

                combo.SelectedIndex = 0;

                Assert.Equal(0, count);
            }
        }

        [Fact]
        public void It_paints_the_comboboxs_background_of_the_current_skin()
        {
            SkinManager.Current = new StubSkinWithComboBox(Color.FromArgb(255, 12, 34, 56));

            using (var combo = new SkinComboBox())
            {
                combo.Size = new Size(100, 24);

                using (var bitmap = new Bitmap(100, 24))
                {
                    combo.DrawToBitmap(bitmap, new Rectangle(0, 0, 100, 24));

                    Assert.Equal(Color.FromArgb(255, 12, 34, 56).ToArgb(), bitmap.GetPixel(50, 12).ToArgb());
                }
            }
        }

        [Fact]
        public void Clicking_the_arrow_while_the_popup_closes_underneath_does_not_reopen_it()
        {
            using (var combo = new SkinComboBox())
            {
                combo.Items.Add("alpha");
                combo.Size = new Size(120, 24);
                combo.CreateControl();

                combo.ClickButtonForTests(0);                  // öffnet
                Assert.True(combo.IsPopupOpenForTests);

                combo.PressButtonForTests(0);                  // MouseDown auf dem Pfeil
                combo.RaisePopupDeactivateForTests();          // Popup verliert die Aktivierung ...
                Application.DoEvents();                        // ... aufgeschobenes Close (Task-12) läuft durch

                combo.ReleaseButtonForTests(0);                // MouseUp: Toggle() feuert

                Assert.False(combo.IsPopupOpenForTests);       // darf NICHT wieder offen sein
            }
        }

        private sealed class StubSkinWithComboBox : SkinBase
        {
            private readonly Color _background;

            public StubSkinWithComboBox(Color background)
            {
                _background = background;

                Define(ElementKeys.ComboBox, ElementState.Normal, new ElementAppearance
                {
                    Background = background,
                    BorderColor = Color.Transparent,
                    BorderWidth = 0,
                    Corners = CornerRadius.None,
                    ForeColor = Color.FromArgb(255, 255, 255, 255),
                    Font = new FontSpec("Segoe UI", 9f),
                    Padding = new Padding(8, 4, 8, 4)
                });
            }

            public override string Name
            {
                get { return "StubComboBox"; }
            }
        }
    }
}
