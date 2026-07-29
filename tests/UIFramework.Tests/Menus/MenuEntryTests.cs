using System.Windows.Forms;
using UIFramework.Controls;
using Xunit;

namespace UIFramework.Tests.Menus
{
    public class MenuEntryTests
    {
        [Fact]
        public void Defaults_are_enabled_unchecked_no_shortcut_no_children()
        {
            var entry = new MenuEntry("&Datei");

            Assert.True(entry.Enabled);
            Assert.False(entry.Checked);
            Assert.False(entry.CheckOnClick);
            Assert.Equal(Keys.None, entry.Shortcut);
            Assert.Empty(entry.Items);
            Assert.False(entry.IsSeparator);
            Assert.True(entry.IsSelectable);
        }

        [Fact]
        public void A_separator_is_never_selectable()
        {
            var separator = MenuEntry.Separator();

            Assert.True(separator.IsSeparator);
            Assert.False(separator.IsSelectable);
        }

        [Fact]
        public void PerformClick_raises_the_click_event_with_the_entry_as_sender()
        {
            var entry = new MenuEntry("X");
            object sender = null;
            entry.Click += (s, e) => sender = s;

            entry.PerformClick();

            Assert.Same(entry, sender);
        }
    }
}
