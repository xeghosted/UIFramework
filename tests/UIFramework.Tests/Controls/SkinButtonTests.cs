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
    public class SkinButtonTests : IDisposable
    {
        /// <summary>
        /// Anders als StubSkin: pro Zustand eine andere Farbe. StubSkin definiert
        /// für jedes Element nur Normal, sodass GetAppearance über die
        /// Rückfallkette (state → Normal) für jeden Zustand dieselbe Farbe liefert —
        /// ein Test darauf würde bestehen, egal welcher Zustand aufgelöst wurde.
        /// Dieser Skin unterscheidet sich pro Zustand, damit ein falsch
        /// aufgelöster Zustand eine sichtbar falsche Farbe ergibt.
        /// </summary>
        private sealed class PerStateButtonSkin : SkinBase
        {
            public static readonly Color NormalColor = Color.FromArgb(255, 10, 20, 30);
            public static readonly Color HoveredColor = Color.FromArgb(255, 40, 50, 60);
            public static readonly Color PressedColor = Color.FromArgb(255, 70, 80, 90);
            public static readonly Color DisabledColor = Color.FromArgb(255, 100, 110, 120);

            public PerStateButtonSkin()
            {
                Define(ElementKeys.Button, ElementState.Normal, Appearance(NormalColor));
                Define(ElementKeys.Button, ElementState.Hovered, Appearance(HoveredColor));
                Define(ElementKeys.Button, ElementState.Pressed, Appearance(PressedColor));
                Define(ElementKeys.Button, ElementState.Disabled, Appearance(DisabledColor));
            }

            public override string Name
            {
                get { return "PerState"; }
            }

            private static ElementAppearance Appearance(Color background)
            {
                return new ElementAppearance
                {
                    Background = background,
                    BackgroundGradientEnd = null,
                    BorderColor = Color.Transparent,
                    BorderWidth = 0,
                    Corners = CornerRadius.None,
                    ForeColor = Color.FromArgb(255, 255, 255, 255),
                    Font = new FontSpec("Segoe UI", 9f),
                    Padding = new Padding(4)
                };
            }
        }

        /// <summary>
        /// Definiert nur Button (undurchsichtiger Hintergrund, kein Rahmen, kein
        /// Padding) und Focus (transparenter Hintergrund, Rahmenbreite 2, kein
        /// Padding) — genug, um den Fokusring isoliert auf der Kante zu prüfen,
        /// ohne dass ein eigener Button-Rahmen oder Padding das Ergebnis verfälscht.
        /// </summary>
        private sealed class SingleColorFocusSkin : SkinBase
        {
            public SingleColorFocusSkin(Color buttonBackground, Color focusColor)
            {
                Define(ElementKeys.Button, ElementState.Normal, new ElementAppearance
                {
                    Background = buttonBackground,
                    BackgroundGradientEnd = null,
                    BorderColor = Color.Transparent,
                    BorderWidth = 0,
                    Corners = CornerRadius.None,
                    ForeColor = Color.FromArgb(255, 255, 255, 255),
                    Font = new FontSpec("Segoe UI", 9f),
                    Padding = new Padding(0)
                });

                Define(ElementKeys.Focus, ElementState.Normal, new ElementAppearance
                {
                    Background = Color.Transparent,
                    BackgroundGradientEnd = null,
                    BorderColor = focusColor,
                    BorderWidth = 2,
                    Corners = CornerRadius.None,
                    ForeColor = focusColor,
                    Font = new FontSpec("Segoe UI", 9f),
                    Padding = new Padding(0)
                });
            }

            public override string Name
            {
                get { return "SingleColorFocus"; }
            }
        }

        /// <summary>
        /// Zählt Invalidate-Aufrufe über das echte Invalidated-Ereignis von
        /// Control — dafür braucht das Control ein Fensterhandle, sonst verwirft
        /// Control.Invalidate() den Aufruf stillschweigend, ohne das Ereignis
        /// auszulösen.
        /// </summary>
        private sealed class InvalidationCountingButton : SkinButton
        {
            public int InvalidationCount { get; private set; }

            protected override void OnInvalidated(InvalidateEventArgs e)
            {
                InvalidationCount++;
                base.OnInvalidated(e);
            }
        }

        public SkinButtonTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void It_paints_the_button_background_of_the_current_skin()
        {
            SkinManager.Current = new StubSkin(Color.FromArgb(255, 11, 22, 33));

            using (var button = new SkinButton())
            {
                button.Size = new Size(80, 30);
                button.Text = "";

                using (var bitmap = new Bitmap(80, 30))
                {
                    button.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 30));

                    Assert.Equal(Color.FromArgb(255, 11, 22, 33).ToArgb(), bitmap.GetPixel(40, 15).ToArgb());
                }
            }
        }

        [Fact]
        public void Text_actually_lands_on_the_pixels()
        {
            SkinManager.Current = new StubSkin(Color.FromArgb(255, 0, 0, 0));

            using (var button = new SkinButton())
            {
                // AutoSize ist seit diesem Fix Standard: ohne das Abschalten würde
                // der Textwechsel unten die explizit gesetzte Größe sofort wieder
                // überschreiben (siehe SkinButton.OnTextChanged) — dieser Test will
                // aber gezielt eine 120x30-Fläche prüfen, nicht das Autosizing.
                button.AutoSize = false;
                button.Size = new Size(120, 30);
                button.Text = "MMMMMMMM";

                using (var bitmap = new Bitmap(120, 30))
                {
                    button.DrawToBitmap(bitmap, new Rectangle(0, 0, 120, 30));

                    // StubSkin zeichnet weißen Text auf schwarzem Grund: irgendwo
                    // muss ein nicht-schwarzes Pixel sein.
                    bool foundText = false;
                    for (int x = 0; x < 120 && !foundText; x++)
                    {
                        for (int y = 0; y < 30 && !foundText; y++)
                        {
                            if (bitmap.GetPixel(x, y).ToArgb() != Color.FromArgb(255, 0, 0, 0).ToArgb())
                                foundText = true;
                        }
                    }

                    Assert.True(foundText, "Es wurde kein Text gezeichnet.");
                }
            }
        }

        [Fact]
        public void The_default_alignment_is_centred()
        {
            using (var button = new SkinButton())
            {
                Assert.Equal(ContentAlignment.MiddleCenter, button.TextAlignment);
            }
        }

        /// <summary>
        /// Spiegelt SkinLabel.AutoSize, das ebenfalls standardmäßig an ist: gleicher
        /// Mechanismus (GetPreferredSize über SkinPainter), kein Grund für
        /// unterschiedliche Vorgaben. Ohne diesen Standard schneidet der Text mit
        /// TextFormatFlags.EndEllipsis oberhalb von 96 dpi ab, siehe
        /// AutoSize_grows_the_button_wide_enough_for_its_caption unten.
        /// </summary>
        [Fact]
        public void AutoSize_defaults_to_true()
        {
            using (var button = new SkinButton())
            {
                Assert.True(button.AutoSize);
            }
        }

        [Fact]
        public void Changing_the_alignment_repaints()
        {
            using (var button = new InvalidationCountingButton())
            {
                // Handle erzwingen: ohne Fensterhandle verwirft Control.Invalidate()
                // den Aufruf, bevor OnInvalidated feuert (siehe InvalidationCountingButton).
                var forceHandle = button.Handle;
                int before = button.InvalidationCount;

                button.TextAlignment = ContentAlignment.MiddleLeft;

                Assert.True(button.InvalidationCount > before,
                    "Das Setzen von TextAlignment hat kein Invalidate ausgelöst.");
            }
        }

        [Fact]
        public void Hovered_paints_the_skins_hovered_color()
        {
            SkinManager.Current = new PerStateButtonSkin();

            using (var button = new ProbeSkinButton())
            {
                button.Size = new Size(80, 30);
                button.Text = "";
                button.RaiseMouseEnter();

                using (var bitmap = new Bitmap(80, 30))
                {
                    button.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 30));

                    Assert.Equal(PerStateButtonSkin.HoveredColor.ToArgb(), bitmap.GetPixel(40, 15).ToArgb());
                }
            }
        }

        [Fact]
        public void Pressed_paints_the_skins_pressed_color()
        {
            SkinManager.Current = new PerStateButtonSkin();

            using (var button = new ProbeSkinButton())
            {
                button.Size = new Size(80, 30);
                button.Text = "";
                button.RaiseMouseEnter();
                button.RaiseMouseDown();

                using (var bitmap = new Bitmap(80, 30))
                {
                    button.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 30));

                    Assert.Equal(PerStateButtonSkin.PressedColor.ToArgb(), bitmap.GetPixel(40, 15).ToArgb());
                }
            }
        }

        [Fact]
        public void Disabled_paints_the_skins_disabled_color()
        {
            SkinManager.Current = new PerStateButtonSkin();

            using (var button = new ProbeSkinButton())
            {
                button.Size = new Size(80, 30);
                button.Text = "";
                button.Enabled = false;

                using (var bitmap = new Bitmap(80, 30))
                {
                    button.DrawToBitmap(bitmap, new Rectangle(0, 0, 80, 30));

                    Assert.Equal(PerStateButtonSkin.DisabledColor.ToArgb(), bitmap.GetPixel(40, 15).ToArgb());
                }
            }
        }

        /// <summary>
        /// Finding 3: DrawFocus hatte bislang null Testabdeckung — kein Test malte
        /// je einen Fokusring, setzte Focused oder prüfte ShowFocusRing. Dieser
        /// Test rendert einen wirklich fokussierten SkinButton über DrawToBitmap
        /// (also den echten Pfad SkinnedControl.OnPaint -&gt; DrawFocus) und prüft,
        /// dass die Fokusfarbe tatsächlich auf einem konkreten Pixel landet.
        ///
        /// Focused simulieren statt echten Betriebssystem-Fokus zu verlangen:
        /// SetFocus/Fensteraktivierung sind in einer automatisierten Umgebung
        /// nicht zuverlässig steuerbar (siehe ProbeFocusableSkinButton). Control.
        /// Focused ist virtual — dafür gedacht, siehe dortige Begründung.
        /// </summary>
        [Fact]
        public void A_focused_button_paints_the_focus_ring_colour_on_the_edge()
        {
            var buttonBackground = Color.FromArgb(255, 5, 5, 5);
            var focusColor = Color.FromArgb(255, 250, 10, 10);

            var skin = new SingleColorFocusSkin(buttonBackground, focusColor);
            SkinManager.Current = skin;

            using (var button = new ProbeFocusableSkinButton())
            {
                button.AutoSize = false;
                button.Size = new Size(40, 40);
                button.Text = "";
                button.FocusedOverride = true;

                using (var bitmap = new Bitmap(40, 40))
                {
                    button.DrawToBitmap(bitmap, new Rectangle(0, 0, 40, 40));

                    // Wie SkinPainterTests.A_border_paints_on_the_edge_but_not_in_the_centre:
                    // der Rahmen (hier: Fokusring, BorderWidth 2, Padding 0) liegt auf der
                    // Kante, nicht in der Mitte.
                    Assert.Equal(focusColor.ToArgb(), bitmap.GetPixel(20, 0).ToArgb());
                    Assert.Equal(buttonBackground.ToArgb(), bitmap.GetPixel(20, 20).ToArgb());
                }
            }
        }

        /// <summary>
        /// Gegenprobe zum vorigen Test: ohne Fokus darf an derselben Kanten-Stelle
        /// keine Fokusfarbe erscheinen. Beweist, dass der positive Test wirklich
        /// den Fokuszustand prüft, nicht zufällig eine ohnehin vorhandene Farbe.
        /// </summary>
        [Fact]
        public void An_unfocused_button_does_not_paint_the_focus_ring()
        {
            var buttonBackground = Color.FromArgb(255, 5, 5, 5);
            var focusColor = Color.FromArgb(255, 250, 10, 10);

            var skin = new SingleColorFocusSkin(buttonBackground, focusColor);
            SkinManager.Current = skin;

            using (var button = new ProbeFocusableSkinButton())
            {
                button.AutoSize = false;
                button.Size = new Size(40, 40);
                button.Text = "";
                button.FocusedOverride = false;

                using (var bitmap = new Bitmap(40, 40))
                {
                    button.DrawToBitmap(bitmap, new Rectangle(0, 0, 40, 40));

                    Assert.NotEqual(focusColor.ToArgb(), bitmap.GetPixel(20, 0).ToArgb());
                }
            }
        }

        [Fact]
        public void Clicking_raises_Click_once()
        {
            using (var button = new SkinButton())
            {
                int clicks = 0;
                button.Click += (s, e) => clicks++;

                button.PerformClick();

                Assert.Equal(1, clicks);
            }
        }

        [Fact]
        public void A_disabled_button_does_not_click()
        {
            using (var button = new SkinButton())
            {
                button.Enabled = false;
                int clicks = 0;
                button.Click += (s, e) => clicks++;

                button.PerformClick();

                Assert.Equal(0, clicks);
            }
        }

        /// <summary>
        /// Regression für den DPI-Abschneide-Defekt aus dem Demo-Programm
        /// (Task-15-Report, Abschnitt "Fix: Textabschneiden oberhalb 96 dpi"):
        /// SkinButton hatte bislang ausschließlich die feste Konstruktor-Größe
        /// (96, 30), die den tatsächlichen Textbedarf nie berücksichtigte —
        /// oberhalb von 96 dpi wuchs die Schrift (korrekt, über SkinPainter),
        /// die Buttongröße aber nie mit, sodass reihenweise Beschriftungen mit
        /// Auslassungspunkten abschnitten.
        ///
        /// "Zeig mir Hover" — eine der echten Demo-Beschriftungen — braucht
        /// bereits bei der DeviceDpi, die der Testprozess liefert (96: der
        /// Testhost ist nicht PerMonitorV2-fähig, siehe unten), mehr als die
        /// alten 96px; das reicht, um den Defekt ohne einen echten
        /// Monitor-DPI-Wechsel reproduzierbar zu machen. Die Wachstumsrate der
        /// Messung selbst mit steigender dpi ist unabhängig davon bereits über
        /// SkinPainterTests.Text_is_measured_larger_at_higher_dpi abgedeckt —
        /// zusammen belegen beide Tests, dass die Lücke bei jeder DPI ≥ 96
        /// nur noch größer würde, nie kleiner.
        ///
        /// Ein echter Test mit einer live simulierten DPI &gt; 96 wäre ehrlicher,
        /// ist hier aber nicht erreichbar: Control.DeviceDpi liest bei einem
        /// nicht-PerMonitorV2-fähigen Prozess (wie dem xUnit-Testhost) einen
        /// prozessweiten, nicht pro Instanz überschreibbaren Wert — ein Versuch,
        /// das private Feld per Reflection zu setzen, hatte nachweislich keine
        /// Wirkung auf den Rückgabewert (siehe Task-15-Report für das Protokoll
        /// dieses Experiments). Ein echter DPI-Wechsel setzt einen realen
        /// Multi-DPI-Monitor oder ein per-Monitor-fähiges Fensterhandle voraus.
        /// </summary>
        [Fact]
        public void AutoSize_grows_the_button_wide_enough_for_its_caption()
        {
            SkinManager.Current = new LightSkin();

            using (var button = new SkinButton())
            {
                button.AutoSize = true;
                button.Text = "Zeig mir Hover";

                var appearance = SkinManager.Current.GetAppearance(ElementKeys.Button, ElementState.Normal);

                using (var bitmap = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(bitmap))
                {
                    var measured = SkinPainter.MeasureText(g, button.Text, appearance, button.DeviceDpi);
                    var needed = SkinPainter.InflateByPadding(measured, appearance, button.DeviceDpi);

                    Assert.True(button.Width >= needed.Width,
                        string.Format(
                            "Button ist {0}px breit, braucht aber {1}px — die Beschriftung würde abgeschnitten.",
                            button.Width, needed.Width));
                }
            }
        }

        /// <summary>
        /// Prüft das Opt-out: mit AutoSize explizit ausgeschaltet löst ein
        /// Textwechsel kein AdjustSize() mehr aus (siehe SkinButton.OnTextChanged),
        /// eine explizit gesetzte Größe bleibt also erhalten. AutoSize ist seit
        /// diesem Fix standardmäßig an (siehe AutoSize_defaults_to_true) — dieser
        /// Test verifiziert nur noch, dass das Abschalten wirklich wirkt, nicht
        /// mehr irgendeine "bestehende" Vorgabe.
        /// </summary>
        [Fact]
        public void Opting_out_of_AutoSize_keeps_an_explicitly_set_size_across_a_text_change()
        {
            using (var button = new SkinButton())
            {
                button.AutoSize = false;
                button.Size = new Size(96, 30);

                button.Text = "Ein erheblich längerer Text als vorher";

                Assert.Equal(new Size(96, 30), button.Size);
            }
        }
    }
}
