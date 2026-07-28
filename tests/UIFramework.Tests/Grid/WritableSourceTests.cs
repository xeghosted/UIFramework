using System;
using System.Collections.Generic;
using UIFramework.Grid;
using Xunit;

namespace UIFramework.Tests.Grid
{
    public class WritableSourceTests
    {
        private sealed class Person
        {
            public string Name;
            public int Rang;
        }

        private static List<Person> People()
        {
            return new List<Person>
            {
                new Person { Name = "Ada",    Rang = 3 },
                new Person { Name = "Grace",  Rang = 1 },
                new Person { Name = "Alan",   Rang = 4 },
                new Person { Name = "Edsger", Rang = 2 }
            };
        }

        private static ListDataSource<Person> Writable(List<Person> people)
        {
            var source = new ListDataSource<Person>(people);
            source.Map("Name", p => p.Name);
            source.Map("Rang", p => p.Rang);
            source.MapSet("Name", (p, v) => p.Name = (string)v);
            return source;
        }

        [Fact]
        public void Writing_through_sort_and_filter_hits_the_right_inner_row()
        {
            var people = People();

            // Filter: Rang gerade raus -> sichtbar bleiben Ada(3) und Grace(1).
            var filtered = new FilteredSource(Writable(people),
                (s, i) => (int)s.GetValue(i, "Rang") % 2 == 1);
            var sorted = new SortedSource(filtered);
            sorted.Sort("Rang", SortDirection.Ascending);   // Grace(1) vor Ada(3)

            ((IWritableGridDataSource)sorted).SetValue(0, "Name", "Grande");

            Assert.Equal("Grande", people[1].Name);          // Grace ist innere Zeile 1
            Assert.Equal("Ada", people[0].Name);             // niemand sonst angefasst
            Assert.Equal("Alan", people[2].Name);
            Assert.Equal("Edsger", people[3].Name);
        }

        [Fact]
        public void A_column_without_a_setter_throws()
        {
            var source = Writable(People());
            Assert.Throws<ArgumentException>(
                () => ((IWritableGridDataSource)source).SetValue(0, "Rang", 9));
        }

        [Fact]
        public void A_decorator_over_a_read_only_inner_source_throws_on_write()
        {
            var readOnly = new ListDataSource<Person>(People());
            readOnly.Map("Name", p => p.Name);

            var sorted = new SortedSource(readOnly);

            Assert.Throws<InvalidOperationException>(
                () => ((IWritableGridDataSource)sorted).SetValue(0, "Name", "x"));
        }

        [Fact]
        public void Writing_out_of_range_throws()
        {
            var source = Writable(People());
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ((IWritableGridDataSource)source).SetValue(99, "Name", "x"));
        }
    }
}
