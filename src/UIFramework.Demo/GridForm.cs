using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
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

            var source = new ListDataSource<Zeile>(BuildRows(1000000));
            source.Map("Nummer", z => z.Nummer);
            source.Map("Name", z => z.Name);
            source.Map("Ort", z => z.Ort);
            source.Map("Betrag", z => z.Betrag.ToString("N2"));

            grid.DataSource = source;
            Controls.Add(grid);
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
