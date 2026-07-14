using System;
using System.Drawing;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class SkinButtonTests : IDisposable
    {
        public SkinButtonTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void It_paints_the_button_background_of_the_current_skin()
        {
            SkinManager.Current = new StubSkin(Color.FromArgb(255, 11, 22, 33));

            using (var button = new SkinButton())
            {
                button.Size = new Size(80, 30);
                button.Text = "";

                using (var bitmap = new Bitmap(80, 30))
                {
                    button.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 30));

                    Assert.Equal(Color.FromArgb(255, 11, 22, 33).ToArgb(), bitmap.GetPixel(40, 15).ToArgb());
                }
            }
        }

        [Fact]
        public void Text_actually_lands_on_the_pixels()
        {
            SkinManager.Current = new StubSkin(Color.FromArgb(255, 0, 0, 0));

            using (var button = new SkinButton())
            {
                button.Size = new Size(120, 30);
                button.Text = "MMMMMMMM";

                using (var bitmap = new Bitmap(120, 30))
                {
                    button.DrawToBitmap(bitmap, new Rectangle(0, 0, 120, 30));

                    // StubSkin zeichnet weißen Text auf schwarzem Grund: irgendwo
                    // muss ein nicht-schwarzes Pixel sein.
                    bool foundText = false;
                    for (int x = 0; x < 120 && !foundText; x++)
                    {
                        for (int y = 0; y < 30 && !foundText; y++)
                        {
                            if (bitmap.GetPixel(x, y).ToArgb() != Color.FromArgb(255, 0, 0, 0).ToArgb())
                                foundText = true;
                        }
                    }

                    Assert.True(foundText, "Es wurde kein Text gezeichnet.");
                }
            }
        }

        [Fact]
        public void The_default_alignment_is_centred()
        {
            using (var button = new SkinButton())
            {
                Assert.Equal(ContentAlignment.MiddleCenter, button.TextAlignment);
            }
        }

        [Fact]
        public void Changing_the_alignment_repaints()
        {
            using (var button = new SkinButton())
            {
                button.TextAlignment = ContentAlignment.MiddleLeft;

                Assert.Equal(ContentAlignment.MiddleLeft, button.TextAlignment);
            }
        }

        [Fact]
        public void Clicking_raises_Click_once()
        {
            using (var button = new SkinButton())
            {
                int clicks = 0;
                button.Click += (s, e) => clicks++;

                button.PerformClick();

                Assert.Equal(1, clicks);
            }
        }

        [Fact]
        public void A_disabled_button_does_not_click()
        {
            using (var button = new SkinButton())
            {
                button.Enabled = false;
                int clicks = 0;
                button.Click += (s, e) => clicks++;

                button.PerformClick();

                Assert.Equal(0, clicks);
            }
        }
    }
}
