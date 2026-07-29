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
    public class MenuContentTests : IDisposable
    {
        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        private static List<MenuEntry> Entries()
        {
            var parent = new MenuEntry("&Zuletzt");
            parent.Items.Add(new MenuEntry("A"));
            return new List<MenuEntry>
            {
                new MenuEntry("&Neu") { Shortcut = Keys.Control | Keys.N },
                MenuEntry.Separator(),
                parent,
                new MenuEntry("&Ende") { Enabled = false }
            };
        }

        private static MenuContent Measured(List<MenuEntry> entries, out Size size)
        {
            var content = new MenuContent(entries);
            using (var bmp = new Bitmap(8, 8))
            using (var g = Graphics.FromImage(bmp))
                size = content.Measure(g, 96, 0);
            return content;
        }

        [Fact]
        public void Measure_stacks_rows_and_separators_are_flatter_than_entries()
        {
            List<MenuEntry> entries = Entries();
            Size size;
            var content = Measured(entries, out size);

            var row0 = content.RowBounds(0);
            var row1 = content.RowBounds(1);
            var row2 = content.RowBounds(2);

            Assert.True(row1.Height < row0.Height);                   // Separator flacher
            Assert.Equal(row0.Bottom, row1.Top);                      // lückenlos gestapelt
            Assert.Equal(row1.Bottom, row2.Top);
            Assert.True(size.Height >= content.RowBounds(3).Bottom);
        }

        [Fact]
        public void A_shortcut_widens_the_popup()
        {
            Size plain, withShortcut;
            Measured(new List<MenuEntry> { new MenuEntry("X") }, out plain);
            Measured(new List<MenuEntry> { new MenuEntry("X")
                { Shortcut = Keys.Control | Keys.Shift | Keys.S } }, out withShortcut);

            Assert.True(withShortcut.Width > plain.Width);
        }

        [Fact]
        public void Hovering_a_selectable_row_reports_its_index_a_separator_reports_none()
        {
            List<MenuEntry> entries = Entries();
            Size size;
            var content = Measured(entries, out size);
            var reported = new List<int>();
            content.HoveredIndexChanged += reported.Add;

            var row0 = content.RowBounds(0);
            content.HandleMouseMove(new Point(row0.Left + 4, row0.Top + row0.Height / 2));
            var row1 = content.RowBounds(1);
            content.HandleMouseMove(new Point(row1.Left + 4, row1.Top + row1.Height / 2));

            Assert.Equal(new[] { 0, -1 }, reported);
        }

        [Fact]
        public void Clicking_a_selectable_entry_raises_EntryClicked_only_for_it()
        {
            List<MenuEntry> entries = Entries();
            Size size;
            var content = Measured(entries, out size);
            MenuEntry clicked = null;
            content.EntryClicked += e => clicked = e;

            var separator = content.RowBounds(1);
            content.HandleMouseClick(new Point(separator.Left + 4, separator.Top));
            Assert.Null(clicked);

            var disabled = content.RowBounds(3);
            content.HandleMouseClick(new Point(disabled.Left + 4, disabled.Top + 2));
            Assert.Null(clicked);

            var parent = content.RowBounds(2);
            content.HandleMouseClick(new Point(parent.Left + 4, parent.Top + 2));
            Assert.Same(entries[2], clicked);
        }

        [Fact]
        public void Setting_the_keyboard_selection_raises_VisualChanged()
        {
            List<MenuEntry> entries = Entries();
            Size size;
            var content = Measured(entries, out size);
            int raised = 0;
            content.VisualChanged += (s, e) => raised++;

            content.SelectedIndex = 0;

            Assert.Equal(1, raised);
            Assert.Equal(0, content.SelectedIndex);
        }

        [Fact]
        public void HandleKey_declines_everything_keys_belong_to_the_controller()
        {
            List<MenuEntry> entries = Entries();
            Size size;
            var content = Measured(entries, out size);

            Assert.False(content.HandleKey(Keys.Down));
            Assert.False(content.HandleKey(Keys.Escape));
        }

        [Fact]
        public void Paint_runs_without_crashing_in_both_skins()
        {
            // Zeichnet wirklich (Haken, Pfeil, Separator, Disabled) — Absturzfreiheit
            // und "malt irgendwas" sind headless prüfbar, Pixel-Wahrheit prüft die
            // GUI-Fahrprobe.
            List<MenuEntry> entries = Entries();
            entries[0].Checked = true;
            Size size;
            var content = Measured(entries, out size);

            using (var bmp = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height)))
            using (var g = Graphics.FromImage(bmp))
            {
                content.Paint(g, new Rectangle(Point.Empty, size), 96);
            }
        }
    }
}
