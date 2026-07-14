using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Dpi;
using UIFramework.Core.Skinning;
using UIFramework.Core.Skinning.Skins;

namespace UIFramework.Demo
{
    /// <summary>
    /// Der Prüfstand von Auge. Was flimmert, ob der Hover sich richtig anfühlt,
    /// ob die Abstände stimmen — das sieht kein automatischer Test.
    /// </summary>
    internal sealed class MainForm : Form
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
            // PerformAutoScale intern täte. Konfliktfrei mit SkinButton/SkinLabel-
            // AutoSize: deren OnDpiChangedAfterParent setzt die Größe absolut aus
            // GetPreferredSize neu, das gewinnt unabhängig von der Aufrufreihenfolge
            // gegen jede vorherige multiplikative Skalierung.
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
            var toggleLight = new SkinButton { Text = "Heller Skin", AutoSize = true };
            toggleLight.Click += (s, e) => SkinManager.Current = new LightSkin();
            toggleLight.Location = new Point(16, 16);

            var toggleDark = new SkinButton { Text = "Dunkler Skin", AutoSize = true };
            toggleDark.Click += (s, e) => SkinManager.Current = new DarkSkin();
            toggleDark.Location = new Point(toggleLight.Right + Gap, 16);

            var disabled = new SkinButton { Text = "Deaktiviert", AutoSize = true, Enabled = false };
            disabled.Location = new Point(toggleDark.Right + Gap, 16);

            // Zeilenhöhe der ersten Reihe: die drei Schaltflächen können je nach
            // Text und Skin-Zustand unterschiedlich hoch ausfallen.
            int row1Bottom = Math.Max(toggleLight.Bottom, Math.Max(toggleDark.Bottom, disabled.Bottom));

            var hoverMe = new SkinButton { Text = "Zeig mir Hover", AutoSize = true };
            hoverMe.Location = new Point(16, row1Bottom + RowGap);

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

            root.Controls.Add(toggleLight);
            root.Controls.Add(toggleDark);
            root.Controls.Add(disabled);
            root.Controls.Add(hoverMe);
            root.Controls.Add(heading);
            root.Controls.Add(_dpiLabel);
            root.Controls.Add(nested);

            UpdateDpiLabel();
        }

        /// <summary>
        /// Skaliert einmalig alle author-gesetzten Bounds (ClientSize, das feste
        /// verschachtelte Panel, alle Point-Literale) von der 96-dpi-Entwurfsbasis
        /// auf die tatsächliche DeviceDpi dieses Fensters. Scale(SizeF) ist exakt
        /// der Aufruf, den ContainerControl.PerformAutoScale intern täte — hier
        /// von Hand ausgelöst, weil dieser Aufruf bei einem PerMonitorV2-Prozess
        /// auf .NET Framework 4.8 nachweislich (siehe Konstruktor-Kommentar oben)
        /// nicht von selbst passiert. DeviceDpi ist an dieser Stelle (nach dem
        /// Fensterhandle, vor OnShown) bereits die echte Monitor-DPI. AutoSize-
        /// Controls (SkinButton/SkinLabel) sind hiervon unberührt: die überschreiben
        /// ihre Größe ohnehin absolut über OnDpiChangedAfterParent.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DeviceDpi != 96)
            {
                float factor = DpiScale.ScaleF(1f, DeviceDpi);
                Scale(new SizeF(factor, factor));
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
