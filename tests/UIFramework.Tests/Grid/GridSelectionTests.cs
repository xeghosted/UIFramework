using UIFramework.Grid;
using Xunit;

namespace UIFramework.Tests.Grid
{
    public class GridSelectionTests
    {
        [Fact]
        public void A_fresh_selection_has_nothing_and_no_current_row()
        {
            var selection = new GridSelection();

            Assert.Equal(0, selection.Count);
            Assert.Equal(-1, selection.CurrentRow);
            Assert.False(selection.IsSelected(0));
        }

        [Fact]
        public void A_plain_click_selects_exactly_one_row()
        {
            var selection = new GridSelection();

            selection.Select(5);

            Assert.Equal(1, selection.Count);
            Assert.True(selection.IsSelected(5));
            Assert.Equal(5, selection.CurrentRow);
        }

        [Fact]
        public void A_plain_click_replaces_whatever_was_selected()
        {
            var selection = new GridSelection();
            selection.Select(1);
            selection.Toggle(2);

            selection.Select(9);

            Assert.Equal(1, selection.Count);
            Assert.True(selection.IsSelected(9));
            Assert.False(selection.IsSelected(1));
        }

        [Fact]
        public void A_plain_click_sets_the_anchor()
        {
            var selection = new GridSelection();

            selection.Select(5);

            Assert.Equal(5, selection.AnchorRow);
        }

        [Fact]
        public void Ctrl_click_adds_without_dropping_the_rest()
        {
            var selection = new GridSelection();
            selection.Select(1);

            selection.Toggle(3);

            Assert.Equal(2, selection.Count);
            Assert.True(selection.IsSelected(1));
            Assert.True(selection.IsSelected(3));
        }

        [Fact]
        public void Ctrl_click_on_a_selected_row_removes_it_again()
        {
            var selection = new GridSelection();
            selection.Select(1);
            selection.Toggle(3);

            selection.Toggle(3);

            Assert.Equal(1, selection.Count);
            Assert.False(selection.IsSelected(3));
        }

        [Fact]
        public void Ctrl_click_moves_the_anchor_to_where_it_happened()
        {
            // Sonst spannte ein folgendes Umschalt-Klick vom falschen Punkt aus.
            var selection = new GridSelection();
            selection.Select(1);

            selection.Toggle(7);

            Assert.Equal(7, selection.AnchorRow);
        }

        [Fact]
        public void Shift_click_spans_from_the_anchor_downwards()
        {
            var selection = new GridSelection();
            selection.Select(2);

            selection.ExtendTo(5);

            Assert.Equal(4, selection.Count);
            Assert.True(selection.IsSelected(2));
            Assert.True(selection.IsSelected(5));
            Assert.False(selection.IsSelected(1));
            Assert.False(selection.IsSelected(6));
        }

        [Fact]
        public void Shift_click_spans_upwards_too()
        {
            var selection = new GridSelection();
            selection.Select(5);

            selection.ExtendTo(2);

            Assert.Equal(4, selection.Count);
            Assert.True(selection.IsSelected(2));
            Assert.True(selection.IsSelected(5));
        }

        [Fact]
        public void Shift_click_leaves_the_anchor_where_it_was()
        {
            // Das ist der Sinn eines Ankers: Zweimal Umschalt-Klick spannt
            // beide Male vom selben Punkt.
            var selection = new GridSelection();
            selection.Select(5);

            selection.ExtendTo(2);

            Assert.Equal(5, selection.AnchorRow);
        }

        [Fact]
        public void A_second_shift_click_replaces_the_span_instead_of_adding_to_it()
        {
            var selection = new GridSelection();
            selection.Select(5);
            selection.ExtendTo(8);

            selection.ExtendTo(6);

            Assert.Equal(2, selection.Count);   // 5 und 6
            Assert.False(selection.IsSelected(8));
        }

        [Fact]
        public void Shift_click_moves_the_current_row_but_not_the_anchor()
        {
            var selection = new GridSelection();
            selection.Select(5);

            selection.ExtendTo(2);

            Assert.Equal(2, selection.CurrentRow);
            Assert.Equal(5, selection.AnchorRow);
        }

        [Fact]
        public void Shift_click_without_an_anchor_behaves_like_a_plain_click()
        {
            var selection = new GridSelection();

            selection.ExtendTo(4);

            Assert.Equal(1, selection.Count);
            Assert.True(selection.IsSelected(4));
            Assert.Equal(4, selection.AnchorRow);
        }

        [Fact]
        public void Clearing_drops_everything_including_the_anchor()
        {
            var selection = new GridSelection();
            selection.Select(3);

            selection.Clear();

            Assert.Equal(0, selection.Count);
            Assert.Equal(-1, selection.CurrentRow);
            Assert.Equal(-1, selection.AnchorRow);
        }

        [Fact]
        public void A_shrinking_source_drops_rows_that_no_longer_exist()
        {
            // Passiert beim Filtern (2b). Bliebe Zeile 9 ausgewaehlt, zeigte
            // das Grid eine Auswahl auf einer Zeile, die es nicht mehr gibt.
            var selection = new GridSelection();
            selection.Select(2);
            selection.Toggle(9);

            selection.TrimTo(5);

            Assert.Equal(1, selection.Count);
            Assert.True(selection.IsSelected(2));
            Assert.False(selection.IsSelected(9));
        }

        [Fact]
        public void A_shrinking_source_pulls_back_the_current_row_and_anchor()
        {
            var selection = new GridSelection();
            selection.Select(9);

            selection.TrimTo(5);

            Assert.Equal(-1, selection.CurrentRow);
            Assert.Equal(-1, selection.AnchorRow);
        }

        [Fact]
        public void Trimming_to_zero_empties_everything()
        {
            var selection = new GridSelection();
            selection.Select(1);

            selection.TrimTo(0);

            Assert.Equal(0, selection.Count);
        }

        [Fact]
        public void Trimming_that_changes_nothing_announces_nothing()
        {
            var selection = new GridSelection();
            selection.Select(2);
            int fired = 0;
            selection.Changed += (s, e) => fired++;

            selection.TrimTo(100);

            Assert.Equal(0, fired);
        }

        [Fact]
        public void Selecting_announces_a_change()
        {
            var selection = new GridSelection();
            int fired = 0;
            selection.Changed += (s, e) => fired++;

            selection.Select(1);

            Assert.Equal(1, fired);
        }

        [Fact]
        public void Selecting_the_row_that_is_already_the_only_one_announces_nothing()
        {
            // Sonst zeichnete jeder Klick auf dieselbe Zeile alles neu.
            var selection = new GridSelection();
            selection.Select(1);
            int fired = 0;
            selection.Changed += (s, e) => fired++;

            selection.Select(1);

            Assert.Equal(0, fired);
        }

        [Fact]
        public void Clearing_an_empty_selection_announces_nothing()
        {
            var selection = new GridSelection();
            int fired = 0;
            selection.Changed += (s, e) => fired++;

            selection.Clear();

            Assert.Equal(0, fired);
        }

        [Fact]
        public void A_negative_row_is_ignored_rather_than_selected()
        {
            // GridHitTest liefert -1 fuer "dort liegt nichts". Wer das ungeprueft
            // weiterreicht, waehlte Zeile -1 aus.
            var selection = new GridSelection();

            selection.Select(-1);

            Assert.Equal(0, selection.Count);
            Assert.Equal(-1, selection.CurrentRow);
        }
    }
}
