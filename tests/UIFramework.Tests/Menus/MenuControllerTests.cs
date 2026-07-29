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
        public void All_chain_windows_and_the_owner_count_as_inside()
        {
            using (var controller = OpenContext(Entries()))
            {
                Assert.True(controller.IsWindowInChain(_owner.Handle));
                Assert.False(controller.IsWindowInChain(IntPtr.Zero));
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
