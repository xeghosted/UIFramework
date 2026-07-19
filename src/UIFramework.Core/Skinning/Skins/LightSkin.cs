using System.Drawing;
using System.Windows.Forms;

namespace UIFramework.Core.Skinning.Skins
{
    /// <summary>
    /// Heller Skin. Zusammen mit DarkSkin die EINZIGE Stelle im Framework,
    /// die Farbwerte enthalten darf — Task 14 erzwingt das maschinell.
    /// </summary>
    public sealed class LightSkin : SkinBase
    {
        private static readonly FontSpec BodyFont = new FontSpec("Segoe UI", 9f);

        private static readonly Color Surface = Color.FromArgb(255, 250, 250, 250);
        private static readonly Color SurfaceRaised = Color.FromArgb(255, 255, 255, 255);
        private static readonly Color BorderSubtle = Color.FromArgb(255, 214, 214, 218);
        private static readonly Color BorderStrong = Color.FromArgb(255, 176, 176, 182);
        private static readonly Color TextPrimary = Color.FromArgb(255, 28, 28, 32);
        private static readonly Color TextDisabled = Color.FromArgb(255, 160, 160, 166);
        private static readonly Color Accent = Color.FromArgb(255, 0, 102, 204);
        private static readonly Color AccentHover = Color.FromArgb(255, 0, 118, 234);
        private static readonly Color AccentPressed = Color.FromArgb(255, 0, 84, 168);
        private static readonly Color DisabledFill = Color.FromArgb(255, 236, 236, 239);

        public override string Name
        {
            get { return "Light"; }
        }

        public LightSkin()
        {
            DefineButton();
            DefinePanel();
            DefineLabel();
            DefineFocus();
            DefineWindow();
            DefineScrollBar();
            DefineGrid();
            DefineTextBox();
            DefineComboBox();
            DefineTab();
        }

        private void DefineButton()
        {
            Define(ElementKeys.Button, ElementState.Normal, new ElementAppearance
            {
                Background = Accent,
                BorderColor = Accent,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(12, 6, 12, 6)
            });

            Define(ElementKeys.Button, ElementState.Hovered, new ElementAppearance
            {
                Background = AccentHover,
                BorderColor = AccentHover,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(12, 6, 12, 6)
            });

            Define(ElementKeys.Button, ElementState.Pressed, new ElementAppearance
            {
                Background = AccentPressed,
                BorderColor = AccentPressed,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(12, 6, 12, 6)
            });

            Define(ElementKeys.Button, ElementState.Selected, new ElementAppearance
            {
                Background = AccentPressed,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(12, 6, 12, 6)
            });

            Define(ElementKeys.Button, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(12, 6, 12, 6)
            });
        }

        private void DefinePanel()
        {
            Define(ElementKeys.Panel, ElementState.Normal, new ElementAppearance
            {
                Background = Surface,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(6),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8)
            });

