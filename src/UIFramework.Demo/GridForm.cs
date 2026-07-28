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
        }

        private readonly ListDataSource<Zeile> _baseSource;
        private GridColumn _sortedColumn;
        private SortDirection _sortDirection = SortDirection.None;
        private string _filterText = "";

        public GridForm()
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Text = "UIFramework — Grid mit einer Million Zeilen";
            ClientSize = new Size(760, 460);
            StartPosition = FormStartPosition.CenterScreen;

            var grid = new GridControl { Dock = DockStyle.Fill };
            grid.Columns.Add(new GridColumn("Nummer", "Nr.") { Width = 70 });
            grid.Columns.Add(new GridColumn("Name", "Name") { Width = 160 });
            grid.Columns.Add(new GridColumn("Ort", "Ort") { Width = 140 });
            grid.Columns.Add(new GridColumn("Betrag", "Betrag") { Width = 100 });

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

            grid.HeaderClick += OnGridHeaderClick;
            RebuildDataSource(grid);
            Controls.Add(grid);

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
                RebuildDataSource(grid);
            };
            toolbar.Controls.Add(filterBox);
            Controls.Add(toolbar);
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
                    Betrag = (i % 10000) / 100m
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
