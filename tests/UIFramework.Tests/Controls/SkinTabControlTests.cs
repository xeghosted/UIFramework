using System;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class SkinTabControlTests : IDisposable
    {
        public SkinTabControlTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void The_first_added_tab_is_selected_automatically()
        {
            using (var tabs = new SkinTabControl())
            using (var page = new Panel())
            {
                tabs.AddTab("Erste", page);

                Assert.Equal(0, tabs.SelectedIndex);
                Assert.True(page.Visible);
            }
        }

        [Fact]
        public void Only_the_selected_pages_content_is_visible()
        {
            using (var tabs = new SkinTabControl())
            using (var pageA = new Panel())
            using (var pageB = new Panel())
            {
                tabs.AddTab("A", pageA);
                tabs.AddTab("B", pageB);

                Assert.True(pageA.Visible);
                Assert.False(pageB.Visible);

                tabs.SelectedIndex = 1;

                Assert.False(pageA.Visible);
                Assert.True(pageB.Visible);
            }
        }

        [Fact]
        public void Changing_SelectedIndex_raises_SelectedIndexChanged_once()
        {
            using (var tabs = new SkinTabControl())
            using (var pageA = new Panel())
            using (var pageB = new Panel())
            {
                tabs.AddTab("A", pageA);
                tabs.AddTab("B", pageB);

                int count = 0;
                tabs.SelectedIndexChanged += (s, e) => count++;

                tabs.SelectedIndex = 1;

                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void Setting_SelectedIndex_to_its_current_value_does_not_raise_the_event()
        {
            using (var tabs = new SkinTabControl())
            using (var pageA = new Panel())
            using (var pageB = new Panel())
            {
                tabs.AddTab("A", pageA);
                tabs.AddTab("B", pageB);

                int count = 0;
                tabs.SelectedIndexChanged += (s, e) => count++;

                tabs.SelectedIndex = 0;

                Assert.Equal(0, count);
            }
        }

        [Fact]
        public void Setting_SelectedIndex_out_of_range_throws()
        {
            using (var tabs = new SkinTabControl())
            using (var page = new Panel())
            {
                tabs.AddTab("A", page);

                Assert.Throws<ArgumentOutOfRangeException>(() => tabs.SelectedIndex = 3);
            }
        }

        [Fact]
        public void AddTab_with_null_content_throws()
        {
            using (var tabs = new SkinTabControl())
            {
                Assert.Throws<ArgumentNullException>(() => tabs.AddTab("A", null));
            }
        }

        [Fact]
        public void Clicking_a_header_activates_its_page()
        {
            using (var tabs = new SkinTabControl())
            using (var pageA = new Panel())
            using (var pageB = new Panel())
            {
                tabs.AddTab("A", pageA);
                tabs.AddTab("B", pageB);

                // Der zweite Reiter ist das zweite hinzugefuegte Kind-Control der
                // Kopfleiste (Index 1) -- siehe SkinTabControl.AddTab.
                var headerStrip = (Control)tabs.Controls[1];
                var secondHeader = (SkinTabControl.TabHeaderItem)headerStrip.Controls[1];
                secondHeader.RaiseClickForTests();

                Assert.Equal(1, tabs.SelectedIndex);
            }
        }
    }
}
