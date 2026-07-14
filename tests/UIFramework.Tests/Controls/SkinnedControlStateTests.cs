using System;
using System.Drawing;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class SkinnedControlStateTests : IDisposable
    {
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
