using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Controls;
using UIFramework.Core.Dpi;
using UIFramework.Grid;

namespace UIFramework.Demo
{
    /// <summary>
    /// Der Prüfstand für Teilprojekt 2a. Eine Million Zeilen — nicht als Angeberei,
    /// sondern weil erst diese Zahl beweist, dass wirklich nichts materialisiert
    /// wird. Bei tausend Zeilen fiele ein kaputte Virtualisierung nicht auf.
    ///
    /// Die Liste hält eine Million Objekte, das ist der Punkt: Nicht die Quelle
    /// ist virtuell, sondern das Zeichnen.
    /// </summary>
    internal sealed class GridForm : SkinnedForm
    {
        private sealed class Zeile
        {
            public int Nummer { get; set; }
            public string Name { get; set; }
            public string Ort { get; set; }
            public decimal Betrag { get; set; }
            public bool Aktiv { get; set; }
        }

        private readonly ListDataSource<Zeile> _baseSource;
        private GridColumn _sortedColumn;
        private SortDirection _sortDirection = SortDirection.None;
        private string _filterText = "";

        // Feld statt lokaler Variable: CopySelectedRow (Kontextmenü) braucht das
        // Grid außerhalb des Konstruktors.
        private readonly GridControl _grid;
        private readonly PopupMenu _rowMenu = new PopupMenu();

        public GridForm()
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Text = "UIFramework — Grid mit einer Million Zeilen";
            ClientSize = new Size(760, 460);
            StartPosition = FormStartPosition.CenterScreen;

            _grid = new GridControl { Dock = DockStyle.Fill };
            _grid.Columns.Add(new GridColumn("Nummer", "Nr.") { Width = 70, ReadOnly = true });
            _grid.Columns.Add(new GridColumn("Name", "Name")
            {
                Width = 160,
                EditorFactory = () => new SkinTextBox()
            });
            _grid.Columns.Add(new GridColumn("Ort", "Ort")
            {
                Width = 140,
                EditorFactory = () =>
                {
                    var combo = new SkinComboBox();
                    foreach (var ort in new[] { "Berlin", "Hamburg", "München", "Köln", "Zürich", "Wien" })
                        combo.Items.Add(ort);
                    return combo;
                }
            });
            _grid.Columns.Add(new GridColumn("Betrag", "Betrag")
            {
                Width = 100,
                // Betrag liegt 0..99,99 (i%10000/100) — die Grenzen sind die der Daten.
                EditorFactory = () => new SpinEdit { MinValue = 0, MaxValue = 100, Increment = 1 }
            });
            _grid.Columns.Add(new GridColumn("Aktiv", "Aktiv")
            {
                Width = 70,
                EditorFactory = () => new CheckEdit { Text = "an" }
            });

            _baseSource = new ListDataSource<Zeile>(BuildRows(1000000));
            _baseSource.Map("Nummer", z => z.Nummer);
            _baseSource.Map("Name", z => z.Name);
            _baseSource.Map("Ort", z => z.Ort);
            // "N2" liesse "10.00" lexikografisch vor "2.50" sortieren --
            // SortedSource vergleicht die formatierte ZEICHENKETTE, nicht die
            // Zahl dahinter (siehe Spec 2b: kein Anzeige-/Sortierwert-Splitting
            // in IGridDataSource.GetValue). Fest zweistellig gepolstert bleibt
            // die Zeichenkettenordnung deckungsgleich mit der Zahlenordnung.
            _baseSource.Map("Betrag", z => z.Betrag.ToString("00.00"));
            _baseSource.Map("Aktiv", z => z.Aktiv);
            _baseSource.MapSet("Name", (z, v) => z.Name = (string)v);
            _baseSource.MapSet("Ort", (z, v) => z.Ort = (string)(v ?? ""));
            _baseSource.MapSet("Betrag", (z, v) => z.Betrag = (decimal)v);
            _baseSource.MapSet("Aktiv", (z, v) => z.Aktiv = (bool)v);

            _grid.HeaderClick += OnGridHeaderClick;
            RebuildDataSource(_grid);
            Controls.Add(_grid);

            // Kontextmenü am Grid: Zeile kopieren, Auswahl aufheben. Gleiche
            // Konstruktion wie die Menüleiste in MainForm (Task 11), nur ohne
            // Leiste — PopupMenu statt MenuBar.
            var copyRow = new MenuEntry("Zeile &kopieren");
            copyRow.Click += (s, e) => CopySelectedRow();
            var clearSelection = new MenuEntry("Auswahl &aufheben");
            // GridControl hat kein ClearSelection() (im Brief nur als Platzhalter
            // genannt) — die echte API sitzt auf GridControl.Selection
            // (GridSelection.Clear()), siehe GridSelection.cs.
            clearSelection.Click += (s, e) => _grid.Selection.Clear();
            _rowMenu.Items.Add(copyRow);
            _rowMenu.Items.Add(MenuEntry.Separator());
            _rowMenu.Items.Add(clearSelection);

            _grid.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                    _rowMenu.Show(_grid, _grid.PointToScreen(e.Location));
            };

            // Dock=Top statt einer festen Location: Ein absolut plaziertern
            // Knopf ueber dem Dock=Fill-Grid verdeckte sonst dauerhaft dessen
            // Kopfzeile und die erste Datenzeile (am echten Fenster gefunden,
            // kein Test sieht Layout-Ueberlappung). Docking reserviert dem
            // Knopf seinen eigenen Streifen, das Grid fuellt den Rest darunter.
            var toolbar = new SkinPanel { Dock = DockStyle.Top, Height = 40 };

