using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
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
