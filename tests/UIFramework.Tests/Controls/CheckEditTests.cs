using System;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class CheckEditTests : IDisposable
    {
        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void Toggling_Checked_raises_the_event_once_per_change()
        {
            using (var check = new CheckEdit())
            {
                int raises = 0;
                check.CheckedChanged += (s, e) => raises++;

                check.Checked = true;
                check.Checked = true;    // kein zweites Ereignis
                check.Checked = false;

                Assert.Equal(2, raises);
            }
        }

        [Fact]
        public void The_space_key_toggles()
        {
            using (var check = new CheckEdit())
            {
                check.PerformKey(Keys.Space);
                Assert.True(check.Checked);

                check.PerformKey(Keys.Space);
                Assert.False(check.Checked);
            }
        }

        [Fact]
        public void Other_keys_do_not_toggle()
        {
            using (var check = new CheckEdit())
            {
                check.PerformKey(Keys.Enter);
                Assert.False(check.Checked);
            }
        }

        [Fact]
        public void Losing_focus_confirms_exactly_once()
        {
            // Befund F1: CheckEdit hat keine LostFocus-Verdrahtung wie
            // ButtonEditBase._inner — das eigene OnLostFocus muss bestätigen.
            using (var check = new CheckEdit())
            {
                int confirmed = 0;
                check.EditConfirmed += (s, e) => confirmed++;

                check.RaiseLostFocusForTests();

                Assert.Equal(1, confirmed);
            }
        }

        [Fact]
        public void A_disabled_check_edit_ignores_space_enter_and_escape_alike()
        {
            // Befund F4: vorher prüfte nur Space auf Enabled, Enter/Escape
            // feuerten auch an einem deaktivierten Editor.
            using (var check = new CheckEdit { Enabled = false })
            {
                int confirmed = 0, cancelled = 0;
                check.EditConfirmed += (s, e) => confirmed++;
                check.EditCancelled += (s, e) => cancelled++;

                check.PerformKey(Keys.Space);
                check.PerformKey(Keys.Enter);
                check.PerformKey(Keys.Escape);

                Assert.False(check.Checked);
                Assert.Equal(0, confirmed);
                Assert.Equal(0, cancelled);
            }
        }

        [Fact]
        public void The_preferred_size_grows_with_the_text()
        {
            using (var shortOne = new CheckEdit { Text = "Ja" })
            using (var longOne = new CheckEdit { Text = "Deutlich längerer Beschriftungstext" })
            {
                var small = shortOne.GetPreferredSize(System.Drawing.Size.Empty);
                var large = longOne.GetPreferredSize(System.Drawing.Size.Empty);

                Assert.True(large.Width > small.Width);
                Assert.True(small.Width > 0 && small.Height > 0);
            }
        }

        [Fact]
        public void AutoSize_grows_the_control_when_text_is_assigned()
        {
            // AutoSize=true ist der Standard — die Größe passt sich dem Text an.
            using (var check = new CheckEdit { Text = "Kurz" })
            {
                int smallWidth = check.Width;
                check.Text = "Dies ist ein deutlich längerer Text";
                int largeWidth = check.Width;

                Assert.True(largeWidth > smallWidth);
            }
        }
    }
}
