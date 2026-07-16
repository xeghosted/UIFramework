using System;
using System.Linq;
using UIFramework.Grid;
using Xunit;

namespace UIFramework.Tests.Grid
{
    public class GridColumnTests
    {
        [Fact]
        public void A_column_knows_its_key_and_header()
        {
            var column = new GridColumn("Name", "Nachname");

            Assert.Equal("Name", column.Key);
            Assert.Equal("Nachname", column.Header);
        }

        [Fact]
        public void A_null_key_is_a_programming_error()
        {
            Assert.Throws<ArgumentNullException>(() => new GridColumn(null, "Kopf"));
        }

        [Fact]
        public void Width_cannot_be_dragged_below_the_minimum()
        {
            // Sonst zieht der Anwender die Spalte auf 0 und findet sie nie wieder.
            var column = new GridColumn("Name", "Kopf");
            column.MinWidth = 24;

            column.Width = 5;

            Assert.Equal(24, column.Width);
        }

        [Fact]
        public void Raising_the_minimum_pushes_a_too_narrow_width_up()
        {
            var column = new GridColumn("Name", "Kopf");
            column.Width = 30;

            column.MinWidth = 50;

            Assert.Equal(50, column.Width);
        }

        [Fact]
        public void Changing_the_width_announces_it()
        {
            var column = new GridColumn("Name", "Kopf");
            int fired = 0;
            column.Changed += (s, e) => fired++;

            column.Width = 200;

            Assert.Equal(1, fired);
        }

        [Fact]
        public void Setting_the_width_to_what_it_already_is_announces_nothing()
        {
            var column = new GridColumn("Name", "Kopf");
            column.Width = 200;
            int fired = 0;
            column.Changed += (s, e) => fired++;

            column.Width = 200;

            Assert.Equal(0, fired);
        }

        [Fact]
        public void A_width_clamped_to_the_minimum_announces_nothing_the_second_time()
        {
            // Beim Ziehen unter die Mindestbreite kommen Dutzende Mausereignisse.
            // Ohne diesen Vergleich zeichnete das Grid jedes einzelne neu.
            var column = new GridColumn("Name", "Kopf");
            column.MinWidth = 24;
            column.Width = 5;      // -> 24, feuert
            int fired = 0;
            column.Changed += (s, e) => fired++;

            column.Width = 3;      // -> ebenfalls 24

            Assert.Equal(0, fired);
        }

        [Fact]
        public void The_collection_keeps_the_order_things_were_added_in()
        {
            var columns = new GridColumnCollection();
            columns.Add(new GridColumn("A", "A"));
            columns.Add(new GridColumn("B", "B"));

            Assert.Equal(new[] { "A", "B" }, columns.Select(c => c.Key).ToArray());
        }

        [Fact]
        public void Moving_a_column_reorders_the_collection()
        {
            var columns = new GridColumnCollection();
            columns.Add(new GridColumn("A", "A"));
            columns.Add(new GridColumn("B", "B"));
            columns.Add(new GridColumn("C", "C"));

            columns.Move(0, 2);

            Assert.Equal(new[] { "B", "C", "A" }, columns.Select(c => c.Key).ToArray());
        }

        [Fact]
        public void Moving_a_column_backwards_works_too()
        {
            var columns = new GridColumnCollection();
            columns.Add(new GridColumn("A", "A"));
            columns.Add(new GridColumn("B", "B"));
            columns.Add(new GridColumn("C", "C"));

            columns.Move(2, 0);

            Assert.Equal(new[] { "C", "A", "B" }, columns.Select(c => c.Key).ToArray());
        }

        [Fact]
        public void Moving_a_column_onto_itself_changes_nothing_and_announces_nothing()
        {
            var columns = new GridColumnCollection();
            columns.Add(new GridColumn("A", "A"));
            columns.Add(new GridColumn("B", "B"));
            int fired = 0;
            columns.Changed += (s, e) => fired++;

            columns.Move(1, 1);

            Assert.Equal(new[] { "A", "B" }, columns.Select(c => c.Key).ToArray());
            Assert.Equal(0, fired);
        }

        [Fact]
        public void Moving_outside_the_collection_is_a_programming_error()
        {
            var columns = new GridColumnCollection();
            columns.Add(new GridColumn("A", "A"));

            Assert.Throws<ArgumentOutOfRangeException>(() => columns.Move(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => columns.Move(-1, 0));
        }

        [Fact]
        public void Adding_a_column_announces_it()
        {
            var columns = new GridColumnCollection();
            int fired = 0;
            columns.Changed += (s, e) => fired++;

            columns.Add(new GridColumn("A", "A"));

            Assert.Equal(1, fired);
        }

        [Fact]
        public void The_collection_passes_on_a_change_inside_one_of_its_columns()
        {
            // Ohne das muesste GridControl sich an jede einzelne Spalte haengen
            // und beim Entfernen wieder loesen — die klassische Leckquelle.
            var columns = new GridColumnCollection();
            var column = new GridColumn("A", "A");
            columns.Add(column);

            int fired = 0;
            columns.Changed += (s, e) => fired++;

            column.Width = 300;

            Assert.Equal(1, fired);
        }

        [Fact]
        public void Two_columns_with_the_same_key_are_a_programming_error()
        {
            // Der Schluessel adressiert die Zelle bei der Datenquelle. Doppelt
            // vergeben faende GetValue stumm die falsche Spalte.
            var columns = new GridColumnCollection();
            columns.Add(new GridColumn("A", "Erste"));

            Assert.Throws<ArgumentException>(() => columns.Add(new GridColumn("A", "Zweite")));
        }

        [Fact]
        public void Adding_null_is_a_programming_error()
        {
            var columns = new GridColumnCollection();

            Assert.Throws<ArgumentNullException>(() => columns.Add(null));
        }
    }
}
