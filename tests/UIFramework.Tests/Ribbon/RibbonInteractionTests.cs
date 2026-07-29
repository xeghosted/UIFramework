using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Ribbon
{
    /// <summary>
    /// Testbett für die Item-Interaktion (Task 6): baut ein Ribbon mit je
    /// einem Element jeder relevanten Art in einer Gruppe (Form+Show-Muster
    /// wie TestRibbon in RibbonControlTests — parentless Controls malen nie,
    /// siehe Task-9-Lehre aus 4a) und hält bequeme Referenzen auf die
    /// einzelnen Items, damit die Fakten unten ihre Punkte gezielt in der
    /// Mitte der jeweiligen Box ansetzen können, statt Indizes zu erraten.
    /// Bewusst eine EIGENE Klasse statt Umbau von RibbonControlTests.TestRibbon
    /// (Auftrag: die bestehende Testklasse bleibt unangetastet).
    /// </summary>
    internal sealed class RibbonTestBed : IDisposable
    {
        public readonly Form Host;
        public readonly RibbonControl Ribbon;
        public readonly RibbonItem Button;
        public readonly RibbonItem Toggle;
        public readonly RibbonItem DropdownNoMenu;
        public readonly RibbonItem DropdownWithMenu;
        public readonly RibbonItem Disabled;
        public readonly RibbonItem SeparatorItem;

        public RibbonTestBed()
        {
            Ribbon = new RibbonControl { Width = 800 };
            var tab = new RibbonTab { Text = "Start" };
            var group = new RibbonGroup { Title = "G" };

            Button = new RibbonItem("Klick") { Size = RibbonItemSize.Small };
            Toggle = new RibbonItem("Um") { Kind = RibbonItemKind.ToggleButton, Size = RibbonItemSize.Small };
            DropdownNoMenu = new RibbonItem("Ohne") { Kind = RibbonItemKind.DropDownButton, Size = RibbonItemSize.Small };
            DropdownWithMenu = new RibbonItem("Mit") { Kind = RibbonItemKind.DropDownButton, Size = RibbonItemSize.Small };
            Disabled = new RibbonItem("Aus") { Enabled = false, Size = RibbonItemSize.Small };
            SeparatorItem = RibbonItem.Separator();

            group.Items.Add(Button);
            group.Items.Add(Toggle);
            group.Items.Add(DropdownNoMenu);
            group.Items.Add(DropdownWithMenu);
            group.Items.Add(Disabled);
            group.Items.Add(SeparatorItem);

            tab.Groups.Add(group);
            Ribbon.Tabs.Add(tab);

            Host = new Form();
            Host.Controls.Add(Ribbon);
            Host.Show();          // parentless Controls malen nie (Task-9-Lehre aus 4a)
            Ribbon.Refresh();
        }

        /// <summary>Mittelpunkt der gemalten Box eines Items — für Down/Up-Punkte.</summary>
        public Point CenterOf(RibbonItem item)
        {
            var placed = Ribbon.PlacedItemsForTests();
            for (int i = 0; i < placed.Length; i++)
            {
                if (ReferenceEquals(placed[i].Item, item))
                {
                    var b = placed[i].Bounds;
                    return new Point(b.Left + b.Width / 2, b.Top + b.Height / 2);
                }
            }
            throw new InvalidOperationException("Item wurde nicht platziert.");
        }

        /// <summary>
        /// Ein Punkt innerhalb des Client-Bereichs, der weder einen Tab-Kopf
        /// noch ein Item trifft — oben rechts, weit rechts vom (einzigen,
        /// schmalen) Tab-Kopf UND rechts von der (einzigen, schmalen) Gruppe.
        /// </summary>
        public Point EmptyPoint
        {
            get { return new Point(Ribbon.ClientSize.Width - 5, 5); }
        }

        public void Dispose()
        {
            Host.Dispose();
        }
    }

    [Collection(SkinManagerCollection.Name)]
    public class RibbonInteractionTests : IDisposable
    {
        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void Clicking_a_button_raises_click_on_mouse_up_over_the_same_item()
        {
            using (var bed = new RibbonTestBed())
            {
                int clicks = 0;
                bed.Button.Click += (s, e) => clicks++;
                var itemMitte = bed.CenterOf(bed.Button);

                bed.Ribbon.PerformMouseDownForTests(itemMitte);
                Assert.Equal(0, clicks);          // erst MouseUp führt aus

                bed.Ribbon.PerformMouseUpForTests(itemMitte);
                Assert.Equal(1, clicks);
            }
        }

        [Fact]
        public void A_toggle_button_checks_before_click()
        {
            using (var bed = new RibbonTestBed())
            {
                bool checkedDuringClick = false;
                bed.Toggle.Click += (s, e) => checkedDuringClick = bed.Toggle.Checked;
                var itemMitte = bed.CenterOf(bed.Toggle);

                bed.Ribbon.PerformMouseDownForTests(itemMitte);
                bed.Ribbon.PerformMouseUpForTests(itemMitte);

                Assert.True(bed.Toggle.Checked);
                Assert.True(checkedDuringClick);
            }
        }

        [Fact]
        public void Mouse_up_somewhere_else_cancels_the_press()
        {
            using (var bed = new RibbonTestBed())
            {
                int clicks = 0;
                bed.Button.Click += (s, e) => clicks++;
                var itemMitte = bed.CenterOf(bed.Button);

                bed.Ribbon.PerformMouseDownForTests(itemMitte);
                bed.Ribbon.PerformMouseUpForTests(bed.EmptyPoint);

                Assert.Equal(0, clicks);
                Assert.False(bed.Ribbon.HasPressForTests);
            }
        }

        [Fact]
        public void A_disabled_item_and_a_separator_never_fire()
        {
            using (var bed = new RibbonTestBed())
            {
                int clicks = 0;
                bed.Disabled.Click += (s, e) => clicks++;
                bed.SeparatorItem.Click += (s, e) => clicks++;

                var disabledMitte = bed.CenterOf(bed.Disabled);
                bed.Ribbon.PerformMouseDownForTests(disabledMitte);
                bed.Ribbon.PerformMouseUpForTests(disabledMitte);

                var separatorMitte = bed.CenterOf(bed.SeparatorItem);
                bed.Ribbon.PerformMouseDownForTests(separatorMitte);
                bed.Ribbon.PerformMouseUpForTests(separatorMitte);

                Assert.Equal(0, clicks);
            }
        }

        [Fact]
        public void A_dropdown_button_without_menu_does_nothing()
        {
            using (var bed = new RibbonTestBed())
            {
                int clicks = 0;
                bed.DropdownNoMenu.Click += (s, e) => clicks++;
                var itemMitte = bed.CenterOf(bed.DropdownNoMenu);

                bed.Ribbon.PerformMouseDownForTests(itemMitte);
                bed.Ribbon.PerformMouseUpForTests(itemMitte);   // darf nicht werfen

                Assert.Equal(0, clicks);
            }
        }

        [Fact]
        public void A_dropdown_button_opens_its_menu_below_the_item()
        {
            using (var bed = new RibbonTestBed())
            using (var menu = new PopupMenu())
            {
                menu.Items.Add(new MenuEntry("&Eins"));
                bed.DropdownWithMenu.Menu = menu;
                var itemMitte = bed.CenterOf(bed.DropdownWithMenu);

                bed.Ribbon.PerformMouseDownForTests(itemMitte);
                bed.Ribbon.PerformMouseUpForTests(itemMitte);

                // IsOpen genügt als Headless-Beweis; die tatsächliche
                // Bündigkeit unterhalb der Item-Bounds prüft die Fahrprobe
                // (Screen.FromControl/RectangleToScreen liefern im
                // Test-Prozess keine verlässliche Bildschirmgeometrie).
                Assert.True(menu.ControllerForTests.IsOpen);
                Assert.Equal(1, menu.ControllerForTests.ChainDepth);
            }
        }

        // ---- Fix-Runde 1: Klick-Zeit-Recheck (Review-Fund) ------------------

        [Fact]
        public void An_item_disabled_between_down_and_up_does_not_execute()
        {
            using (var bed = new RibbonTestBed())
            {
                int clicks = 0;
                bed.Toggle.Click += (s, e) => clicks++;
                var itemMitte = bed.CenterOf(bed.Toggle);

                bed.Ribbon.PerformMouseDownForTests(itemMitte);
                bed.Toggle.Enabled = false;   // App-Timer/Async-Callback zwischen Down und Up
                bed.Ribbon.PerformMouseUpForTests(itemMitte);

                Assert.Equal(0, clicks);
                Assert.False(bed.Toggle.Checked);
                Assert.False(bed.Ribbon.HasPressForTests);
            }
        }
    }
}
