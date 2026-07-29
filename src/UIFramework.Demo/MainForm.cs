using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Controls;
using UIFramework.Core.Dpi;
using UIFramework.Core.Skinning;
using UIFramework.Core.Skinning.Skins;

namespace UIFramework.Demo
{
    /// <summary>
    /// Der Prüfstand von Auge. Was flimmert, ob der Hover sich richtig anfühlt,
    /// ob die Abstände stimmen — das sieht kein automatischer Test.
    /// </summary>
    internal sealed class MainForm : SkinnedForm
    {
        private readonly SkinLabel _dpiLabel = new SkinLabel();

        // Fester Abstand zwischen Controls in logischen Pixeln. Bewusst kein
        // DpiScale hier (das wäre DPI-Arithmetik in einer Assembly, die davon
        // frei bleiben soll) — die Reihenfolge unten misst stattdessen die
        // bereits DPI-korrekten AutoSize-Größen der Controls selbst und reiht
        // aneinander. Ein fester Pixel-Wert als Lücke bleibt bei jeder DPI ein
        // sichtbarer, wenn auch nicht perfekt skalierender, Abstand.
        private const int Gap = 8;
        private const int RowGap = 12;

        public MainForm()
        {
            // Autorengesetzte Bounds (ClientSize, das feste 400×120-Panel unten,
            // alle Point/Size-Literale) sind in logischen Pixeln bei 96 dpi
            // formuliert und skalieren von sich aus nie mit — anders als die
            // AutoSize-Buttons/-Labels oben, deren Größe aus dem tatsächlichen
            // Textbedarf folgt. Genau das Skalieren author-gesetzter Bounds ist
            // WinForms' eigene Aufgabe über AutoScaleMode/AutoScaleDimensions, hier
            // deklarativ als Entwurfs-Basis (96, 96) benannt. Per Messung (Task-15-
            // Report) reicht das deklarative Setzen bei diesem PerMonitorV2-Prozess
            // für die Erstanzeige aber NICHT: AutoScaleDimensions liest unter
            // AutoScaleMode.Dpi live die aktuelle DeviceDpi zurück statt des hier
            // gesetzten Werts, wodurch PerformAutoScale (auch explizit aufgerufen)
            // Entwurfs- und Ist-Wert stets identisch sieht und nie skaliert — ein
            // .NET-Framework-4.8-Verhalten, kein Bug in diesem Code. Deshalb ruft
            // OnLoad unten Scale(...) selbst auf, exakt der Aufruf, den
            // PerformAutoScale intern täte.
            //
            // ACHTUNG bei AutoSize-Controls (SkinButton/SkinLabel): Scale(SizeF)
            // multipliziert Bounds unconditionally für JEDES Kind der Hierarchie —
            // SkinButton/SkinLabel überschreiben SetBoundsCore nicht, es gibt also
            // keine Bremse, die eine bereits korrekte AutoSize-Größe verschont.
            // Ob diese Größe zum Zeitpunkt von OnLoad schon korrekt ist (weil vorher
            // ein echtes OnDpiChangedAfterParent gefeuert hat) oder noch auf der
            // 96-dpi-Baseline steht, lässt sich nicht verlässlich vorhersagen —
            // OnDpiChangedAfterParent feuert bei einer DPI-ÄNDERUNG, und bei der
            // erstmaligen Fenstererzeugung auf einem Nicht-96-dpi-Monitor ist
            // ungewiss, ob das überhaupt als "Änderung" zählt.
            //
            // Live an diesem Fenster gemessen (Task-Report zu diesem Fix, echter
            // Lauf bei DeviceDpi 120/125%): "Zeig mir Hover" — GetPreferredSize bei
            // 96 dpi (Testhost-Baseline) liefert 105px; korrekt EINMAL auf 120 dpi
            // skaliert (105 × 1,25) ergibt ~131px — exakt das, was ReapplyAutoSize
            // unten tatsächlich liefert (gemessen: 132px). Ohne ReapplyAutoSize
            // (Scale() allein trifft die AutoSize-Controls unconditionally mit)
            // maß derselbe Button 165px — 105 × 1,25², also ein zweites Mal mit
            // demselben Faktor multipliziert: der Doppel-Skalierungs-Defekt war in
            // diesem Prozess real reproduzierbar, nicht nur theoretisch möglich.
            // OnLoad unten behebt das, indem es AutoSize-Controls NACH Scale(...)
            // explizit und absolut aus GetPreferredSize neu setzt (ReapplyAutoSize),
            // statt sich auf die Aufrufreihenfolge mit OnDpiChangedAfterParent zu
            // verlassen.
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Text = "UIFramework — Fundament";
            ClientSize = new Size(560, 380);
            StartPosition = FormStartPosition.CenterScreen;

            var root = new SkinPanel { Dock = DockStyle.Fill };
            Controls.Add(root);

            // AutoSize=true: die Schaltflächen richten sich nach dem tatsächlichen
            // Textbedarf (SkinButton.GetPreferredSize) statt der festen 96×30-
            // Vorgabe. Ohne das schneidet der Text oberhalb von 96 dpi ab, weil
            // die feste Größe nicht mit der (korrekt skalierenden) Schrift wächst.
            //
            // Die Skin-Umschaltung (vormals toggleLight/toggleDark) ist ins
            // Menü gewandert (Spec, Menüsystem) — geblieben ist als einzige
            // Schaltfläche der ersten Reihe der bewusst deaktivierte Testknopf.
            var disabled = new SkinButton { Text = "Deaktiviert", AutoSize = true, Enabled = false };
            disabled.Location = new Point(16, 16);

            // Zeilenhöhe der ersten Reihe: nur noch dieser eine Knopf bestimmt sie.
            int row1Bottom = disabled.Bottom;

            var hoverMe = new SkinButton { Text = "Zeig mir Hover", AutoSize = true };
            hoverMe.Location = new Point(16, row1Bottom + RowGap);

            var openGrid = new SkinButton { Text = "Grid mit 1.000.000 Zeilen", AutoSize = true };
            openGrid.Click += (s, e) => new GridForm().Show();
            openGrid.Location = new Point(hoverMe.Right + Gap, row1Bottom + RowGap);

            var openEditors = new SkinButton { Text = "Editoren", AutoSize = true };
            openEditors.Click += (s, e) => new EditorForm().Show();
            openEditors.Location = new Point(openGrid.Right + Gap, row1Bottom + RowGap);

            var heading = new SkinLabel
            {
                Text = "Beschriftung in 9pt Segoe UI",
                Location = new Point(16, hoverMe.Bottom + RowGap)
            };

            _dpiLabel.Location = new Point(16, heading.Bottom + RowGap);

            var nested = new SkinPanel
            {
                Location = new Point(16, _dpiLabel.Bottom + RowGap),
                Size = new Size(400, 120)
            };
            nested.Controls.Add(new SkinLabel
            {
                Text = "Panel im Panel — prüft DisplayRectangle und Innenabstand",
                Dock = DockStyle.Top
            });

            root.Controls.Add(disabled);
            root.Controls.Add(hoverMe);
            root.Controls.Add(openGrid);
            root.Controls.Add(openEditors);
            root.Controls.Add(heading);
            root.Controls.Add(_dpiLabel);
            root.Controls.Add(nested);

            UpdateDpiLabel();

            // Menüleiste als Dock=Top-Streifen ÜBER dem Dock=Fill-Inhalt: root
            // (Fill) ist bereits oben zuerst zu Controls hinzugefügt worden,
            // die Leiste kommt jetzt danach — exakt das Muster des 2b-Filter-
            // streifens in GridForm (dort: grid zuerst, toolbar danach).
            var menuBar = new MenuBar { Dock = DockStyle.Top };

            var file = new MenuEntry("&Datei");
            var openGridEntry = new MenuEntry("&Grid-Demo");
            openGridEntry.Click += (s, e) => new GridForm().Show();
            var openEditorsEntry = new MenuEntry("&Editoren");
            openEditorsEntry.Click += (s, e) => new EditorForm().Show();
            var openRibbonEntry = new MenuEntry("&Ribbon-Demo");
            openRibbonEntry.Click += (s, e) => new RibbonForm().Show();
            var quit = new MenuEntry("&Beenden") { Shortcut = Keys.Control | Keys.Q };
            quit.Click += (s, e) => Close();
            file.Items.Add(openGridEntry);
            file.Items.Add(openEditorsEntry);
            file.Items.Add(openRibbonEntry);
            file.Items.Add(MenuEntry.Separator());
            file.Items.Add(quit);

            var view = new MenuEntry("&Ansicht");
            var lightEntry = new MenuEntry("&Heller Skin") { CheckOnClick = true, Checked = true };
            var darkEntry = new MenuEntry("&Dunkler Skin") { CheckOnClick = true };
            lightEntry.Click += (s, e) =>
            {
                // Exklusivität ist App-Sache, kein Framework-Feature (kein RadioGroup-YAGNI).
                lightEntry.Checked = true; darkEntry.Checked = false;
                SkinManager.Current = new LightSkin();
            };
            darkEntry.Click += (s, e) =>
            {
                darkEntry.Checked = true; lightEntry.Checked = false;
                SkinManager.Current = new DarkSkin();
            };
            view.Items.Add(lightEntry);
            view.Items.Add(darkEntry);

            var extras = new MenuEntry("E&xtras");
            var nested2 = new MenuEntry("&Untermenü");
            var deeper = new MenuEntry("&Tiefer");
            deeper.Items.Add(new MenuEntry("Ebene &3a"));
            deeper.Items.Add(new MenuEntry("Ebene 3&b"));
            nested2.Items.Add(deeper);
            nested2.Items.Add(new MenuEntry("Ebene &2"));
            extras.Items.Add(nested2);
            extras.Items.Add(new MenuEntry("&Gesperrt") { Enabled = false });

            menuBar.Items.Add(file);
            menuBar.Items.Add(view);
            menuBar.Items.Add(extras);

            Controls.Add(menuBar);
        }

