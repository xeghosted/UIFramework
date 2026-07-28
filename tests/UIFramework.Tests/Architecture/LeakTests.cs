using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Grid;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Architecture
{
    [Collection(SkinManagerCollection.Name)]
    public class LeakTests : IDisposable
    {
        public LeakTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void A_thousand_disposed_controls_leave_nothing_registered()
        {
            for (int i = 0; i < 1000; i++)
            {
                using (var button = new SkinButton())
                using (var panel = new SkinPanel())
                using (var label = new SkinLabel())
                {
                    button.Text = "x";
                    label.Text = "y";
                    panel.Controls.Add(button);
                }
            }

            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [Fact]
        public void A_thousand_forgotten_controls_leave_nothing_registered_either()
        {
            CreateAndForget(1000);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Ohne die schwachen Referenzen stünde hier 1000 — und jedes dieser
            // Controls hinge für die Lebensdauer der App im Speicher.
            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateAndForget(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var button = new SkinButton();
                GC.KeepAlive(button);
                // Bewusst kein Dispose: prüft das Netz, nicht den Normalfall.
            }
        }

        [Fact]
        public void Switching_the_skin_a_thousand_times_does_not_grow_the_registration_list()
        {
            using (var button = new SkinButton())
            {
                for (int i = 0; i < 1000; i++)
                {
                    SkinManager.Current = new StubSkin(
                        System.Drawing.Color.FromArgb(255, i % 256, 0, 0), "Skin" + i);
                }

                Assert.Equal(1, SkinManager.RegisteredCount);
            }
        }

        [Fact]
        public void A_grid_registers_itself_and_both_of_its_scrollbars()
        {
            // Erst die Erwartung festnageln, dann ihr Verschwinden pruefen —
            // sonst bewiese der Test unten auch dann "kein Leck", wenn sich das
            // Grid nie registriert haette.
            using (var grid = new GridControl())
            {
                Assert.Equal(3, SkinManager.RegisteredCount);
            }
        }

        [Fact]
        public void A_thousand_disposed_grids_leave_nothing_registered()
        {
            for (int i = 0; i < 1000; i++)
            {
                using (var grid = new GridControl())
                {
                    grid.Columns.Add(new GridColumn("A", "A"));
                    grid.DataSource = new CountingDataSource(100);
                }
            }

            // Ohne dass WinForms die beiden Leisten mit entsorgt, stuenden hier
            // 2000 verwaiste Registrierungen.
            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [Fact]
        public void A_thousand_disposed_scrollbars_leave_nothing_registered()
        {
            for (int i = 0; i < 1000; i++)
            {
                using (var bar = new SkinScrollBar())
                {
                    bar.Maximum = 500;
                }
            }

            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [Fact]
        public void A_thousand_disposed_textboxes_leave_nothing_registered()
        {
            for (int i = 0; i < 1000; i++)
            {
                using (var box = new SkinTextBox())
                {
                    box.Text = "x";
                }
            }

            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [Fact]
        public void A_thousand_disposed_comboboxes_leave_nothing_registered()
        {
            for (int i = 0; i < 1000; i++)
            {
                using (var combo = new SkinComboBox())
                {
                    combo.Items.Add("a");
                    combo.SelectedIndex = 0;
                }
            }

            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [Fact]
        public void A_thousand_disposed_tabcontrols_leave_nothing_registered()
        {
            for (int i = 0; i < 1000; i++)
            {
                using (var tabs = new SkinTabControl())
                using (var page = new Panel())
                {
                    tabs.AddTab("A", page);
                }
            }

            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [Fact]
        public void A_thousand_disposed_spinedits_leave_nothing_registered()
        {
            for (int i = 0; i < 1000; i++)
            {
                using (var spin = new SpinEdit())
                {
                    spin.Value = 5;
                }
            }

            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [Fact]
        public void A_thousand_disposed_dateedits_leave_nothing_registered()
        {
            for (int i = 0; i < 1000; i++)
            {
                using (var edit = new DateEdit())
                {
                    edit.Value = new DateTime(2026, 7, 28);
                }
            }

            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [Fact]
        public void A_thousand_disposed_checkedits_leave_nothing_registered()
        {
            for (int i = 0; i < 1000; i++)
            {
                using (var check = new CheckEdit())
                {
                    check.Checked = true;
                }
            }

            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [Fact]
        public void Disposing_a_dateedit_with_an_open_popup_leaves_nothing_registered()
        {
            // ButtonEditBase.Dispose ruft ClosePopup() — der Weg muss auch
            // greifen, wenn das Popup beim Dispose noch offen ist (nicht nur
            // beim regulären Schließen über Wahl/Escape/Deaktivierung).
            using (var edit = new DateEdit())
            {
                edit.CreateControl();
                edit.ClickButtonForTests(0);   // öffnet den Kalender
                Assert.True(edit.IsPopupOpenForTests);
            }

            Assert.Equal(0, SkinManager.RegisteredCount);
        }
    }
}
