using System.Collections.Generic;
using System.Drawing;
using UIFramework.Controls;
using Xunit;

namespace UIFramework.Tests.Ribbon
{
    public class RibbonLayoutTests
    {
        private static RibbonBox Box(RibbonItem item, int w, int h)
        {
            return new RibbonBox { Item = item, Size = new Size(w, h) };
        }

        [Fact]
        public void Tab_headers_line_up_gapless_from_the_origin()
        {
            var rects = RibbonLayout.LayoutTabHeaders(
                new List<Size> { new Size(60, 24), new Size(80, 24) }, new Point(10, 5));
            Assert.Equal(new Rectangle(10, 5, 60, 24), rects[0]);
            Assert.Equal(new Rectangle(70, 5, 80, 24), rects[1]);
        }

        [Fact]
        public void A_large_item_takes_a_full_height_column_of_its_own()
        {
            var large = new RibbonItem("A");
            var placed = RibbonLayout.ArrangeGroupItems(
                new List<RibbonBox> { Box(large, 48, 90) }, 90, 4);
            Assert.Single(placed);
            Assert.Equal(new Rectangle(0, 0, 48, 90), placed[0].Bounds);
        }

        [Fact]
        public void Small_items_stack_three_per_column_then_wrap()
        {
            var items = new List<RibbonBox>();
            for (int i = 0; i < 4; i++)
                items.Add(Box(new RibbonItem("S" + i) { Size = RibbonItemSize.Small }, 40 + i, 20));
            var placed = RibbonLayout.ArrangeGroupItems(items, 90, 4);

            Assert.Equal(new Rectangle(0, 0, 40, 30), placed[0].Bounds);   // Zeile = 90/3
            Assert.Equal(new Rectangle(0, 30, 41, 30), placed[1].Bounds);
            Assert.Equal(new Rectangle(0, 60, 42, 30), placed[2].Bounds);
            // neue Spalte: Spaltenbreite = max(40..42) = 42, plus gap 4 → x = 46
            Assert.Equal(new Rectangle(46, 0, 43, 30), placed[3].Bounds);
        }

        [Fact]
        public void A_large_item_ends_a_running_small_column()
        {
            var placed = RibbonLayout.ArrangeGroupItems(new List<RibbonBox>
            {
                Box(new RibbonItem("S") { Size = RibbonItemSize.Small }, 40, 20),
                Box(new RibbonItem("L"), 48, 90)
            }, 90, 4);
            Assert.Equal(0, placed[0].Bounds.Left);
            Assert.Equal(44, placed[1].Bounds.Left);                       // 40 + gap 4
            Assert.Equal(90, placed[1].Bounds.Height);
        }

        [Fact]
        public void A_separator_gets_its_own_full_height_column()
        {
            var placed = RibbonLayout.ArrangeGroupItems(new List<RibbonBox>
            {
                Box(new RibbonItem("L1"), 48, 90),
                Box(RibbonItem.Separator(), 7, 90),
                Box(new RibbonItem("L2"), 48, 90)
            }, 90, 4);
            Assert.Equal(new Rectangle(52, 0, 7, 90), placed[1].Bounds);
            Assert.Equal(new Rectangle(63, 0, 48, 90), placed[2].Bounds);
        }

        [Fact]
        public void Groups_line_up_with_frames_titles_and_shifted_items()
        {
            var arranged = new[]
            {
                new[] { new RibbonPlacedItem { Item = new RibbonItem("A"), Bounds = new Rectangle(0, 0, 48, 90) } }
            };
            var panes = RibbonLayout.LayoutGroups(
                new List<Size> { new Size(48, 90) }, arranged,
                new Point(20, 40), titleHeight: 18, groupGap: 6,
                padLeft: 5, padTop: 5, padRight: 5, padBottom: 5);

            Assert.Single(panes);
            Assert.Equal(new Rectangle(20, 40, 58, 118), panes[0].Frame);      // 48+10 x 90+10+18
            Assert.Equal(new Rectangle(20, 140, 58, 18), panes[0].TitleRow);   // unten im Rahmen
            Assert.Equal(new Rectangle(25, 45, 48, 90), panes[0].Items[0].Bounds);
        }

        [Fact]
        public void An_empty_group_yields_empty_arrays_not_crashes()
        {
            Assert.Empty(RibbonLayout.ArrangeGroupItems(new List<RibbonBox>(), 90, 4));
        }
    }
}