            var filterBox = new SkinTextBox
            {
                PlaceholderText = "Ort filtern (Enter)",
                Location = new Point(8, 6),
                Width = 200
            };
            // Bestätigen (Enter/Fokusverlust) statt je Tastendruck: Der Filteraufbau
            // läuft synchron über eine Million Zeilen (dokumentierter Einmal-Preis,
            // Spec 2b) — pro Zeichen wäre das eine fühlbare Bremse.
            filterBox.EditConfirmed += (s, e) =>
            {
                if (_filterText == filterBox.Text) return;
                _filterText = filterBox.Text;
                RebuildDataSource(_grid);
            };
            toolbar.Controls.Add(filterBox);
            Controls.Add(toolbar);
        }

        /// <summary>
        /// Kopiert die Zellwerte der selektierten Zeile Tab-getrennt in die
        /// Zwischenablage — Spaltenreihenfolge wie im Grid sichtbar. Über die
        /// echte Selektion-API geprüft (GridSelection.IsSelected), nicht nur
        /// CurrentRow: Nach einem Strg-Klick, der die zuletzt berührte Zeile
        /// gerade ABwählt, zeigt CurrentRow noch auf sie, obwohl sie nicht
        /// mehr ausgewählt ist (siehe GridSelection.Toggle) — ohne die Prüfung
        /// kopierte das eine bereits abgewählte Zeile.
        /// </summary>
        private void CopySelectedRow()
        {
            int row = _grid.Selection.CurrentRow;
            if (row < 0 || !_grid.Selection.IsSelected(row)) return;

            var source = _grid.DataSource;
            if (source == null) return;

            var columns = _grid.Columns;
            var values = new string[columns.Count];
            for (int c = 0; c < columns.Count; c++)
            {
                object value = source.GetValue(row, columns[c].Key);
                values[c] = value == null ? "" : value.ToString();
            }

            Clipboard.SetText(string.Join("\t", values));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _rowMenu.Dispose();
            base.Dispose(disposing);
        }

        private static List<Zeile> BuildRows(int count)
        {
            string[] namen = { "Ada", "Grace", "Alan", "Edsger", "Barbara", "Donald", "Niklaus" };
            string[] orte = { "Berlin", "Hamburg", "München", "Köln", "Zürich", "Wien" };

            var rows = new List<Zeile>(count);
            for (int i = 0; i < count; i++)
            {
                rows.Add(new Zeile
                {
                    Nummer = i,
                    Name = namen[i % namen.Length] + " " + i,
                    Ort = orte[i % orte.Length],
                    Betrag = (i % 10000) / 100m,
                    Aktiv = i % 2 == 0
                });
            }
            return rows;
        }

        private void OnGridHeaderClick(object sender, int columnIndex)
        {
            var grid = (GridControl)sender;
            var column = grid.Columns[columnIndex];

            if (ReferenceEquals(column, _sortedColumn))
            {
                // Zyklus: Aufsteigend -> Absteigend -> Keine -> Aufsteigend.
                _sortDirection = _sortDirection == SortDirection.Ascending
                    ? SortDirection.Descending
                    : _sortDirection == SortDirection.Descending
                        ? SortDirection.None
                        : SortDirection.Ascending;
            }
            else
            {
                StripArrow(_sortedColumn);
                _sortedColumn = column;
                _sortDirection = SortDirection.Ascending;
            }

            if (_sortDirection == SortDirection.None)
            {
                // Dritter Klick auf dieselbe Spalte: Ohne dieses Strippen bliebe
                // der Pfeil im Kopftext stehen, obwohl _sortedColumn gleich auf
                // null faellt und RebuildDataSource den sortierten Zweig dann
                // gar nicht mehr betritt (Bug im urspruenglichen Entwurf).
                StripArrow(column);
                _sortedColumn = null;
            }

            RebuildDataSource(grid);
        }

        private void RebuildDataSource(GridControl grid)
        {
            IGridDataSource source = _baseSource;

            if (!string.IsNullOrEmpty(_filterText))
            {
                string needle = _filterText;
                source = new FilteredSource(source, (s, i) =>
                {
                    var ort = (string)s.GetValue(i, "Ort");
                    return ort.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
                });
            }

            if (_sortedColumn != null && _sortDirection != SortDirection.None)
            {
                var sorted = new SortedSource(source);
                sorted.Sort(_sortedColumn.Key, _sortDirection);
                source = sorted;

                _sortedColumn.Header = BaseHeader(_sortedColumn) +
                    (_sortDirection == SortDirection.Ascending ? " ▲" : " ▼");
            }

            grid.DataSource = source;
        }

        private static void StripArrow(GridColumn column)
        {
            if (column == null) return;
            column.Header = BaseHeader(column);
        }

        private static string BaseHeader(GridColumn column)
        {
            int space = column.Header.IndexOf(' ');
            return space < 0 ? column.Header : column.Header.Substring(0, space);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Wie MainForm: Der PerMonitorV2-Prozess skaliert autorengesetzte
            // Bounds auf .NET Framework 4.8 nicht von selbst (siehe die
            // ausführliche Begründung in MainForm.OnLoad).
            if (DeviceDpi != 96)
            {
                float factor = DpiScale.ScaleF(1f, DeviceDpi);
                Scale(new SizeF(factor, factor));
            }
        }
    }
}
