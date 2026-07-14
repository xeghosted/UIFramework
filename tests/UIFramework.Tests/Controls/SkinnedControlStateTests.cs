using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class SkinnedControlStateTests : IDisposable
    {
        /// <summary>
        /// Wie StubSkin, aber mit einem einstellbaren Eckradius — nötig, um die
        /// vier Eckenquadrate außerhalb des abgerundeten Pfads zu treffen, die
        /// StubSkin (Corners = None) niemals erzeugt.
        /// </summary>
        private sealed class RoundedStubSkin : SkinBase
        {
            public RoundedStubSkin(Color background, int cornerRadius)
            {
                var appearance = new ElementAppearance
                {
                    Background = background,
                    BackgroundGradientEnd = null,
                    BorderColor = Color.Transparent,
                    BorderWidth = 0,
                    Corners = new CornerRadius(cornerRadius),
                    ForeColor = Color.FromArgb(255, 255, 255, 255),
                    Font = new FontSpec("Segoe UI", 9f),
                    Padding = new Padding(4)
                };

                Define(ElementKeys.Panel, ElementState.Normal, appearance);
            }

            public override string Name
            {
                get { return "RoundedStub"; }
            }
        }

        /// <summary>
        /// Ein Elternteil, dessen wirklich gemaltes Erscheinungsbild sich von
        /// seiner BackColor-Eigenschaft unterscheidet. Nötig, um "echte"
        /// Transparenz (Weiterreichen der Hintergrundmalung an den Eltern) von
        /// bloßer Ambient-Vererbung der BackColor zu unterscheiden — ein Kind
        /// ohne explizite BackColor erbt sonst zufällig dieselbe flache Farbe
        /// wie sein Elternteil und ein Test würde den Fehler nicht bemerken.
        /// </summary>
        private sealed class DistinctBackgroundParent : Panel
        {
            public Color PaintedBackground { get; set; }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                using (var brush = new SolidBrush(PaintedBackground))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }
        }

        public SkinnedControlStateTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void A_fresh_control_is_Normal()
        {
            using (var control = new ProbeControl())
            {
                Assert.Equal(ElementState.Normal, control.State);
            }
        }

        [Fact]
        public void Hovering_makes_it_Hovered()
        {
            using (var control = new ProbeControl())
            {
                control.RaiseMouseEnter();

                Assert.Equal(ElementState.Hovered, control.State);
            }
        }

        [Fact]
        public void Leaving_returns_it_to_Normal()
        {
            using (var control = new ProbeControl())
            {
                control.RaiseMouseEnter();
                control.RaiseMouseLeave();

                Assert.Equal(ElementState.Normal, control.State);
            }
        }

        [Fact]
        public void Pressed_outranks_Hovered()
        {
            using (var control = new ProbeControl())
            {
                control.RaiseMouseEnter();
                control.RaiseMouseDown();

                Assert.Equal(ElementState.Pressed, control.State);
            }
        }

        [Fact]
        public void Releasing_the_button_falls_back_to_Hovered()
        {
            using (var control = new ProbeControl())
            {
                control.RaiseMouseEnter();
                control.RaiseMouseDown();
                control.RaiseMouseUp();

                Assert.Equal(ElementState.Hovered, control.State);
            }
        }

        [Fact]
        public void Leaving_while_pressed_clears_the_press()
        {
            using (var control = new ProbeControl())
            {
                control.RaiseMouseEnter();
                control.RaiseMouseDown();
                control.RaiseMouseLeave();

                // Sonst bliebe das Control für immer gedrückt, wenn der Anwender
                // mit gehaltener Taste hinauszieht und dort loslässt.
                Assert.Equal(ElementState.Normal, control.State);
            }
        }

        [Fact]
        public void Disabled_outranks_everything()
        {
            using (var control = new ProbeControl())
            {
                control.RaiseMouseEnter();
                control.RaiseMouseDown();
                control.Enabled = false;

                Assert.Equal(ElementState.Disabled, control.State);
            }
        }

        [Fact]
        public void Selected_outranks_Normal_but_loses_to_Hovered()
        {
            using (var control = new ProbeControl())
            {
                control.SelectedForTest = true;
                Assert.Equal(ElementState.Selected, control.State);

                control.RaiseMouseEnter();
                Assert.Equal(ElementState.Hovered, control.State);
            }
        }

        [Fact]
        public void A_control_registers_itself_and_unregisters_on_dispose()
        {
            var control = new ProbeControl();
            Assert.Equal(1, SkinManager.RegisteredCount);

            control.Dispose();

            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [Fact]
        public void It_paints_the_background_of_the_current_skin()
        {
            SkinManager.Current = new StubSkin(Color.FromArgb(255, 7, 8, 9));

            using (var control = new ProbeControl())
            {
                control.Size = new Size(30, 30);

                using (var bitmap = new Bitmap(30, 30))
                {
                    control.DrawToBitmap(bitmap, new Rectangle(0, 0, 30, 30));

                    Assert.Equal(Color.FromArgb(255, 7, 8, 9).ToArgb(), bitmap.GetPixel(15, 15).ToArgb());
                }
            }
        }

        [Fact]
        public void Corner_pixels_show_the_parent_background_not_default_gray()
        {
            // Radius 6 auf einem 30x30-Control: Pixel (0,0) liegt sicher außerhalb
            // des abgerundeten Pfads, also im Eckenquadrat, das der Painter nie füllt.
            SkinManager.Current = new RoundedStubSkin(Color.FromArgb(255, 7, 8, 9), 6);

            using (var parent = new DistinctBackgroundParent())
            {
                // BackColor bleibt bewusst der Systemstandard (Ambient-Vererbungsfarbe);
                // das wirklich Gemalte (PaintedBackground) ist eine andere Farbe.
                // So kann ein Kind, das nur die BackColor des Elternteils erbt, nicht
                // zufällig denselben Wert liefern wie ein Kind, das echt durchsichtig ist.
                parent.PaintedBackground = Color.FromArgb(255, 10, 20, 30);
                parent.Size = new Size(100, 100);

                using (var control = new ProbeControl())
                {
                    control.Location = new Point(10, 10);
                    control.Size = new Size(30, 30);
                    parent.Controls.Add(control);

                    using (var bitmap = new Bitmap(30, 30))
                    {
                        control.DrawToBitmap(bitmap, new Rectangle(0, 0, 30, 30));

                        Assert.Equal(parent.PaintedBackground.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
                    }
                }
            }
        }

        [Fact]
        public void Switching_the_skin_changes_what_it_paints()
        {
            using (var control = new ProbeControl())
            {
                control.Size = new Size(30, 30);

                SkinManager.Current = new StubSkin(Color.FromArgb(255, 7, 8, 9), "A");
                using (var first = new Bitmap(30, 30))
                {
                    control.DrawToBitmap(first, new Rectangle(0, 0, 30, 30));
                    Assert.Equal(Color.FromArgb(255, 7, 8, 9).ToArgb(), first.GetPixel(15, 15).ToArgb());
                }

                SkinManager.Current = new StubSkin(Color.FromArgb(255, 90, 80, 70), "B");
                using (var second = new Bitmap(30, 30))
                {
                    control.DrawToBitmap(second, new Rectangle(0, 0, 30, 30));
                    Assert.Equal(Color.FromArgb(255, 90, 80, 70).ToArgb(), second.GetPixel(15, 15).ToArgb());
                }
            }
        }
    }
}