        /// <summary>
        /// Skaliert einmalig alle author-gesetzten Bounds (ClientSize, das feste
        /// verschachtelte Panel, alle Point-Literale) von der 96-dpi-Entwurfsbasis
        /// auf die tatsächliche DeviceDpi dieses Fensters. Scale(SizeF) ist exakt
        /// der Aufruf, den ContainerControl.PerformAutoScale intern täte — hier
        /// von Hand ausgelöst, weil dieser Aufruf bei einem PerMonitorV2-Prozess
        /// auf .NET Framework 4.8 nachweislich (siehe Konstruktor-Kommentar oben)
        /// nicht von selbst passiert. DeviceDpi ist an dieser Stelle (nach dem
        /// Fensterhandle, vor OnShown) bereits die echte Monitor-DPI.
        ///
        /// AutoSize-Controls (SkinButton/SkinLabel) NICHT der Scale()-Vervielfachung
        /// überlassen (siehe Konstruktor-Kommentar): ReapplyAutoSize setzt ihre
        /// Größe danach explizit und absolut aus GetPreferredSize neu, bei der zu
        /// diesem Zeitpunkt garantiert echten DeviceDpi — unabhängig davon, ob
        /// Scale() sie soeben (fälschlich) mitskaliert hat oder nicht.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DeviceDpi != 96)
            {
                float factor = DpiScale.ScaleF(1f, DeviceDpi);
                Scale(new SizeF(factor, factor));
            }

            ReapplyAutoSize(this);
        }

        /// <summary>
        /// Setzt für jedes AutoSize-SkinButton/-SkinLabel in der Hierarchie die
        /// Größe absolut aus GetPreferredSize neu — unabhängig davon, was
        /// Control.Scale(SizeF) zuvor mit der Größe gemacht hat (siehe OnLoad).
        /// Positionen (Location) fasst das bewusst nicht an: die kommen bereits
        /// korrekt aus Scale(), das die gesamte author-gesetzte Layout-Geometrie
        /// gleichförmig mitskaliert.
        /// </summary>
        private static void ReapplyAutoSize(Control root)
        {
            foreach (Control child in root.Controls)
            {
                if (child.AutoSize && (child is SkinButton || child is SkinLabel))
                {
                    child.Size = child.GetPreferredSize(Size.Empty);
                }

                ReapplyAutoSize(child);
            }
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            UpdateDpiLabel();
            base.OnDpiChangedAfterParent(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateDpiLabel();
        }

        private void UpdateDpiLabel()
        {
            _dpiLabel.Text = "Aktuelle DPI: " + DeviceDpi + "  (" + (DeviceDpi * 100 / 96) + " %)";
        }
    }
}
