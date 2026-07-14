using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Core.Skinning.Skins;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class SkinnedFormTests : IDisposable
    {
        public SkinnedFormTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void A_form_registers_itself_with_the_skin_manager()
        {
            using (new SkinnedForm())
            {
                Assert.Equal(1, SkinManager.RegisteredCount);
            }
        }

        [Fact]
        public void A_disposed_form_is_no_longer_registered()
        {
            var form = new SkinnedForm();
            form.Dispose();

            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [Fact]
        public void Without_a_handle_nothing_is_pushed_to_the_window_manager()
        {
            using (var form = new SkinnedForm())
            {
                // Kein Handle erzwungen: es gibt kein Fenster zum Einfärben.
                Assert.Equal(0, form.CaptionApplyCount);
            }
        }

        [Fact]
        public void Creating_the_handle_applies_the_caption_once()
        {
            using (var form = new SkinnedForm())
            {
                var unused = form.Handle;

                Assert.Equal(1, form.CaptionApplyCount);
            }
        }

        [Fact]
        public void Repainting_without_a_skin_change_does_not_touch_the_window_manager_again()
        {
            using (var form = new SkinnedForm())
            {
                var unused = form.Handle;
                int afterHandle = form.CaptionApplyCount;

                form.Invalidate();
                form.Invalidate();
                form.Invalidate();

                // Der Merker muss greifen: ohne ihn ginge bei jedem Repaint
                // ein Schwung P/Invoke-Aufrufe raus.
                Assert.Equal(afterHandle, form.CaptionApplyCount);
            }
        }

        [Fact]
        public void Switching_the_skin_applies_the_new_caption()
        {
            using (var form = new SkinnedForm())
            {
                var unused = form.Handle;
                int afterHandle = form.CaptionApplyCount;

                SkinManager.Current = new DarkSkin();

                Assert.Equal(afterHandle + 1, form.CaptionApplyCount);
            }
        }

        [Fact]
        public void A_dark_caption_is_detected_from_the_skin_colours_alone()
        {
            // Heller Text auf dunkler Leiste heißt: dunkle Leiste.
            var dark = new ElementAppearance
            {
                Background = Color.FromArgb(255, 20, 20, 20),
                ForeColor = Color.FromArgb(255, 240, 240, 240)
            };

            var light = new ElementAppearance
            {
                Background = Color.FromArgb(255, 240, 240, 240),
                ForeColor = Color.FromArgb(255, 20, 20, 20)
            };

            Assert.True(SkinnedForm.IsDarkCaption(dark));
            Assert.False(SkinnedForm.IsDarkCaption(light));
        }

        [Fact]
        public void The_built_in_skins_are_classified_the_way_a_human_would()
        {
            var light = new LightSkin().GetAppearance(ElementKeys.Window, ElementState.Normal);
            var dark = new DarkSkin().GetAppearance(ElementKeys.Window, ElementState.Normal);

            Assert.False(SkinnedForm.IsDarkCaption(light));
            Assert.True(SkinnedForm.IsDarkCaption(dark));
        }
    }
}
