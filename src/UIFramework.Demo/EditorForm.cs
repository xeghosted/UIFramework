using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Controls;
using UIFramework.Core.Dpi;

namespace UIFramework.Demo
{
    /// <summary>
    /// Der Prüfstand für Teilprojekt 3a: alle fünf Editoren untereinander,
    /// jeweils mit Beschriftung. Tab-Reihenfolge = Anlagereihenfolge.
    /// </summary>
    internal sealed class EditorForm : SkinnedForm
    {
        public EditorForm()
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Text = "UIFramework — Editoren";
            ClientSize = new Size(420, 320);
            StartPosition = FormStartPosition.CenterScreen;

            var root = new SkinPanel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            Controls.Add(root);

            int y = 16;
            const int rowGap = 40;
            const int editorLeft = 150;
            const int editorWidth = 220;

            AddRow(root, "Text:", y, new SkinTextBox
            {
                PlaceholderText = "Freitext …",
                Location = new Point(editorLeft, y),
                Width = editorWidth
            });
            y += rowGap;

            AddRow(root, "Zahl (0–100):", y, new SpinEdit
            {
                MinValue = 0, MaxValue = 100, Increment = 5, Value = 25,
                Location = new Point(editorLeft, y),
                Width = editorWidth
            });
            y += rowGap;

            AddRow(root, "Datum:", y, new DateEdit
            {
                Value = DateTime.Today,
                Location = new Point(editorLeft, y),
                Width = editorWidth
            });
            y += rowGap;

            var combo = new SkinComboBox { Location = new Point(editorLeft, y), Width = editorWidth };
            combo.Items.Add("Berlin");
            combo.Items.Add("Hamburg");
            combo.Items.Add("München");
            combo.SelectedIndex = 0;
            AddRow(root, "Auswahl:", y, combo);
            y += rowGap;

            // AutoSize ist bei CheckEdit seit Teilprojekt 10 der Default (wie
            // bei SkinButton) — kein explizites Setzen nötig.
            AddRow(root, "Option:", y, new CheckEdit
            {
                Text = "aktiviert",
                Location = new Point(editorLeft, y)
            });
        }

        private static void AddRow(SkinPanel root, string caption, int y, Control editor)
        {
            root.Controls.Add(new SkinLabel
            {
                Text = caption,
                Location = new Point(16, y + 4),
                AutoSize = true
            });
            root.Controls.Add(editor);
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

            ReapplyAutoSize(this);
        }

        /// <summary>
        /// Wie MainForm.ReapplyAutoSize: SkinLabel (die Beschriftungen) und
        /// CheckEdit ("Option:") sind AutoSize=true per Default. Scale(SizeF)
        /// würde ihre bereits korrekte GetPreferredSize-Größe unconditionally
        /// ein zweites Mal mitskalieren (ausführliche Begründung im
        /// MainForm-Konstruktor-Kommentar) — deshalb hier dieselbe Korrektur.
        /// Positionen bleiben unangetastet, die kommen bereits korrekt aus
        /// Scale().
        /// </summary>
        private static void ReapplyAutoSize(Control root)
        {
            foreach (Control child in root.Controls)
            {
                if (child.AutoSize && (child is SkinLabel || child is CheckEdit))
                {
                    child.Size = child.GetPreferredSize(Size.Empty);
                }

                ReapplyAutoSize(child);
            }
        }
    }
}
