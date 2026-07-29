using System;
using System.Collections.Generic;
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
    /// Der Prüfstand für Teilprojekt 4b1: ein Ribbon mit allen Item-Arten
    /// (großer/kleiner Knopf, Separator, ToggleButton, DropDownButton,
    /// deaktiviertes Element mit Bild, deaktivierter Tab) über drei Tabs.
    /// Jeder Klick trägt sich ins Protokoll-Label ein — dieselbe Sichtbarkeit,
    /// die MainForm für Hover/DPI bietet, hier für Ribbon-Interaktion.
    /// </summary>
    internal sealed class RibbonForm : SkinnedForm
    {
        private readonly SkinLabel _log = new SkinLabel();
        private readonly PopupMenu _zoomMenu = new PopupMenu();

        // Laufzeit-Icons (Task 7, kein Ressourcen-Reservoir nötig): das
        // Framework disposed App-Bilder nie (RibbonItem.Image gehört der
        // App, siehe RibbonItem-Doku) — diese Liste hält alle selbst
        // gemalten Bitmaps fest, damit Dispose sie wieder freigeben kann.
        private readonly List<Image> _icons = new List<Image>();

        public RibbonForm()
        {
            // Exakt das MainForm-Muster (siehe dortiger Konstruktor-Kommentar
            // für die ausführliche Begründung): AutoScaleDimensions/-Mode hier
            // nur als Entwurfs-Basis deklariert, das eigentliche Skalieren
            // übernimmt OnLoad unten von Hand.
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Text = "UIFramework — Ribbon";
            ClientSize = new Size(900, 420);
            StartPosition = FormStartPosition.CenterScreen;

            _log.Text = "Zuletzt: —";
            _log.Location = new Point(16, 16);

            var body = new SkinPanel { Dock = DockStyle.Fill };
            body.Controls.Add(_log);
            Controls.Add(body);

            var ribbon = new RibbonControl { Dock = DockStyle.Top };
            BuildTabs(ribbon);

            // Dock=Top-Streifen ÜBER dem Dock=Fill-Inhalt: body (Fill) ist
            // bereits oben zuerst zu Controls hinzugefügt worden, das Ribbon
            // kommt jetzt danach — dasselbe Muster wie MainForm (menuBar)
            // und GridForm (toolbar).
            Controls.Add(ribbon);
        }

        private void BuildTabs(RibbonControl ribbon)
        {
            // ---- Tab "Start" ------------------------------------------------
            var ablage = new RibbonGroup("Ablage");

            var neu = new RibbonItem("Neu") { Image = Track(MakeIcon(Color.SeaGreen, 'N')) };
            neu.Click += (s, e) => Log("Neu");

            var oeffnen = new RibbonItem("Öffnen") { Image = Track(MakeIcon(Color.SteelBlue, 'Ö')) };
            oeffnen.Click += (s, e) => Log("Öffnen");

            var speichern = new RibbonItem("Speichern") { Size = RibbonItemSize.Small };
            speichern.Click += (s, e) => Log("Speichern");

            var drucken = new RibbonItem("Drucken") { Size = RibbonItemSize.Small };
            drucken.Click += (s, e) => Log("Drucken");

            var schliessen = new RibbonItem("Schließen") { Size = RibbonItemSize.Small };
            schliessen.Click += (s, e) => Log("Schließen");

            var verwerfen = new RibbonItem("Verwerfen")
            {
                Enabled = false,
                Image = Track(MakeIcon(Color.Firebrick, 'V'))
            };
            verwerfen.Click += (s, e) => Log("Verwerfen");

            ablage.Items.Add(neu);
            ablage.Items.Add(oeffnen);
            ablage.Items.Add(speichern);
            ablage.Items.Add(drucken);
            ablage.Items.Add(schliessen);
            ablage.Items.Add(RibbonItem.Separator());
            ablage.Items.Add(verwerfen);

            var ansicht = new RibbonGroup("Ansicht");

            var toggleDark = new RibbonItem("Dunkel")
            {
                Kind = RibbonItemKind.ToggleButton,
                Image = Track(MakeIcon(Color.DimGray, 'D'))
            };
            toggleDark.Click += (s, e) =>
            {
                // Checked steht zum Zeitpunkt dieses Handlers bereits auf dem
                // NEUEN Zustand (RibbonControl.Execute togglet vor PerformClick).
                SkinManager.Current = toggleDark.Checked ? (ISkin)new DarkSkin() : new LightSkin();
                Log("Dunkel");
            };

            var zoom50 = new MenuEntry("&50 %");
            zoom50.Click += (s, e) => Log("Zoom 50 %");
            var zoom100 = new MenuEntry("&100 %");
            zoom100.Click += (s, e) => Log("Zoom 100 %");
            var zoom200 = new MenuEntry("&200 %");
            zoom200.Click += (s, e) => Log("Zoom 200 %");
            _zoomMenu.Items.Add(zoom50);
            _zoomMenu.Items.Add(zoom100);
            _zoomMenu.Items.Add(zoom200);

            var zoomItem = new RibbonItem("Zoom")
            {
                Kind = RibbonItemKind.DropDownButton,
                Image = Track(MakeIcon(Color.DarkOrange, 'Z')),
                Menu = _zoomMenu
            };

            ansicht.Items.Add(toggleDark);
            ansicht.Items.Add(zoomItem);

            var start = new RibbonTab("Start");
            start.Groups.Add(ablage);
            start.Groups.Add(ansicht);

            // ---- Tab "Extras" ------------------------------------------------
            var werkzeuge = new RibbonGroup("Werkzeuge");

            var pinsel = new RibbonItem("Pinsel")
            {
                Size = RibbonItemSize.Small,
                Image = Track(MakeIcon(Color.MediumPurple, 'P'))
            };
            pinsel.Click += (s, e) => Log("Pinsel");

            var radierer = new RibbonItem("Radierer")
            {
                Size = RibbonItemSize.Small,
                Image = Track(MakeIcon(Color.Goldenrod, 'R'))
            };
            radierer.Click += (s, e) => Log("Radierer");

            var lupe = new RibbonItem("Lupe")
            {
                Size = RibbonItemSize.Small,
                Image = Track(MakeIcon(Color.Teal, 'L'))
            };
            lupe.Click += (s, e) => Log("Lupe");

            werkzeuge.Items.Add(pinsel);
            werkzeuge.Items.Add(radierer);
            werkzeuge.Items.Add(lupe);

            var extras = new RibbonTab("Extras");
            extras.Groups.Add(werkzeuge);

            // ---- Tab "Gesperrt" (deaktiviert, ohne Gruppen) -------------------
            var gesperrt = new RibbonTab("Gesperrt") { Enabled = false };

            ribbon.Tabs.Add(start);
            ribbon.Tabs.Add(extras);
            ribbon.Tabs.Add(gesperrt);
        }

        private void Log(string name)
        {
            _log.Text = "Zuletzt: " + name;
        }

        /// <summary>Merkt sich eine selbst gemalte Bitmap für Dispose.</summary>
        private Bitmap Track(Bitmap bitmap)
        {
            _icons.Add(bitmap);
            return bitmap;
        }

        // 32x32-Basisbild reicht: der Painter skaliert auf die DPI-echte Zone.
        private static Bitmap MakeIcon(Color fill, char letter)
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(fill)) g.FillEllipse(brush, 2, 2, 28, 28);
                TextRenderer.DrawText(g, letter.ToString(), new Font("Segoe UI", 14f, FontStyle.Bold),
                    new Rectangle(0, 0, 32, 32), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            return bmp;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _zoomMenu.Dispose();
                foreach (var icon in _icons) icon.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Wie MainForm: Der PerMonitorV2-Prozess skaliert autorengesetzte
        /// Bounds auf .NET Framework 4.8 nicht von selbst (siehe die
        /// ausführliche Begründung in MainForm.OnLoad).
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
        /// Wie MainForm.ReapplyAutoSize: _log (SkinLabel) ist AutoSize=true
        /// per Default. Scale(SizeF) würde seine bereits korrekte
        /// GetPreferredSize-Größe unconditionally ein zweites Mal
        /// mitskalieren (ausführliche Begründung im MainForm-Konstruktor-
        /// Kommentar) — deshalb hier dieselbe Korrektur. Das Ribbon selbst
        /// braucht das nicht: seine Höhe verwaltet RibbonControl bereits
        /// selbst gegen genau diesen Doppel-Skalierungs-Defekt (siehe
        /// RibbonControl.OnLayout).
        /// </summary>
        private static void ReapplyAutoSize(Control root)
        {
            foreach (Control child in root.Controls)
            {
                if (child.AutoSize && child is SkinLabel)
                {
                    child.Size = child.GetPreferredSize(Size.Empty);
                }

                ReapplyAutoSize(child);
            }
        }
    }
}
