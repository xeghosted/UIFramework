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
    public class SkinButtonTests : IDisposable
    {
        /// <summary>
        /// Anders als StubSkin: pro Zustand eine andere Farbe. StubSkin definiert
        /// für jedes Element nur Normal, sodass GetAppearance über die
        /// Rückfallkette (state → Normal) für jeden Zustand dieselbe Farbe liefert —
        /// ein Test darauf würde bestehen, egal welcher Zustand aufgelöst wurde.
        /// Dieser Skin unterscheidet sich pro Zustand, damit ein falsch
        /// aufgelöster Zustand eine sichtbar falsche Farbe ergibt.
        /// </summary>
        private sealed class PerStateButtonSkin : SkinBase
        {
            public static readonly Color NormalColor = Color.FromArgb(255, 10, 20, 30);
            public static readonly Color HoveredColor = Color.FromArgb(255, 40, 50, 60);
            public static readonly Color PressedColor = Color.FromArgb(255, 70, 80, 90);
            public static readonly Color DisabledColor = Color.FromArgb(255, 100, 110, 120);

            public PerStateButtonSkin()
            {
                Define(ElementKeys.Button, ElementState.Normal, Appearance(NormalColor));
                Define(ElementKeys.Button, ElementState.Hovered, Appearance(HoveredColor));
                Define(ElementKeys.Button, ElementState.Pressed, Appearance(PressedColor));
                Define(ElementKeys.Button, ElementState.Disabled, Appearance(DisabledColor));
            }

            public override string Name
            {
                get { return "PerState"; }
            }

            private static ElementAppearance Appearance(Color background)
            {
                return new ElementAppearance
                {
                    Background = background,
                    BackgroundGradientEnd = null,
                    BorderColor = Color.Transparent,
                    BorderWidth = 0,
                    Corners = CornerRadius.None,
                    ForeColor = Color.FromArgb(255, 255, 255, 255),
                    Font = new FontSpec("Segoe UI", 9f),
                    Padding = new Padding(4)
                };
            }
        }

        /// <summary>
        /// Zählt Invalidate-Aufrufe über das echte Invalidated-Ereignis von
        /// Control — dafür braucht das Control ein Fensterhandle, sonst verwirft
        /// Control.Invalidate() den Aufruf stillschweigend, ohne das Ereignis
        /// auszulösen.
        /// </summary>
        private sealed class InvalidationCountingButton : SkinButton
        {
            public int InvalidationCount { get; private set; }

            protected override void OnInvalidated(InvalidateEventArgs e)
            {
                InvalidationCount++;
                base.OnInvalidated(e);
            }
        }

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
            using (var button = new InvalidationCountingButton())
            {
                // Handle erzwingen: ohne Fensterhandle verwirft Control.Invalidate()
                // den Aufruf, bevor OnInvalidated feuert (siehe InvalidationCountingButton).
                var forceHandle = button.Handle;
                int before = button.InvalidationCount;

                button.TextAlignment = ContentAlignment.MiddleLeft;

                Assert.True(button.InvalidationCount > before,
                    "Das Setzen von TextAlignment hat kein Invalidate ausgelöst.");
            }
        }

        [Fact]
        public void Hovered_paints_the_skins_hovered_color()
        {
            SkinManager.Current = new PerStateButtonSkin();

            using (var button = new ProbeSkinButton())
            {
                button.Size = new Size(80, 30);
                button.Text = "";
                button.RaiseMouseEnter();

                using (var bitmap = new Bitmap(80, 30))
                {
                    button.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 30));

                    Assert.Equal(PerStateButtonSkin.HoveredColor.ToArgb(), bitmap.GetPixel(40, 15).ToArgb());
                }
            }
        }

        [Fact]
        public void Pressed_paints_the_skins_pressed_color()
        {
            SkinManager.Current = new PerStateButtonSkin();

            using (var button = new ProbeSkinButton())
            {
                button.Size = new Size(80, 30);
                button.Text = "";
                button.RaiseMouseEnter();
                button.RaiseMouseDown();

                using (var bitmap = new Bitmap(80, 30))
                {
                    button.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 30));

                    Assert.Equal(PerStateButtonSkin.PressedColor.ToArgb(), bitmap.GetPixel(40, 15).ToArgb());
                }
            }
        }

        [Fact]
        public void Disabled_paints_the_skins_disabled_color()
        {
            SkinManager.Current = new PerStateButtonSkin();

            using (var button = new ProbeSkinButton())
            {
                button.Size = new Size(80, 30);
                button.Text = "";
                button.Enabled = false;

                using (var bitmap = new Bitmap(80, 30))
                {
                    button.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 30));

                    Assert.Equal(PerStateButtonSkin.DisabledColor.ToArgb(), bitmap.GetPixel(40, 15).ToArgb());
                }
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
