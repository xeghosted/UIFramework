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

        // ---- Abschluss-Review: Fund 1 (Innen-Geometrie aus der Normal-
        // Erscheinung), Fund 2 (Guard geschrumpftes Modell), Fund 3 (Hover
        // nullen bei Tab-Wechsel) — additiv, Fakten oben bleiben unangetastet.

        [Fact]
        public void A_checked_and_an_unchecked_small_toggle_button_of_the_same_text_get_identical_bounds()
        {
            var checkedItem = new RibbonItem("Ab") { Kind = RibbonItemKind.ToggleButton, Size = RibbonItemSize.Small, Checked = true };
            var uncheckedItem = new RibbonItem("Ab") { Kind = RibbonItemKind.ToggleButton, Size = RibbonItemSize.Small, Checked = false };

            var ribbon = new RibbonControl { Width = 800 };
            var tab = new RibbonTab { Text = "Start" };
            var group = new RibbonGroup { Title = "G" };
            group.Items.Add(checkedItem);
            group.Items.Add(uncheckedItem);
            tab.Groups.Add(group);
            ribbon.Tabs.Add(tab);

            var host = new Form();
            host.Controls.Add(ribbon);
            host.Show();
            ribbon.Refresh();      // Malpfad darf für BEIDE Checked-Zustände nicht werfen

            var placed = ribbon.PlacedItemsForTests();
            var checkedBounds = Array.Find(placed, p => ReferenceEquals(p.Item, checkedItem)).Bounds;
            var uncheckedBounds = Array.Find(placed, p => ReferenceEquals(p.Item, uncheckedItem)).Bounds;

            // Gleicher Text -> gleiche gemessene Box, unabhängig vom Checked-
            // Zustand (BuildBox misst schon immer mit metrics.ButtonAppearance).
            Assert.Equal(checkedBounds.Size, uncheckedBounds.Size);

            ribbon.Dispose();
        }

        [Fact]
        public void Checking_a_toggle_button_does_not_shift_its_inner_content_zone()
        {
            // Regressionsbeweis für Fund 1: die Selected-Erscheinung von
            // RibbonButton trägt BorderWidth 1 (Normal/Disabled 0) — käme die
            // Innenzone aus der Zustands-Erscheinung, verschöbe sich diese
            // Zone beim Checken um genau diese Rahmenbreite.
            var item = new RibbonItem("Ab") { Kind = RibbonItemKind.ToggleButton, Size = RibbonItemSize.Small };

            var ribbon = new RibbonControl { Width = 800 };
            var tab = new RibbonTab { Text = "Start" };
            var group = new RibbonGroup { Title = "G" };
            group.Items.Add(item);
            tab.Groups.Add(group);
            ribbon.Tabs.Add(tab);

            var host = new Form();
            host.Controls.Add(ribbon);
            host.Show();
            ribbon.Refresh();

            var uncheckedZone = ribbon.InnerZoneForTests(item);

            item.Checked = true;
            ribbon.Refresh();      // Zustand jetzt Selected (Border 1 statt 0)

            var checkedZone = ribbon.InnerZoneForTests(item);

            Assert.Equal(uncheckedZone, checkedZone);

            ribbon.Dispose();
        }

        [Fact]
        public void Clicking_a_tab_header_whose_tab_was_removed_since_the_last_repaint_does_not_throw()
        {
            using (var ribbon = TestRibbon())
            {
                var headers = ribbon.TabHeaderBoundsForTests();
                var staleHeaderPoint = new Point(headers[2].Left + 2, headers[2].Top + 2);

                // Modell nachträglich verkleinert, OHNE Repaint dazwischen —
                // _tabHeaderBounds bleibt stale auf 3 Einträge stehen (Fund 2).
                ribbon.Tabs.RemoveAt(2);
                ribbon.Tabs.RemoveAt(1);

                var exception = Record.Exception(() => ribbon.PerformMouseDownForTests(staleHeaderPoint));
                Assert.Null(exception);
            }
        }

        [Fact]
        public void Changing_the_selected_tab_clears_stale_hover_state()
        {
            using (var ribbon = TestRibbon())
            {
                var headers = ribbon.TabHeaderBoundsForTests();
                ribbon.PerformMouseMoveForTests(new Point(headers[0].Left + 2, headers[0].Top + 2));
                Assert.True(ribbon.HasHoverForTests);

                ribbon.SelectedTabIndex = 1;   // Fund 3: Hover muss mit dem Wechsel verschwinden

                Assert.False(ribbon.HasHoverForTests);
            }
        }
    }
}
