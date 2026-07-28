using System;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class CellEditorContractTests : IDisposable
    {
        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void A_textbox_editor_round_trips_its_value_as_text()
        {
            using (var edit = new SkinTextBox())
            {
                IGridCellEditor editor = edit;
                editor.EditValue = "Hamburg";
                Assert.Equal("Hamburg", editor.EditValue);
                Assert.Same(edit, editor.EditorControl);
            }
        }

        [Fact]
        public void A_spin_editor_reads_unconfirmed_text_without_mutating_its_value()
        {
            using (var spin = new SpinEdit { MinValue = 0, MaxValue = 100, Value = 25 })
            {
                IGridCellEditor editor = spin;
                spin.SetTextForTests("50");

                Assert.Equal(50m, editor.EditValue);   // liest den getippten Stand …
                Assert.Equal(25m, spin.Value);         // … ohne den Wert zu bestätigen
            }
        }

        [Fact]
        public void A_spin_editor_accepts_a_boxed_decimal_and_a_string()
        {
            using (var spin = new SpinEdit { MinValue = 0, MaxValue = 100 })
            {
                IGridCellEditor editor = spin;

                editor.EditValue = 40m;
                Assert.Equal(40m, spin.Value);

                editor.EditValue = "60";
                Assert.Equal(60m, spin.Value);
            }
        }

        [Fact]
        public void A_date_editor_maps_null_and_dates_both_ways()
        {
            using (var date = new DateEdit())
            {
                IGridCellEditor editor = date;

                editor.EditValue = new DateTime(2026, 7, 15);
                Assert.Equal(new DateTime(2026, 7, 15), date.Value);

                editor.EditValue = null;
                Assert.Null(date.Value);
                Assert.Null(editor.EditValue);
            }
        }

        [Fact]
        public void A_combo_editor_selects_the_matching_item()
        {
            using (var combo = new SkinComboBox())
            {
                combo.Items.Add("Berlin");
                combo.Items.Add("Hamburg");

                IGridCellEditor editor = combo;
                editor.EditValue = "Hamburg";

                Assert.Equal(1, combo.SelectedIndex);
                Assert.Equal("Hamburg", editor.EditValue);
            }
        }

        [Fact]
        public void A_check_editor_maps_booleans()
        {
            using (var check = new CheckEdit())
            {
                IGridCellEditor editor = check;
                editor.EditValue = true;
                Assert.True(check.Checked);
                Assert.Equal(true, editor.EditValue);
            }
        }

        [Fact]
        public void BeginWith_seeds_the_text_core_and_is_harmless_elsewhere()
        {
            using (var edit = new SkinTextBox { Text = "alt" })
            using (var check = new CheckEdit())
            {
                ((IGridCellEditor)edit).BeginWith("n");
                Assert.Equal("n", edit.Text);

                ((IGridCellEditor)check).BeginWith("n");   // darf nicht werfen
                Assert.False(check.Checked);
            }
        }

        [Fact]
        public void Escape_in_the_core_requests_cancel_and_enter_requests_confirm()
        {
            using (var edit = new SkinTextBox())
            {
                IGridCellEditor editor = edit;
                int confirmed = 0, cancelled = 0;
                editor.ConfirmRequested += (s, e) => confirmed++;
                editor.CancelRequested += (s, e) => cancelled++;

                edit.ConfirmForTests();
                edit.CancelForTests();

                Assert.Equal(1, confirmed);
                Assert.Equal(1, cancelled);
            }
        }

        [Fact]
        public void A_check_editor_requests_confirm_on_enter_and_cancel_on_escape()
        {
            using (var check = new CheckEdit())
            {
                IGridCellEditor editor = check;
                int confirmed = 0, cancelled = 0;
                editor.ConfirmRequested += (s, e) => confirmed++;
                editor.CancelRequested += (s, e) => cancelled++;

                check.PerformKey(Keys.Enter);
                check.PerformKey(Keys.Escape);

                Assert.Equal(1, confirmed);
                Assert.Equal(1, cancelled);
            }
        }
    }
}
