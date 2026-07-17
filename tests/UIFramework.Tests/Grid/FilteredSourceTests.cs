using System;
using System.Collections.Generic;
using System.Linq;
using UIFramework.Grid;
using Xunit;

namespace UIFramework.Tests.Grid
{
    public class FilteredSourceTests
    {
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
                return _values[rowIndex];
            }
        }

        [Fact]
        public void Only_matching_rows_pass_through_in_original_order()
        {
            var inner = new StubSource(1, 2, 3, 4, 5, 6);
            var filtered = new FilteredSource(inner, (s, i) => (int)s.GetValue(i, "V") % 2 == 0);

            var values = Enumerable.Range(0, filtered.RowCount)
                .Select(i => filtered.GetValue(i, "V")).ToArray();

            Assert.Equal(new object[] { 2, 4, 6 }, values);
        }

        [Fact]
        public void Row_count_is_the_match_count_not_the_source_size()
        {
            var inner = new StubSource(1, 2, 3, 4, 5);
            var filtered = new FilteredSource(inner, (s, i) => (int)s.GetValue(i, "V") > 3);

            Assert.Equal(2, filtered.RowCount);
        }

        [Fact]
        public void No_matches_gives_an_empty_source_rather_than_throwing()
        {
            var inner = new StubSource(1, 2, 3);
            var filtered = new FilteredSource(inner, (s, i) => false);

            Assert.Equal(0, filtered.RowCount);
        }

        [Fact]
        public void An_empty_inner_source_gives_an_empty_result()
        {
            var filtered = new FilteredSource(new StubSource(), (s, i) => true);

            Assert.Equal(0, filtered.RowCount);
        }

        [Fact]
        public void Refresh_reapplies_the_predicate_to_a_source_that_changed()
        {
            var list = new List<Person> { new Person { Age = 10 } };
            var inner = new ListDataSource<Person>(list);
            inner.Map("Age", p => p.Age);
            var filtered = new FilteredSource(inner, (s, i) => (int)s.GetValue(i, "Age") >= 18);
            Assert.Equal(0, filtered.RowCount);

            list.Add(new Person { Age = 30 });
            filtered.Refresh();

            Assert.Equal(1, filtered.RowCount);
            Assert.Equal(30, filtered.GetValue(0, "Age"));
        }

        private sealed class Person
        {
            public int Age { get; set; }
        }

        [Fact]
        public void The_predicate_is_checked_exactly_once_per_row()
        {
            var inner = new StubSource(1, 2, 3, 4, 5);
            int calls = 0;

            var filtered = new FilteredSource(inner, (s, i) => { calls++; return true; });

            Assert.Equal(5, calls);

            calls = 0;
            filtered.Refresh();
            Assert.Equal(5, calls);
        }

        [Fact]
        public void A_null_inner_source_is_a_programming_error()
        {
            Assert.Throws<ArgumentNullException>(() => new FilteredSource(null, (s, i) => true));
        }

        [Fact]
        public void A_null_predicate_is_a_programming_error()
        {
            Assert.Throws<ArgumentNullException>(() => new FilteredSource(new StubSource(), null));
        }
    }
}