            Define(ElementKeys.Panel, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(6),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(8)
            });
        }

        private void DefineLabel()
        {
            Define(ElementKeys.Label, ElementState.Normal, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(0)
            });

            Define(ElementKeys.Label, ElementState.Disabled, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(0)
            });
        }

        private void DefineFocus()
        {
            Define(ElementKeys.Focus, ElementState.Normal, new ElementAppearance
            {
                Background = Color.Transparent,
                BorderColor = TextPrimary,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(2)
            });
        }

        private void DefineWindow()
        {
            Define(ElementKeys.Window, ElementState.Normal, new ElementAppearance
            {
                // Der Ton der Fläche darunter (siehe DefinePanel), damit die
                // Leiste optisch in den Client-Bereich übergeht.
                Background = Surface,
                BorderColor = BorderSubtle,
                // Steuert nichts — die Rahmengeometrie gehört Windows. Aber
                // BorderColor oben geht an DWMWA_BORDER_COLOR, und Windows
                // zeichnet daraufhin einen 1px-Rahmen. 0 wäre eine Falschaussage
                // gegenüber jedem, der diese Zeile liest oder anzeigt.
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(0)
            });
        }

        private void DefineScrollBar()
        {
            // Rinne: etwas abgesetzt von der Fläche, damit die Leiste als eigenes
            // Element lesbar bleibt.
            Define(ElementKeys.ScrollBar, ElementState.Normal, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(0)
            });

            Define(ElementKeys.ScrollBar, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(0)
            });

            Define(ElementKeys.ScrollBarThumb, ElementState.Normal, new ElementAppearance
            {
                Background = BorderStrong,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.ScrollBarThumb, ElementState.Hovered, new ElementAppearance
            {
                Background = TextDisabled,
                BorderColor = TextDisabled,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(2)
            });

            Define(ElementKeys.ScrollBarThumb, ElementState.Pressed, new ElementAppearance
            {
                Background = Accent,
                BorderColor = Accent,
                BorderWidth = 1,
                Corners = new CornerRadius(3),
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(2)
            });
        }

        private void DefineGrid()
        {
            // Die Fläche unter der letzten Zeile: bewusst der Grundton, nicht der
            // Zellton — so ist sichtbar, wo die Daten enden.
            Define(ElementKeys.Grid, ElementState.Normal, new ElementAppearance
            {
                Background = Surface,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(0)
            });

            Define(ElementKeys.GridHeader, ElementState.Normal, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 5, 8, 5)
            });

            Define(ElementKeys.GridHeader, ElementState.Hovered, new ElementAppearance
            {
                Background = BorderSubtle,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 5, 8, 5)
            });

            Define(ElementKeys.GridHeader, ElementState.Pressed, new ElementAppearance
            {
                Background = BorderStrong,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 5, 8, 5)
            });

            Define(ElementKeys.GridCell, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.GridCell, ElementState.Hovered, new ElementAppearance
            {
                Background = Surface,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.GridCell, ElementState.Selected, new ElementAppearance
            {
                Background = Accent,
                BorderColor = Accent,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = SurfaceRaised,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.GridCell, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = CornerRadius.None,
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });
        }

        private void DefineTextBox()
        {
            Define(ElementKeys.TextBox, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(6, 4, 6, 4)
            });

            Define(ElementKeys.TextBox, ElementState.Hovered, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(6, 4, 6, 4)
            });

            Define(ElementKeys.TextBox, ElementState.Selected, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = Accent,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(6, 4, 6, 4)
            });

            Define(ElementKeys.TextBox, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(6, 4, 6, 4)
            });
        }

        private void DefineComboBox()
        {
            Define(ElementKeys.ComboBox, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.ComboBox, ElementState.Hovered, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.ComboBox, ElementState.Selected, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = Accent,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.ComboBox, ElementState.Disabled, new ElementAppearance
            {
                Background = DisabledFill,
                BorderColor = BorderSubtle,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(8, 4, 8, 4)
            });

            Define(ElementKeys.ComboBoxList, ElementState.Normal, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = BorderStrong,
                BorderWidth = 1,
                Corners = new CornerRadius(4),
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(0)
            });
        }

        private void DefineTab()
        {
            Define(ElementKeys.Tab, ElementState.Normal, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(14, 8, 14, 8)
            });

            Define(ElementKeys.Tab, ElementState.Hovered, new ElementAppearance
            {
                Background = BorderSubtle,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextPrimary,
                Font = BodyFont,
                Padding = new Padding(14, 8, 14, 8)
            });

            Define(ElementKeys.Tab, ElementState.Selected, new ElementAppearance
            {
                Background = SurfaceRaised,
                BorderColor = Accent,
                BorderWidth = 2,
                Corners = CornerRadius.None,
                ForeColor = Accent,
                Font = BodyFont,
                Padding = new Padding(14, 8, 14, 8)
            });

            Define(ElementKeys.Tab, ElementState.Disabled, new ElementAppearance
            {
                Background = Surface,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = TextDisabled,
                Font = BodyFont,
                Padding = new Padding(14, 8, 14, 8)
            });
        }
    }
}
