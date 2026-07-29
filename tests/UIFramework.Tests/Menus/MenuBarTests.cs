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
    public class MenuBarTests : IDisposable
    {
        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        /// <summary>Bar mit "&Datei" (Kinder: "&Neu", "&Beenden" mit
        /// Strg+Q), "&Ansicht" (1 Kind) und "&Extras" (1 Kind) — in einem
        /// echten, gezeigten Form-Fenster (Muster: MenuControllerTests,
        /// "Disposing_the_owner_form_closes_the_mode") und ein Paint
        /// erzwungen, danach ist ItemBoundsForTests gefüllt. Ein bloßes
        /// CreateControl() reicht NICHT: ein Control ohne Elternteil bleibt
        /// ein WS_CHILD-Fenster mit Eltern-Handle NULL — Windows liefert dafür
        /// nie ein WM_PAINT, egal wie oft man Invalidate/Refresh ruft (per
        /// GetWindowLong/IsWindowVisible nachvollzogen). Erst ein echtes,
        /// gezeigtes Form macht die Bar tatsächlich sichtbar und damit
        /// malbar. Das Host-Fenster stirbt mit der Bar (Disposed-Kopplung),
        /// damit "using (var bar = TestBar())" allein reicht.</summary>
        private static MenuBar TestBar()
        {
            var datei = new MenuEntry("&Datei");
            datei.Items.Add(new MenuEntry("&Neu"));
            datei.Items.Add(new MenuEntry("&Beenden") { Shortcut = Keys.Control | Keys.Q });

            var ansicht = new MenuEntry("&Ansicht");
            ansicht.Items.Add(new MenuEntry("Eins"));

            var extras = new MenuEntry("&Extras");
            extras.Items.Add(new MenuEntry("Eins"));

            var bar = new MenuBar { Size = new Size(500, 28) };
            bar.Items.Add(datei);
            bar.Items.Add(ansicht);
            bar.Items.Add(extras);

            var host = new Form { ShowInTaskbar = false };
            host.Controls.Add(bar);
            bar.Disposed += (s, e) => host.Dispose();
            host.Show();

            bar.Refresh();
            return bar;
        }

        private static MenuEntry FindEntry(MenuBar bar, string text)
        {
            return FindEntry(bar.Items, text);
        }

        private static MenuEntry FindEntry(IList<MenuEntry> entries, string text)
        {
            foreach (var entry in entries)
            {
                if (entry.Text == text) return entry;
                var hit = FindEntry(entry.Items, text);
                if (hit != null) return hit;
            }
            return null;
        }

        [Fact]
        public void The_bar_is_not_selectable_focus_never_moves()
        {
            using (var bar = new MenuBar())
            {
                Assert.False(bar.CanSelect);
            }
        }

        [Fact]
        public void Items_lay_out_left_to_right_without_overlap()
        {
            using (var bar = TestBar())
            {
                var bounds = bar.ItemBoundsForTests();

                Assert.Equal(3, bounds.Length);
                Assert.True(bounds[0].Right <= bounds[1].Left);
                Assert.True(bounds[1].Right <= bounds[2].Left);
            }
        }

        [Fact]
        public void ProcessMnemonic_opens_the_matching_dropdown_with_the_first_entry_selected()
        {
            using (var bar = TestBar())
            {
                bool handled = bar.PerformMnemonicForTests('A');     // "&Ansicht"

                Assert.True(handled);
                Assert.True(bar.ControllerForTests.IsOpen);
                Assert.Equal(1, bar.ControllerForTests.BarIndex);
                Assert.Equal(0, bar.ControllerForTests.ContentAtForTests(0).SelectedIndex);
            }
        }

        [Fact]
        public void ProcessMnemonic_without_a_match_declines()
        {
            using (var bar = TestBar())
            {
                Assert.False(bar.PerformMnemonicForTests('Q'));
                Assert.False(bar.ControllerForTests.IsOpen);
            }
        }

        [Fact]
        public void ProcessShortcut_fires_the_entry_and_reports_handled()
        {
            using (var bar = TestBar())
            {
                bool clicked = false;
                FindEntry(bar, "&Beenden").Click += (s, e) => clicked = true;

                Assert.True(bar.ProcessShortcut(Keys.Control | Keys.Q));
                Assert.True(clicked);
                Assert.False(bar.ProcessShortcut(Keys.Control | Keys.F12));
            }
        }
    }
}
