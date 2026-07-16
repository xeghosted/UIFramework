using System;
using System.Collections.Generic;
using UIFramework.Grid;
using Xunit;

namespace UIFramework.Tests.Grid
{
    public class ListDataSourceTests
    {
        private sealed class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        private static ListDataSource<Person> TwoPeople()
        {
            var source = new ListDataSource<Person>(new List<Person>
            {
                new Person { Name = "Ada", Age = 36 },
                new Person { Name = "Grace", Age = 45 }
            });

            source.Map("Name", p => p.Name);
            source.Map("Age", p => p.Age);
            return source;
        }

        [Fact]
        public void The_row_count_follows_the_list()
        {
            Assert.Equal(2, TwoPeople().RowCount);
        }

        [Fact]
        public void A_mapped_column_returns_the_value_of_that_row()
        {
            var source = TwoPeople();

            Assert.Equal("Grace", source.GetValue(1, "Name"));
            Assert.Equal(36, source.GetValue(0, "Age"));
        }

        [Fact]
        public void The_row_count_reflects_a_list_that_changed_after_construction()
        {
            // Die Quelle haelt die Liste, sie kopiert sie nicht. Sonst zeigte das
            // Grid einen Stand von vorgestern.
            var list = new List<Person>();
            var source = new ListDataSource<Person>(list);

            list.Add(new Person { Name = "Ada" });

            Assert.Equal(1, source.RowCount);
        }

        [Fact]
        public void An_unmapped_column_is_a_programming_error()
        {
            var source = TwoPeople();

            // Nicht null zurueckgeben: Eine leere Spalte im Grid saehe aus wie
            // fehlende Daten. Der Tippfehler im Schluessel soll auffallen.
            Assert.Throws<ArgumentException>(() => source.GetValue(0, "Nmae"));
        }

        [Fact]
        public void A_row_index_outside_the_list_is_a_programming_error()
        {
            var source = TwoPeople();

            Assert.Throws<ArgumentOutOfRangeException>(() => source.GetValue(2, "Name"));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.GetValue(-1, "Name"));
        }

        [Fact]
        public void A_null_list_is_a_programming_error()
        {
            Assert.Throws<ArgumentNullException>(() => new ListDataSource<Person>(null));
        }

        [Fact]
        public void Mapping_the_same_key_twice_replaces_the_accessor()
        {
            var source = TwoPeople();

            source.Map("Name", p => p.Age);

            Assert.Equal(36, source.GetValue(0, "Name"));
        }

        [Fact]
        public void A_null_value_in_the_data_comes_back_as_null()
        {
            // Das Grid muss damit umgehen; die Quelle darf es nicht verschlucken.
            var source = new ListDataSource<Person>(new List<Person> { new Person { Name = null } });
            source.Map("Name", p => p.Name);

            Assert.Null(source.GetValue(0, "Name"));
        }
    }
}
