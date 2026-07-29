using System.Windows.Forms;
using UIFramework.Controls;
using Xunit;

namespace UIFramework.Tests.Ribbon
{
    public class RibbonModelTests
    {
        [Fact]
        public void Item_defaults_are_a_large_enabled_button_without_image()
        {
            var item = new RibbonItem("Speichern");
            Assert.Equal(RibbonItemKind.Button, item.Kind);
            Assert.Equal(RibbonItemSize.Large, item.Size);
            Assert.True(item.Enabled);
            Assert.False(item.Checked);
            Assert.Null(item.Image);
            Assert.Null(item.Menu);
            Assert.True(item.IsInteractive);
        }

        [Fact]
        public void A_separator_is_never_interactive()
        {
            var separator = RibbonItem.Separator();
            Assert.Equal(RibbonItemKind.Separator, separator.Kind);
            Assert.False(separator.IsInteractive);
        }

        [Fact]
        public void A_disabled_item_is_not_interactive()
        {
            Assert.False(new RibbonItem("X") { Enabled = false }.IsInteractive);
        }

        [Fact]
        public void PerformClick_raises_click_with_the_item_as_sender()
        {
            var item = new RibbonItem("X");
            object sender = null;
            item.Click += (s, e) => sender = s;
            item.PerformClick();
            Assert.Same(item, sender);
        }

        [Fact]
        public void Tab_and_group_collections_are_never_null()
        {
            Assert.NotNull(new RibbonTab().Groups);
            Assert.NotNull(new RibbonGroup().Items);
            Assert.True(new RibbonTab().Enabled);
        }
    }
}
