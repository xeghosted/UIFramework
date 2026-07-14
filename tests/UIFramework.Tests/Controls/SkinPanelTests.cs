using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;
using UIFramework.Core.Skinning.Skins;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class SkinPanelTests : IDisposable
    {
        /// <summary>
        /// Anders als StubSkin: definiert eine Erscheinung ausschließlich für
        /// ElementKeys.Panel. StubSkin weist Button, Panel, Label und Focus
        /// dieselbe Instanz zu, sodass ein Test darauf bestünde, egal welcher
        /// dieser Schlüssel von SkinPanel.ElementKey zurückgegeben würde. Mit
        /// diesem Skin fällt ein falscher Schlüssel auf SkinBase.FallbackAppearance
        /// zurück und der Test schlägt sichtbar fehl.
        /// </summary>
        private sealed class PanelOnlySkin : SkinBase
        {
            public static readonly Color PanelColor = Color.FromArgb(255, 44, 55, 66);

            public PanelOnlySkin()
            {
                Define(ElementKeys.Panel, ElementState.Normal, new ElementAppearance
                {
                    Background = PanelColor,
                    BackgroundGradientEnd = null,
                    BorderColor = Color.Transparent,
                    BorderWidth = 0,
                    Corners = CornerRadius.None,
                    ForeColor = Color.FromArgb(255, 255, 255, 255),
                    Font = new FontSpec("Segoe UI", 9f),
                    Padding = new Padding(4)
                });
            }

            public override string Name
            {
                get { return "PanelOnly"; }
            }
        }

        public SkinPanelTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void The_display_rectangle_is_inset_by_the_skin_padding()
        {
            // PanelOnlySkin definiert Padding(4) auf allen Seiten, kein Rahmen.
            SkinManager.Current = new PanelOnlySkin();

            using (var panel = new SkinPanel())
            {
                panel.Size = new Size(100, 100);

                var display = panel.DisplayRectangle;

                Assert.Equal(4, display.Left);
                Assert.Equal(4, display.Top);
                Assert.Equal(92, display.Width);
                Assert.Equal(92, display.Height);
            }
        }

        [Fact]
        public void A_docked_child_lands_inside_the_padding()
        {
            SkinManager.Current = new PanelOnlySkin();

            using (var panel = new SkinPanel())
            {
                panel.Size = new Size(100, 100);

                var child = new Control { Dock = DockStyle.Fill };
                panel.Controls.Add(child);
                panel.PerformLayout();

                Assert.Equal(4, child.Left);
                Assert.Equal(4, child.Top);
                Assert.Equal(92, child.Width);
            }
        }

        [Fact]
        public void It_paints_the_panel_background_of_the_current_skin()
        {
            SkinManager.Current = new PanelOnlySkin();

            using (var panel = new SkinPanel())
            {
                panel.Size = new Size(60, 60);

                using (var bitmap = new Bitmap(60, 60))
                {
                    panel.DrawToBitmap(bitmap, new Rectangle(0, 0, 60, 60));

                    Assert.Equal(PanelOnlySkin.PanelColor.ToArgb(), bitmap.GetPixel(30, 30).ToArgb());
                }
            }
        }

        [Fact]
        public void A_panel_is_not_a_tab_stop()
        {
            using (var panel = new SkinPanel())
            {
                // Ein Container ist kein Bedienelement.
                Assert.False(panel.TabStop);
            }
        }

        /// <summary>
        /// Regression für die zweite Hälfte des DPI-Defekts aus dem Demo-Programm
        /// (Task-15-Report, Abschnitt "Fix: Autorengesetzte Größen skalieren mit
        /// der DPI"): das verschachtelte Panel in MainForm hat eine autoren-
        /// gesetzte, feste Größe (400×120) — anders als die AutoSize-Buttons folgt
        /// diese Größe nicht aus Inhalt, sondern ist eine reine Entwurfsvorgabe.
        /// Padding und Rahmenbreite des Skins skalieren mit der DPI (korrekt), die
        /// Panelgröße selbst aber nicht — der Innenraum SCHRUMPFT also relativ zur
        /// (ebenfalls mit der DPI wachsenden) Beschriftung, exakt umgekehrt zu dem,
        /// was nötig wäre.
        ///
        /// MainForm behebt das, indem es die gesamte Fensterhierarchie einmalig per
        /// <see cref="Control.Scale(SizeF)"/> mit dem Faktor dpi/96 skaliert (siehe
        /// MainForm.OnLoad) — demselben Aufruf, den WinForms' eigenes
        /// ContainerControl.PerformAutoScale intern täte. Dieser Test ruft exakt
        /// dieselbe WinForms-API mit demselben Faktor direkt auf einem Panel auf,
        /// das die Demo-Größe/den Demo-Text nachbildet, und prüft dieselbe
        /// Interior-vs-Bedarf-Rechnung wie die manuelle Probe aus dem Report.
        ///
        /// Bewusst kein Test über eine echte <see cref="Control.DeviceDpi"/> &gt; 96:
        /// siehe SkinButtonTests.AutoSize_grows_the_button_wide_enough_for_its_caption
        /// für das dort dokumentierte, hier erneut bestätigte Experiment — DeviceDpi
        /// ist im xUnit-Testhost-Prozess nicht pro Instanz überschreibbar. Anders als
        /// der Button-Fix lässt sich der Panel-Fix aber OHNE DeviceDpi vollständig
        /// ehrlich testen: sowohl Control.Scale(SizeF) als auch
        /// SkinPainter.GetContentRectangle/MeasureText nehmen die DPI als
        /// expliziten Parameter bzw. Faktor entgegen, unabhängig von
        /// Control.DeviceDpi/Prozess-Awareness — exakt der Mechanismus, den
        /// MainForm.OnLoad tatsächlich verwendet.
        /// </summary>
        [Theory]
        [InlineData(120)]
        [InlineData(144)]
        public void Scaling_the_panel_like_MainForm_does_leaves_room_for_the_nested_caption(int dpi)
        {
            const string nestedLabelText = "Panel im Panel — prüft DisplayRectangle und Innenabstand";

            SkinManager.Current = new LightSkin();
            var panelAppearance = SkinManager.Current.GetAppearance(ElementKeys.Panel, ElementState.Normal);
            var labelAppearance = SkinManager.Current.GetAppearance(ElementKeys.Label, ElementState.Normal);

            using (var panel = new SkinPanel())
            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                panel.Location = new Point(16, 16);
                panel.Size = new Size(400, 120);

                // Derselbe Aufruf wie MainForm.OnLoad: Faktor dpi/96, auf die
                // gesamte author-gesetzte Bounds-Hierarchie angewandt.
                float factor = dpi / 96f;
                panel.Scale(new SizeF(factor, factor));

                var interior = SkinPainter.GetContentRectangle(panel.ClientRectangle, panelAppearance, dpi);
                var measured = SkinPainter.MeasureText(g, nestedLabelText, labelAppearance, dpi);
                var needed = SkinPainter.InflateByPadding(measured, labelAppearance, dpi);

                Assert.True(interior.Width >= needed.Width,
                    string.Format(
                        "Panel-Innenraum ist bei {0} dpi {1}px breit, die Beschriftung braucht aber {2}px — sie würde abgeschnitten.",
                        dpi, interior.Width, needed.Width));
            }
        }

        /// <summary>
        /// Gegenprobe: ohne die Skalierung aus MainForm.OnLoad (unskaliertes
        /// 400×120-Panel) reicht der Innenraum bei 120/144 dpi nicht — das ist der
        /// ursprünglich gemeldete Defekt. Beweist, dass der obige Test wirklich die
        /// Skalierung prüft und nicht zufällig auch ohne sie bestünde.
        /// </summary>
        [Theory]
        [InlineData(120)]
        [InlineData(144)]
        public void Without_scaling_the_fixed_400_wide_panel_is_too_narrow_for_the_nested_caption(int dpi)
        {
            const string nestedLabelText = "Panel im Panel — prüft DisplayRectangle und Innenabstand";

            SkinManager.Current = new LightSkin();
            var panelAppearance = SkinManager.Current.GetAppearance(ElementKeys.Panel, ElementState.Normal);
            var labelAppearance = SkinManager.Current.GetAppearance(ElementKeys.Label, ElementState.Normal);

            using (var panel = new SkinPanel())
            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                panel.Size = new Size(400, 120);

                var interior = SkinPainter.GetContentRectangle(panel.ClientRectangle, panelAppearance, dpi);
                var measured = SkinPainter.MeasureText(g, nestedLabelText, labelAppearance, dpi);
                var needed = SkinPainter.InflateByPadding(measured, labelAppearance, dpi);

                Assert.True(interior.Width < needed.Width,
                    "Diese Gegenprobe sollte bei " + dpi + " dpi ohne Skalierung fehlschlagen (Innenraum reicht nicht) " +
                    "-- tut sie das nicht mehr, hat sich die Textmetrik oder das Padding so verändert, dass der " +
                    "ursprüngliche Defekt gar nicht mehr reproduzierbar ist.");
            }
        }
    }
}
