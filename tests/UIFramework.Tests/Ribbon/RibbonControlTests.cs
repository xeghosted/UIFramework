using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Ribbon
{
    [Collection(SkinManagerCollection.Name)]
    public class RibbonControlTests : IDisposable
    {
        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        private static RibbonControl TestRibbon()
        {
            var ribbon = new RibbonControl { Width = 800 };
            var tab1 = new RibbonTab { Text = "Start" };
            var group = new RibbonGroup { Title = "Ablage" };
            group.Items.Add(new RibbonItem("Neu"));
            group.Items.Add(new RibbonItem("Öffnen") { Size = RibbonItemSize.Small });
            group.Items.Add(RibbonItem.Separator());
            group.Items.Add(new RibbonItem("Aus") { Enabled = false });
            tab1.Groups.Add(group);
            ribbon.Tabs.Add(tab1);
            ribbon.Tabs.Add(new RibbonTab { Text = "Extras" });
            ribbon.Tabs.Add(new RibbonTab { Text = "Gesperrt", Enabled = false });

            var host = new Form();
            host.Controls.Add(ribbon);
            host.Show();          // parentless Controls malen nie (Task-9-Lehre aus 4a)
            ribbon.Refresh();
            return ribbon;
        }

        [Fact]
        public void The_ribbon_is_not_selectable_and_has_an_intrinsic_height()
        {
            using (var ribbon = TestRibbon())
            {
                Assert.False(ribbon.CanSelect);
                Assert.True(ribbon.Height > 0);
                Assert.Equal(ribbon.GetPreferredSize(Size.Empty).Height, ribbon.Height);
            }
        }

        [Fact]
        public void Scaling_cannot_permanently_distort_the_height()
        {
            using (var ribbon = TestRibbon())
            {
                ribbon.Scale(new SizeF(1.25f, 1.25f));
                ribbon.PerformLayout();
                Assert.Equal(ribbon.GetPreferredSize(Size.Empty).Height, ribbon.Height);
            }
        }

        [Fact]
        public void Tab_headers_and_items_are_cached_after_paint_and_hit_test_finds_them()
        {
            using (var ribbon = TestRibbon())
            {
                var headers = ribbon.TabHeaderBoundsForTests();
                Assert.Equal(3, headers.Length);
                Assert.True(headers[0].Right <= headers[1].Left);

                var hit = ribbon.HitTestForTests(new Point(
                    headers[1].Left + 2, headers[1].Top + 2));
                Assert.Equal(RibbonHitKind.TabHeader, hit.Kind);
                Assert.Equal(1, hit.TabIndex);

                var items = ribbon.PlacedItemsForTests();
                Assert.True(items.Length >= 4);
                var first = items[0];
                var itemHit = ribbon.HitTestForTests(new Point(
                    first.Bounds.Left + 2, first.Bounds.Top + 2));
                Assert.Equal(RibbonHitKind.Item, itemHit.Kind);
                Assert.Same(first.Item, itemHit.Item);
            }
        }

        [Fact]
        public void Clicking_an_enabled_tab_selects_it_a_disabled_one_does_not()
        {
            using (var ribbon = TestRibbon())
            {
                var headers = ribbon.TabHeaderBoundsForTests();
                ribbon.PerformMouseDownForTests(new Point(headers[1].Left + 2, headers[1].Top + 2));
                Assert.Equal(1, ribbon.SelectedTabIndex);

                ribbon.PerformMouseDownForTests(new Point(headers[2].Left + 2, headers[2].Top + 2));
                Assert.Equal(1, ribbon.SelectedTabIndex);   // Gesperrt bleibt wirkungslos
            }
        }

        [Fact]
        public void SelectedTabIndex_clamps_and_reports_minus_one_without_tabs()
        {
            using (var empty = new RibbonControl())
            {
                Assert.Equal(-1, empty.SelectedTabIndex);
            }
            using (var ribbon = TestRibbon())
            {
                ribbon.SelectedTabIndex = 99;
                Assert.Equal(2, ribbon.SelectedTabIndex);
                ribbon.SelectedTabIndex = -5;
                Assert.Equal(0, ribbon.SelectedTabIndex);
            }
        }

        [Fact]
        public void Mouse_leave_clears_all_hover_state()
        {
            using (var ribbon = TestRibbon())
            {
                var headers = ribbon.TabHeaderBoundsForTests();
                ribbon.PerformMouseMoveForTests(new Point(headers[0].Left + 2, headers[0].Top + 2));
                Assert.True(ribbon.HasHoverForTests);
                ribbon.PerformMouseLeaveForTests();
                Assert.False(ribbon.HasHoverForTests);
            }
        }

        // ---- Fix-Runde 1: Image == null / leerer Text kollabieren die
        // jeweils fehlende Zone (Design-Spec, Modell-Abschnitt) — additiv,
        // bestehende Fakten oben bleiben unangetastet.

        [Fact]
        public void A_small_item_without_an_image_measures_narrower_than_the_same_item_with_one()
        {
            using (var image = new Bitmap(16, 16))
            {
                var withImage = new RibbonItem("Ab") { Size = RibbonItemSize.Small, Image = image };
                var withoutImage = new RibbonItem("Ab") { Size = RibbonItemSize.Small };

                var ribbon = new RibbonControl { Width = 800 };
                var tab = new RibbonTab { Text = "Start" };
                var group = new RibbonGroup { Title = "G" };
                group.Items.Add(withImage);
                group.Items.Add(withoutImage);
                tab.Groups.Add(group);
                ribbon.Tabs.Add(tab);

                var host = new Form();
                host.Controls.Add(ribbon);
                host.Show();
                ribbon.Refresh();

                var placed = ribbon.PlacedItemsForTests();
                var boundsWith = Array.Find(placed, p => ReferenceEquals(p.Item, withImage)).Bounds;
                var boundsWithout = Array.Find(placed, p => ReferenceEquals(p.Item, withoutImage)).Bounds;

                Assert.True(boundsWithout.Width < boundsWith.Width);

                ribbon.Dispose();
            }
        }

        [Fact]
        public void Large_items_without_text_or_without_an_image_paint_without_crashing_and_stay_hit_testable()
        {
            using (var image = new Bitmap(16, 16))
            {
                var imageOnly = new RibbonItem { Image = image };     // kein Text
                var textOnly = new RibbonItem("Nur Text");            // kein Bild

                var ribbon = new RibbonControl { Width = 800 };
                var tab = new RibbonTab { Text = "Start" };
                var group = new RibbonGroup { Title = "G" };
                group.Items.Add(imageOnly);
                group.Items.Add(textOnly);
                tab.Groups.Add(group);
                ribbon.Tabs.Add(tab);

                var host = new Form();
                host.Controls.Add(ribbon);
                host.Show();
                ribbon.Refresh();       // darf nicht werfen

                var placed = ribbon.PlacedItemsForTests();
                var imageOnlyBounds = Array.Find(placed, p => ReferenceEquals(p.Item, imageOnly)).Bounds;
                var textOnlyBounds = Array.Find(placed, p => ReferenceEquals(p.Item, textOnly)).Bounds;

                var hitImageOnly = ribbon.HitTestForTests(new Point(
                    imageOnlyBounds.Left + imageOnlyBounds.Width / 2,
                    imageOnlyBounds.Top + imageOnlyBounds.Height / 2));
                Assert.Equal(RibbonHitKind.Item, hitImageOnly.Kind);
                Assert.Same(imageOnly, hitImageOnly.Item);

                var hitTextOnly = ribbon.HitTestForTests(new Point(
                    textOnlyBounds.Left + textOnlyBounds.Width / 2,
                    textOnlyBounds.Top + textOnlyBounds.Height / 2));
                Assert.Equal(RibbonHitKind.Item, hitTextOnly.Kind);
                Assert.Same(textOnly, hitTextOnly.Item);

                ribbon.Dispose();
            }
        }
    }
}
