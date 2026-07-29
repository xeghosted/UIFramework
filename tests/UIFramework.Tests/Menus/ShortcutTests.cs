using System;
using System.Windows.Forms;
using UIFramework.Controls;
using Xunit;

namespace UIFramework.Tests.Menus
{
    public class ShortcutTests
    {
        [Theory]
        [InlineData(Keys.None, "")]
        [InlineData(Keys.Control | Keys.S, "Ctrl+S")]
        [InlineData(Keys.Control | Keys.Shift | Keys.S, "Ctrl+Shift+S")]
        [InlineData(Keys.Alt | Keys.F4, "Alt+F4")]
        [InlineData(Keys.Control | Keys.D1, "Ctrl+1")]
        [InlineData(Keys.Delete, "Delete")]
        public void Format_is_deterministic_and_ordered(Keys shortcut, string expected)
        {
            Assert.Equal(expected, ShortcutDisplay.Format(shortcut));
        }

        [Fact]
        public void Find_walks_into_submenus_and_matches_the_full_key_data()
        {
            var hit = new MenuEntry("&Speichern") { Shortcut = Keys.Control | Keys.S };
            var parent = new MenuEntry("&Datei");
            parent.Items.Add(new MenuEntry("&Neu") { Shortcut = Keys.Control | Keys.N });
            parent.Items.Add(hit);
            var root = new System.Collections.Generic.List<MenuEntry> { parent };

            Assert.Same(hit, MenuShortcuts.Find(root, Keys.Control | Keys.S));
            Assert.Null(MenuShortcuts.Find(root, Keys.Control | Keys.X));
            Assert.Null(MenuShortcuts.Find(root, Keys.S)); // ohne Modifier kein Treffer
        }

        [Fact]
        public void Find_skips_disabled_entries_and_whole_disabled_subtrees()
        {
            var disabledLeaf = new MenuEntry("A") { Shortcut = Keys.F5, Enabled = false };
            var disabledParent = new MenuEntry("B") { Enabled = false };
            disabledParent.Items.Add(new MenuEntry("C") { Shortcut = Keys.F6 });
            var root = new System.Collections.Generic.List<MenuEntry> { disabledLeaf, disabledParent };

            Assert.Null(MenuShortcuts.Find(root, Keys.F5));
            Assert.Null(MenuShortcuts.Find(root, Keys.F6));
        }

        [Fact]
        public void Find_never_returns_a_parent_even_if_it_carries_a_shortcut()
        {
            // Eltern führen nicht aus (Spec) — ein versehentlich gesetzter
            // Shortcut auf einem Eltern-Eintrag bleibt wirkungslos.
            var parent = new MenuEntry("&Datei") { Shortcut = Keys.F7 };
            parent.Items.Add(new MenuEntry("Kind"));
            var root = new System.Collections.Generic.List<MenuEntry> { parent };

            Assert.Null(MenuShortcuts.Find(root, Keys.F7));
        }
    }
}
