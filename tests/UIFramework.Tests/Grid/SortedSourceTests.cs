using System;
using System.Collections.Generic;
using System.Linq;
using UIFramework.Grid;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Grid
{
    public class SortedSourceTests
    {
        /// <summary>
        /// Eine Quelle mit zwei Spalten: "Value" zum Sortieren, "Order" haelt
        /// die urspruengliche Reihenfolge fest -- damit sich Stabilitaet bei
        /// gleichen Werten pruefen laesst.
        /// </summary>
        private sealed class StubSource : IGridDataSource
        {
            private readonly object[] _values;

            public StubSource(params object[] values)
            {
                _values = values;
            }

            public int RowCount
            {
                get { return _values.Length; }
            }

            public object GetValue(int rowIndex, string columnKey)
            {
                if (columnKey == "Value") return _values[rowIndex];
                return rowIndex;   // "Order": die urspruengliche Position
            }
        }

        [Fact]
        public void Ascending_sorts_numbers()
        {
            var inner = new StubSource(30, 10, 20);
            var sorted = new SortedSource(inner);

            sorted.Sort("Value", SortDirection.Ascending);

            Assert.Equal(new object[] { 10, 20, 30 },
                Enumerable.Range(0, 3).Select(i => sorted.GetValue(i, "Value")).ToArray());
        }

        [Fact]
        public void Descending_sorts_numbers()
        {
            var inner = new StubSource(30, 10, 20);
            var sorted = new SortedSource(inner);

            sorted.Sort("Value", SortDirection.Descending);

            Assert.Equal(new object[] { 30, 20, 10 },
                Enumerable.Range(0, 3).Select(i => sorted.GetValue(i, "Value")).ToArray());
        }

        [Fact]
        public void Ascending_sorts_strings()
        {
            var inner = new StubSource("Grace", "Ada", "Barbara");
            var sorted = new SortedSource(inner);

            sorted.Sort("Value", SortDirection.Ascending);

            Assert.Equal(new object[] { "Ada", "Barbara", "Grace" },
                Enumerable.Range(0, 3).Select(i => sorted.GetValue(i, "Value")).ToArray());
        }

        [Fact]
        public void None_restores_the_original_order()
        {
            var inner = new StubSource(30, 10, 20);
            var sorted = new SortedSource(inner);
            sorted.Sort("Value", SortDirection.Ascending);

            sorted.Sort(null, SortDirection.None);

            Assert.Equal(new object[] { 30, 10, 20 },
                Enumerable.Range(0, 3).Select(i => sorted.GetValue(i, "Value")).ToArray());
        }

        [Fact]
        public void Equal_keys_keep_their_original_relative_order()
        {
            // Drei Zeilen mit demselben Wert -- eine instabile Sortierung
            // wuerfelte sie bei jedem Aufruf neu durcheinander.
            var inner = new StubSource(5, 5, 5, 1);
            var sorted = new SortedSource(inner);

            sorted.Sort("Value", SortDirection.Ascending);

            // Die drei Fuenfen muessen in Order 0,1,2 bleiben (ihre urspruengliche
            // Reihenfolge), nach der Eins.
            var order = Enumerable.Range(0, 4).Select(i => sorted.GetValue(i, "Order")).ToArray();
            Assert.Equal(new object[] { 3, 0, 1, 2 }, order);
        }

        [Fact]
        public void An_empty_source_does_not_throw()
        {
            var sorted = new SortedSource(new StubSource());

            sorted.Sort("Value", SortDirection.Ascending);

            Assert.Equal(0, sorted.RowCount);
        }

        [Fact]
        public void A_single_row_sorts_trivially()
        {
            var sorted = new SortedSource(new StubSource(42));

            sorted.Sort("Value", SortDirection.Descending);

            Assert.Equal(42, sorted.GetValue(0, "Value"));
        }

        [Fact]
        public void Row_count_always_follows_the_inner_source()
        {
            var list = new List<Person> { new Person { Age = 3 } };
            var inner = new ListDataSource<Person>(list);
            inner.Map("Age", p => p.Age);
            var sorted = new SortedSource(inner);
            sorted.Sort("Age", SortDirection.Ascending);

            list.Add(new Person { Age = 1 });

            // Die Permutation ist nach dem Wachstum veraltet (Laenge 1 statt 2)
            // und muss beim naechsten Zugriff automatisch neu aufgebaut werden --
            // ohne Wurf, ohne dass jemand erneut Sort(...) ruft.
            Assert.Equal(2, sorted.RowCount);
            Assert.Equal(1, sorted.GetValue(0, "Age"));
            Assert.Equal(3, sorted.GetValue(1, "Age"));
        }

        private sealed class Person
        {
            public int Age { get; set; }
        }

        [Fact]
        public void Sorting_reads_every_row_of_the_source_exactly_once()
        {
            // Der Kern der Sache: Sortieren MUSS jede Zeile lesen (das ist der
            // Preis), darf sie aber nicht mehrfach lesen (das waere ein Fehler).
            var source = new CountingDataSource(1000);
            var sorted = new SortedSource(source);

            sorted.Sort("Nummer", SortDirection.Ascending);

            Assert.Equal(1000, source.TouchedRowCount);
            Assert.Equal(1000, source.GetValueCalls);
        }

        [Fact]
        public void Reading_after_sorting_touches_only_the_requested_rows()
        {
            var source = new CountingDataSource(1000);
            var sorted = new SortedSource(source);
            sorted.Sort("Nummer", SortDirection.Ascending);
            source.Reset();

            sorted.GetValue(0, "Nummer");
            sorted.GetValue(1, "Nummer");
            sorted.GetValue(2, "Nummer");

            Assert.Equal(3, source.GetValueCalls);
            Assert.InRange(source.TouchedRowCount, 1, 3);
        }

        [Fact]
        public void Sort_direction_and_column_are_reported()
        {
            var sorted = new SortedSource(new StubSource(1, 2));

            sorted.Sort("Value", SortDirection.Descending);

            Assert.Equal("Value", sorted.SortColumnKey);
            Assert.Equal(SortDirection.Descending, sorted.Direction);
        }

        [Fact]
        public void A_null_inner_source_is_a_programming_error()
        {
            Assert.Throws<ArgumentNullException>(() => new SortedSource(null));
        }

        [Fact]
        public void Sorting_by_a_null_column_with_a_real_direction_is_a_programming_error()
        {
            var sorted = new SortedSource(new StubSource(1, 2));

            Assert.Throws<ArgumentNullException>(() => sorted.Sort(null, SortDirection.Ascending));
        }
    }
}
