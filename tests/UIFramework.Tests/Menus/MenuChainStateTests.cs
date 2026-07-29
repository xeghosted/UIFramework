using System.Collections.Generic;
using System.Windows.Forms;
using UIFramework.Controls;
using Xunit;

namespace UIFramework.Tests.Menus
{
    public class MenuChainStateTests
    {
        private static List<MenuEntry> FileMenu()
        {
            // [0] Neu, [1] Separator, [2] Zuletzt (2 Kinder), [3] Deaktiviert, [4] Ende
            var recent = new MenuEntry("&Zuletzt");
            recent.Items.Add(new MenuEntry("A"));
            recent.Items.Add(new MenuEntry("B"));
            return new List<MenuEntry>
            {
                new MenuEntry("&Neu"),
                MenuEntry.Separator(),
                recent,
                new MenuEntry("&Kaputt") { Enabled = false },
                new MenuEntry("&Ende")
            };
        }

        private static MenuChainState OpenedFileMenu(int barCount = 3, int barIndex = 0)
        {
            var state = new MenuChainState(barCount, barIndex);
            state.PushLevel(FileMenu());
            return state;
        }

        [Fact]
        public void Down_skips_separators_and_disabled_and_wraps()
        {
            var state = OpenedFileMenu();

            Assert.Equal(MenuKeyActionKind.SelectionChanged, state.HandleKey(Keys.Down).Kind);
            Assert.Equal(0, state.SelectedIndex(0));                 // Neu
            state.HandleKey(Keys.Down);
            Assert.Equal(2, state.SelectedIndex(0));                 // Zuletzt (1 übersprungen)
            state.HandleKey(Keys.Down);
            Assert.Equal(4, state.SelectedIndex(0));                 // Ende (3 übersprungen)
            state.HandleKey(Keys.Down);
            Assert.Equal(0, state.SelectedIndex(0));                 // Umlauf
        }

        [Fact]
        public void Up_from_nothing_selects_the_last_selectable()
        {
            var state = OpenedFileMenu();

            state.HandleKey(Keys.Up);

            Assert.Equal(4, state.SelectedIndex(0));
        }

        [Fact]
        public void Home_and_End_jump_to_first_and_last_selectable()
        {
            var state = OpenedFileMenu();

            state.HandleKey(Keys.End);
            Assert.Equal(4, state.SelectedIndex(0));
            state.HandleKey(Keys.Home);
            Assert.Equal(0, state.SelectedIndex(0));
        }

        [Fact]
        public void Right_on_a_parent_requests_the_submenu_but_does_not_push()
        {
            var state = OpenedFileMenu();
            state.SetSelection(0, 2);

            var action = state.HandleKey(Keys.Right);

            Assert.Equal(MenuKeyActionKind.OpenSubmenu, action.Kind);
            Assert.Same(state.EntriesAt(0)[2], action.Entry);
            Assert.Equal(1, state.Depth);                            // Controller pusht
        }

        [Fact]
        public void Right_on_a_leaf_switches_to_the_next_bar_menu()
        {
            var state = OpenedFileMenu(barCount: 3, barIndex: 0);
            state.SetSelection(0, 0);

            var action = state.HandleKey(Keys.Right);

            Assert.Equal(MenuKeyActionKind.SwitchBar, action.Kind);
            Assert.Equal(1, action.NewBarIndex);
        }

        [Fact]
        public void Right_on_a_leaf_without_a_bar_does_nothing()
        {
            var state = new MenuChainState(0, -1);                   // Kontextmenü
            state.PushLevel(FileMenu());
            state.SetSelection(0, 0);

            Assert.Equal(MenuKeyActionKind.None, state.HandleKey(Keys.Right).Kind);
        }

        [Fact]
        public void Left_at_the_top_level_switches_to_the_previous_bar_menu_with_wraparound()
        {
            var state = OpenedFileMenu(barCount: 3, barIndex: 0);

            var action = state.HandleKey(Keys.Left);

            Assert.Equal(MenuKeyActionKind.SwitchBar, action.Kind);
            Assert.Equal(2, action.NewBarIndex);
        }

        [Fact]
        public void Left_in_a_submenu_pops_the_level()
        {
            var state = OpenedFileMenu();
            state.SetSelection(0, 2);
            state.PushLevel(state.EntriesAt(0)[2].Items);

            var action = state.HandleKey(Keys.Left);

            Assert.Equal(MenuKeyActionKind.CloseLevel, action.Kind);
            Assert.Equal(1, state.Depth);
        }

        [Fact]
        public void Enter_executes_a_leaf_and_opens_a_parent()
        {
            var state = OpenedFileMenu();
            state.SetSelection(0, 4);
            Assert.Equal(MenuKeyActionKind.Execute, state.HandleKey(Keys.Enter).Kind);

            state.SetSelection(0, 2);
            Assert.Equal(MenuKeyActionKind.OpenSubmenu, state.HandleKey(Keys.Enter).Kind);
        }

        [Fact]
        public void Enter_without_a_selection_does_nothing()
        {
            var state = OpenedFileMenu();

            Assert.Equal(MenuKeyActionKind.None, state.HandleKey(Keys.Enter).Kind);
        }

        [Fact]
        public void Escape_closes_level_by_level_and_finally_the_mode()
        {
            var state = OpenedFileMenu();
            state.SetSelection(0, 2);
            state.PushLevel(state.EntriesAt(0)[2].Items);

            Assert.Equal(MenuKeyActionKind.CloseLevel, state.HandleKey(Keys.Escape).Kind);
            Assert.Equal(MenuKeyActionKind.CloseAll, state.HandleKey(Keys.Escape).Kind);
            Assert.Equal(0, state.Depth);
        }

        [Fact]
        public void A_mnemonic_selects_and_executes_the_matching_leaf()
        {
            var state = OpenedFileMenu();

            var action = state.HandleKey(Keys.E);                    // &Ende

            Assert.Equal(MenuKeyActionKind.Execute, action.Kind);
            Assert.Same(state.EntriesAt(0)[4], action.Entry);
        }

        [Fact]
        public void A_mnemonic_on_a_disabled_entry_does_nothing()
        {
            var state = OpenedFileMenu();

            Assert.Equal(MenuKeyActionKind.None, state.HandleKey(Keys.K).Kind); // &Kaputt disabled
        }

        [Fact]
        public void Arrows_do_nothing_when_no_entry_is_selectable()
        {
            var state = new MenuChainState(0, -1);
            state.PushLevel(new List<MenuEntry> { MenuEntry.Separator(),
                new MenuEntry("X") { Enabled = false } });

            Assert.Equal(MenuKeyActionKind.None, state.HandleKey(Keys.Down).Kind);
            Assert.Equal(-1, state.SelectedIndex(0));
        }

        [Fact]
        public void TruncateTo_pops_down_to_the_requested_depth()
        {
            var state = OpenedFileMenu();
            state.SetSelection(0, 2);
            state.PushLevel(state.EntriesAt(0)[2].Items);

            state.TruncateTo(1);

            Assert.Equal(1, state.Depth);
        }
    }
}
