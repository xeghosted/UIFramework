using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Menus
{
    [Collection(SkinManagerCollection.Name)]
    public class MenuControllerTests : IDisposable
    {
        private readonly Control _owner = new Control();

        public void Dispose()
        {
            _owner.Dispose();
            SkinManager.ResetForTests();
        }

        private static List<MenuEntry> Entries()
        {
            var parent = new MenuEntry("&Zuletzt");
            parent.Items.Add(new MenuEntry("&A"));
            parent.Items.Add(new MenuEntry("&B"));
            return new List<MenuEntry>
            {
                new MenuEntry("&Neu"),
                MenuEntry.Separator(),
                parent,
                new MenuEntry("&Ende")
            };
        }

        private MenuController OpenContext(List<MenuEntry> entries)
        {
            var controller = new MenuController(_owner);
            _owner.CreateControl();
            controller.OpenContext(entries, new Point(100, 100));
            return controller;
        }

        /// <summary>Kleiner Test-Fake für IBarHome (das echte Interface ist
        /// internal und die MenuBar selbst existiert unabhängig davon —
        /// Muster aus der Schnittstellen-Doc: "so mit einem Test-Fake baubar,
        /// ohne von der MenuBar abzuhängen"). Ist selbst ein Control: im
        /// Leisten-Modus IST der Owner des MenuControllers die Bar. Jeder
        /// Top-Level-Index bekommt genau ein Blatt als Dropdown-Inhalt (kein
        /// Untermenü) — damit landet OpenBarDropdown(selectFirst) direkt auf
        /// einem Blatt und ein Right danach nimmt garantiert den SwitchBar-
        /// Zweig (statt OpenSubmenu). selectable steuert IsBarItemSelectable
        /// je Index unabhängig von MenuEntry.Enabled/HasChildren — genau das
        /// Wissen, das SwitchBar jetzt prüfen muss (Finding 2).</summary>
        private sealed class FakeBarHome : Control, IBarHome
        {
            private readonly IList<MenuEntry>[] _items;
            private readonly bool[] _selectable;

            public FakeBarHome(bool[] selectable)
            {
                _selectable = selectable;
                _items = new IList<MenuEntry>[selectable.Length];
                for (int i = 0; i < selectable.Length; i++)
                    _items[i] = new List<MenuEntry> { new MenuEntry("&Eins") };
            }

            int IBarHome.BarItemCount { get { return _items.Length; } }
            IList<MenuEntry> IBarHome.BarItems(int index) { return _items[index]; }
            Rectangle IBarHome.BarItemScreenBounds(int index) { return new Rectangle(index * 40, 0, 40, 20); }
            bool IBarHome.IsBarItemSelectable(int index) { return _selectable[index]; }
        }

        private static MenuController OpenBarDropdown(FakeBarHome fake, int barIndex)
        {
            fake.CreateControl();
            var controller = new MenuController(fake);
            controller.OpenBarDropdown(fake, barIndex, true); // selectFirst: wählt das eine Blatt vor
            return controller;
        }

        [Fact]
        public void Opening_a_context_menu_installs_the_filter_and_shows_one_level()
        {
            using (var controller = OpenContext(Entries()))
            {
                Assert.True(controller.IsOpen);
                Assert.Equal(1, controller.ChainDepth);
                Assert.True(controller.FilterInstalledForTests);
                controller.CloseAll();
                Assert.False(controller.FilterInstalledForTests);
                Assert.False(controller.IsOpen);
            }
        }

        [Fact]
        public void Keyboard_walks_into_a_submenu_and_back_out()
        {
            using (var controller = OpenContext(Entries()))
            {
                controller.HandleKey(Keys.Down);              // Neu
                controller.HandleKey(Keys.Down);              // Zuletzt
                controller.HandleKey(Keys.Right);             // Untermenü auf

                Assert.Equal(2, controller.ChainDepth);
                Assert.Equal(0, controller.ContentAtForTests(1).SelectedIndex); // selectFirst

                controller.HandleKey(Keys.Left);
                Assert.Equal(1, controller.ChainDepth);

                controller.HandleKey(Keys.Escape);
                Assert.False(controller.IsOpen);
            }
        }

        [Fact]
        public void Execute_closes_everything_BEFORE_the_click_handler_runs()
        {
            var entries = Entries();
            bool openDuringClick = true;
            MenuController controller = null;
            entries[3].Click += (s, e) => openDuringClick = controller.IsOpen;
            using (controller = OpenContext(entries))
            {
                controller.HandleKey(Keys.E);                 // Mnemonic &Ende

                Assert.False(openDuringClick);                // erst schließen, dann feuern
                Assert.False(controller.FilterInstalledForTests);
            }
        }

        [Fact]
        public void A_throwing_click_handler_cannot_leak_the_filter()
        {
            var entries = Entries();
            entries[3].Click += (s, e) => { throw new InvalidOperationException("App-Fehler"); };
            using (var controller = OpenContext(entries))
            {
                Assert.Throws<InvalidOperationException>(() => controller.HandleKey(Keys.E));

                Assert.False(controller.FilterInstalledForTests);
                Assert.False(controller.IsOpen);
            }
        }

        [Fact]
        public void CheckOnClick_toggles_before_the_click_event()
        {
            var entries = Entries();
            var check = new MenuEntry("&Haken") { CheckOnClick = true };
            bool checkedDuringClick = false;
            check.Click += (s, e) => checkedDuringClick = check.Checked;
            entries.Add(check);
            using (var controller = OpenContext(entries))
            {
                controller.HandleKey(Keys.H);

                Assert.True(check.Checked);
                Assert.True(checkedDuringClick);
            }
        }

        [Fact]
        public void The_hover_timer_opens_the_submenu_of_the_hovered_parent()
        {
            using (var controller = OpenContext(Entries()))
            {
                controller.ContentAtForTests(0).GetType();    // Kette steht
                // Hover auf den Eltern-Eintrag melden (Index 2), dann Timer feuern.
                controller.SimulateHoverForTests(0, 2);
                Assert.Equal(1, controller.ChainDepth);       // noch zu — Verzögerung

                controller.FireHoverTimerForTests();

                Assert.Equal(2, controller.ChainDepth);
                Assert.Equal(-1, controller.ContentAtForTests(1).SelectedIndex); // Maus: ohne Vorauswahl
            }
        }

        [Fact]
        public void Hovering_a_leaf_closes_a_stale_submenu_when_the_timer_fires()
        {
            using (var controller = OpenContext(Entries()))
            {
                controller.SimulateHoverForTests(0, 2);
                controller.FireHoverTimerForTests();
                Assert.Equal(2, controller.ChainDepth);

                controller.SimulateHoverForTests(0, 0);       // Blatt "Neu"
                controller.FireHoverTimerForTests();

                Assert.Equal(1, controller.ChainDepth);
            }
        }

        [Fact]
        public void Clicking_a_parent_opens_its_submenu_immediately()
        {
            using (var controller = OpenContext(Entries()))
            {
                controller.SimulateEntryClickForTests(0, 2);

                Assert.Equal(2, controller.ChainDepth);
            }
        }

        [Fact]
        public void A_click_on_the_context_owner_counts_as_outside_the_chain()
        {
            // Im Kontext-Modus ist der Owner ein beliebiges anderes Control
            // (z.B. eine Grid-Zeile) — ein Klick darauf bei offenem Menü ist
            // ein Außenklick und muss die Kette schließen, nicht durchlaufen
            // (Finding 1). Vorher lieferte IsWindowInChain hier fälschlich true.
            using (var controller = OpenContext(Entries()))
            {
                Assert.False(controller.IsWindowInChain(_owner.Handle));
                Assert.False(controller.IsWindowInChain(IntPtr.Zero));
            }
        }

        [Fact]
        public void A_click_on_the_bar_owner_counts_as_inside_the_chain()
        {
            // Im Leisten-Modus IST der Owner die Bar — ihr eigener Klick
            // (OnMouseDown öffnet/wechselt das Dropdown) muss durchlaufen.
            using (var fake = new FakeBarHome(new[] { true, true }))
            using (var controller = OpenBarDropdown(fake, 0))
            {
                Assert.True(controller.IsWindowInChain(fake.Handle));
            }
        }

        [Fact]
        public void SwitchBar_skips_a_non_selectable_bar_item_in_between()
        {
            // Mittlerer Eintrag (Index 1) nicht selektierbar: Rechts vom
            // ersten (Index 0) muss auf dem dritten (Index 2) landen, nicht
            // ungeprüft auf dem übersprungenen mittleren (Finding 2).
            using (var fake = new FakeBarHome(new[] { true, false, true }))
            using (var controller = OpenBarDropdown(fake, 0))
            {
                controller.HandleKey(Keys.Right);

                Assert.Equal(2, controller.BarIndex);
                Assert.True(controller.IsOpen);
            }
        }

        [Fact]
        public void SwitchBar_is_a_no_op_when_no_other_bar_item_is_selectable()
        {
            // Alle anderen Einträge nicht selektierbar: Rechts darf die Kette
            // weder schließen noch das aktuelle Dropdown neu aufbauen (das
            // würde die Auswahl im Popup zurücksetzen) — reines No-op.
            using (var fake = new FakeBarHome(new[] { true, false, false }))
            using (var controller = OpenBarDropdown(fake, 0))
            {
                int selectedBefore = controller.ContentAtForTests(0).SelectedIndex;

                controller.HandleKey(Keys.Right);

                Assert.Equal(0, controller.BarIndex);
                Assert.True(controller.IsOpen);
                Assert.Equal(1, controller.ChainDepth);
                Assert.Equal(selectedBefore, controller.ContentAtForTests(0).SelectedIndex);
            }
        }

        [Fact]
        public void SwitchBar_is_a_no_op_with_a_single_bar_item()
        {
            // barCount == 1: Rechts auf dem einzigen Blatt darf das eigene
            // Dropdown nicht mit Auswahl-Reset neu öffnen.
            using (var fake = new FakeBarHome(new[] { true }))
            using (var controller = OpenBarDropdown(fake, 0))
            {
                controller.HandleKey(Keys.Right);

                Assert.Equal(0, controller.BarIndex);
                Assert.True(controller.IsOpen);
                Assert.Equal(1, controller.ChainDepth);
            }
        }

        [Fact]
        public void Disposing_the_owner_form_closes_the_mode()
        {
            using (var form = new Form())
            {
                var anchor = new Control();
                form.Controls.Add(anchor);
                form.Show();
                var controller = new MenuController(anchor);
                controller.OpenContext(Entries(), new Point(10, 10));
                Assert.True(controller.IsOpen);

                form.Dispose();

                Assert.False(controller.IsOpen);
                Assert.False(controller.FilterInstalledForTests);
            }
        }
    }
}
