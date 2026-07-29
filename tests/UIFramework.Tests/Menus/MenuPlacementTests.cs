using System.Drawing;
using UIFramework.Controls;
using Xunit;

namespace UIFramework.Tests.Menus
{
    public class MenuPlacementTests
    {
        private static readonly Rectangle Work = new Rectangle(0, 0, 1000, 800);

        [Fact]
        public void A_dropdown_opens_below_the_bar_item_left_aligned()
        {
            var rect = MenuPlacement.PlaceDropdown(
                new Rectangle(100, 30, 60, 24), new Size(200, 300), Work);

            Assert.Equal(new Rectangle(100, 54, 200, 300), rect);
        }

        [Fact]
        public void A_dropdown_flips_above_when_it_would_leave_the_work_area()
        {
            var rect = MenuPlacement.PlaceDropdown(
                new Rectangle(100, 700, 60, 24), new Size(200, 300), Work);

            Assert.Equal(400, rect.Top);                              // 700 - 300
        }

        [Fact]
        public void A_submenu_opens_to_the_right_top_aligned()
        {
            var rect = MenuPlacement.PlaceSubmenu(
                new Rectangle(200, 100, 180, 24), new Size(150, 200), Work);

            Assert.Equal(new Rectangle(380, 100, 150, 200), rect);
        }

        [Fact]
        public void A_submenu_flips_to_the_left_at_the_right_edge()
        {
            var rect = MenuPlacement.PlaceSubmenu(
                new Rectangle(900, 100, 90, 24), new Size(150, 200), Work);

            Assert.Equal(750, rect.Left);                             // 900 - 150
        }

        [Fact]
        public void A_context_menu_flips_up_and_left_at_the_bottom_right_corner()
        {
            var rect = MenuPlacement.PlaceContextMenu(
                new Point(990, 790), new Size(150, 200), Work);

            Assert.Equal(new Rectangle(840, 590, 150, 200), rect);
        }

        [Fact]
        public void Nothing_ever_leaves_the_work_area_even_when_flipping_cannot_help()
        {
            // Zu groß für beide Richtungen: an die Kante klemmen statt negativ werden.
            var rect = MenuPlacement.PlaceDropdown(
                new Rectangle(0, 10, 60, 24), new Size(300, 900), Work);

            Assert.True(rect.Top >= Work.Top);
            Assert.True(rect.Left >= Work.Left);
        }
    }
}
