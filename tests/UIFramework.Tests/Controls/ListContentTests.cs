using System;
using System.Collections.Generic;
using System.Drawing;
using UIFramework.Controls;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class ListContentTests
    {
        private static ListContent Content(out List<object> items, int selected = -1)
        {
            items = new List<object> { "Alpha", "Beta", "Gamma" };
            return new ListContent(items, () => selected);
        }

        [Fact]
        public void Measure_stacks_one_row_per_item_and_honours_the_anchor_width()
        {
            List<object> items;
            var content = Content(out items);

            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                var size = content.Measure(g, 96, anchorWidth: 200);

                Assert.Equal(200, size.Width);
                Assert.True(size.Height >= 3 * 16);   // drei Zeilen à mindestens Fonthöhe
                Assert.Equal(0, size.Height % 3);     // exakt drei gleich hohe Zeilen
            }
        }

        [Fact]
        public void A_click_on_the_second_row_chooses_index_one_and_requests_close()
        {
            List<object> items;
            var content = Content(out items);

            int chosen = -1;
            bool closeAsked = false;
            content.ItemChosen += index => chosen = index;
            content.CloseRequested += (s, e) => closeAsked = true;

            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                var size = content.Measure(g, 96, 200);
                int rowHeight = size.Height / 3;

                content.HandleMouseMove(new Point(10, rowHeight + rowHeight / 2));
                content.HandleMouseClick(new Point(10, rowHeight + rowHeight / 2));
            }

            Assert.Equal(1, chosen);
            Assert.True(closeAsked);
        }

        [Fact]
        public void Escape_requests_close_and_reports_the_key_as_handled()
        {
            List<object> items;
            var content = Content(out items);

            bool closeAsked = false;
            content.CloseRequested += (s, e) => closeAsked = true;

            Assert.True(content.HandleKey(System.Windows.Forms.Keys.Escape));
            Assert.True(closeAsked);
        }
    }
}
