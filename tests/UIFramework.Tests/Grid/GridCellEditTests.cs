using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Grid;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Grid
{
    [Collection(SkinManagerCollection.Name)]
    public class GridCellEditTests : IDisposable
    {
        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        private sealed class Person
        {
            public string Name;
            public int Rang;
        }

        private static List<Person> People(int count)
        {
            var people = new List<Person>();
            for (int i = 0; i < count; i++)
                people.Add(new Person { Name = "P" + i, Rang = i });
            return people;
        }

        private static ListDataSource<Person> Writable(List<Person> people)
        {
            var source = new ListDataSource<Person>(people);
            source.Map("Name", p => p.Name);
            source.Map("Rang", p => p.Rang);
            source.MapSet("Name", (p, v) => p.Name = (string)v);
            return source;
        }

        /// <summary>Nur-Lese-Quelle: implementiert bewusst NUR IGridDataSource —
        /// eine ListDataSource ohne MapSet wäre die falsche Fixture, sie ist als
        /// Typ immer schreibbar (dieselbe Falle wie in WritableSourceTests,
        /// Plan-Korrektur nach Task-4-NEEDS_CONTEXT).</summary>
        private sealed class ReadOnlySource : IGridDataSource
        {
            public int RowCount { get { return 5; } }
            public object GetValue(int rowIndex, string columnKey) { return "r" + rowIndex; }
        }

        private static GridControl Grid(IGridDataSource source)
        {
            var grid = new GridControl();
            grid.Columns.Add(new GridColumn("Name", "Name")
            {
                Width = 120,
                EditorFactory = () => new SkinTextBox()
            });
            grid.Columns.Add(new GridColumn("Rang", "Rang") { Width = 60 });
            grid.DataSource = source;
            return grid;
        }

        [Fact]
        public void BeginEdit_places_the_editor_exactly_over_the_cell_and_loads_the_value()
        {
            var people = People(10);
            using (var grid = Grid(Writable(people)))
            {
                grid.BeginEdit(2, 0);

                Assert.True(grid.IsEditing);
                var editor = grid.CurrentEditorForTests;
                Assert.Equal("P2", editor.EditValue);
                Assert.Equal(grid.CellBounds(2, 0), editor.EditorControl.Bounds);
                Assert.Contains(editor.EditorControl, grid.Controls.Cast<System.Windows.Forms.Control>());
            }
        }

        [Fact]
        public void CommitEdit_writes_through_sort_and_filter_to_the_right_inner_row()
        {
            var people = People(10);

            // Nur ungerade Ränge, absteigend sortiert: sichtbar P9,P7,P5,P3,P1.
            var filtered = new FilteredSource(Writable(people), (s, i) => (int)s.GetValue(i, "Rang") % 2 == 1);
            var sorted = new SortedSource(filtered);
            sorted.Sort("Rang", SortDirection.Descending);

            using (var grid = Grid(sorted))
            {
                grid.BeginEdit(1, 0);                       // sichtbare Zeile 1 = P7
                grid.CurrentEditorForTests.EditValue = "Umbenannt";
                grid.CommitEdit();

                Assert.False(grid.IsEditing);
                Assert.Equal("Umbenannt", people[7].Name);   // innere Zeile 7
                Assert.Equal("P9", people[9].Name);          // Nachbarn unberührt
                Assert.Equal("P5", people[5].Name);
            }
        }

        [Fact]
        public void CancelEdit_closes_without_writing()
        {
            var people = People(5);
            using (var grid = Grid(Writable(people)))
            {
                grid.BeginEdit(1, 0);
                grid.CurrentEditorForTests.EditValue = "Verworfen";
                grid.CancelEdit();

                Assert.False(grid.IsEditing);
                Assert.Equal("P1", people[1].Name);
            }
        }

        [Fact]
        public void Cells_without_source_write_access_column_factory_or_with_readonly_never_activate()
        {
            var people = People(5);

            using (var grid = Grid(new ReadOnlySource()))
            {
                grid.BeginEdit(0, 0);                        // Quelle nicht schreibbar
                Assert.False(grid.IsEditing);
            }

            using (var grid = Grid(Writable(people)))
            {
                grid.BeginEdit(0, 1);                        // Spalte ohne Fabrik
                Assert.False(grid.IsEditing);

                grid.Columns[0].ReadOnly = true;
                grid.BeginEdit(0, 0);                        // Spalte gesperrt
                Assert.False(grid.IsEditing);
            }
        }

        [Fact]
        public void Opening_a_second_edit_commits_the_first()
        {
            var people = People(5);
            using (var grid = Grid(Writable(people)))
            {
                grid.BeginEdit(0, 0);
                grid.CurrentEditorForTests.EditValue = "Erster";
                grid.BeginEdit(2, 0);

                Assert.Equal("Erster", people[0].Name);      // Commit, kein Verwerfen
                Assert.Equal(2, grid.EditRowForTests);
            }
        }

        [Fact]
        public void Switching_the_data_source_discards_the_open_editor_without_writing()
        {
            var people = People(5);
            using (var grid = Grid(Writable(people)))
            {
                grid.BeginEdit(0, 0);
                grid.CurrentEditorForTests.EditValue = "NieGeschrieben";
                grid.DataSource = Writable(People(3));

                Assert.False(grid.IsEditing);
                Assert.Equal("P0", people[0].Name);
            }
        }

        [Fact]
        public void A_throwing_source_lets_the_exception_escape_and_keeps_the_editor_open()
        {
            var people = People(3);
            var source = new ListDataSource<Person>(people);
            source.Map("Name", p => p.Name);
            source.MapSet("Name", (p, v) => throw new InvalidOperationException("App-Quelle"));

            using (var grid = Grid(source))
            {
                grid.BeginEdit(0, 0);
                Assert.Throws<InvalidOperationException>(() => grid.CommitEdit());
                Assert.True(grid.IsEditing);   // ehrlich offen geblieben, nichts verschluckt
            }
        }

        [Fact]
        public void F2_edits_the_first_editable_column_of_the_current_row()
        {
            var people = People(10);
            using (var grid = Grid(Writable(people)))
            {
                grid.Selection.Select(3);
                grid.PerformKey(Keys.F2);

                Assert.True(grid.IsEditing);
                Assert.Equal(3, grid.EditRowForTests);
                Assert.Equal(0, grid.EditColumnForTests);   // "Rang" hat keine Fabrik
            }
        }

        [Fact]
        public void Typing_starts_editing_and_seeds_the_text()
        {
            var people = People(10);
            using (var grid = Grid(Writable(people)))
            {
                grid.Selection.Select(1);
                grid.PerformTyping('n');

                Assert.True(grid.IsEditing);
                Assert.Equal("n", grid.CurrentEditorForTests.EditValue);
            }
        }

        [Fact]
        public void Typing_without_a_current_row_does_nothing()
        {
            var people = People(10);
            using (var grid = Grid(Writable(people)))
            {
                grid.PerformTyping('n');
                Assert.False(grid.IsEditing);
            }
        }

        [Fact]
        public void Clicking_another_cell_commits_the_open_editor()
        {
            var people = People(10);
            using (var grid = Grid(Writable(people)))
            {
                grid.BeginEdit(0, 0);
                grid.CurrentEditorForTests.EditValue = "Geklickt";
                grid.PerformClick(new Point(10, grid.CellBounds(4, 0).Top + 2), Keys.None);

                Assert.False(grid.IsEditing);
                Assert.Equal("Geklickt", people[0].Name);
            }
        }
    }
}
