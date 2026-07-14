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

        public MainForm()
        {
            Text = "UIFramework — Fundament";
            ClientSize = new Size(560, 380);
            StartPosition = FormStartPosition.CenterScreen;

            var root = new SkinPanel { Dock = DockStyle.Fill };
            Controls.Add(root);

            var toggleLight = new SkinButton { Text = "Heller Skin", Location = new Point(16, 16) };
            toggleLight.Click += (s, e) => SkinManager.Current = new LightSkin();

            var toggleDark = new SkinButton { Text = "Dunkler Skin", Location = new Point(124, 16) };
            toggleDark.Click += (s, e) => SkinManager.Current = new DarkSkin();

            var disabled = new SkinButton { Text = "Deaktiviert", Location = new Point(232, 16), Enabled = false };

            var hoverMe = new SkinButton { Text = "Zeig mir Hover", Location = new Point(16, 60) };

            var heading = new SkinLabel { Text = "Beschriftung in 9pt Segoe UI", Location = new Point(16, 104) };

            _dpiLabel.Location = new Point(16, 132);

            var nested = new SkinPanel { Location = new Point(16, 168), Size = new Size(400, 120) };
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
