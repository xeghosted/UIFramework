using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;
using Xunit;

namespace UIFramework.Tests.Rendering
{
    public class SkinPainterTests
    {
        private static readonly Color Fill = Color.FromArgb(255, 200, 30, 40);
        private static readonly Color Edge = Color.FromArgb(255, 10, 20, 220);

        private static Bitmap Render(int size, System.Action<Graphics> draw)
        {
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.FromArgb(0, 0, 0, 0));
                draw(g);
            }
            return bitmap;
        }

        [Fact]
        public void A_solid_background_fills_the_centre()
        {
            var appearance = new ElementAppearance { Background = Fill, Corners = CornerRadius.None };

            using (var bitmap = Render(40, g =>
                SkinPainter.DrawBackground(g, new Rectangle(0, 0, 40, 40), appearance, 96)))
            {
                Assert.Equal(Fill.ToArgb(), bitmap.GetPixel(20, 20).ToArgb());
            }
        }

        [Fact]
        public void A_square_background_reaches_into_the_corner()
        {
            var appearance = new ElementAppearance { Background = Fill, Corners = CornerRadius.None };

            using (var bitmap = Render(40, g =>
                SkinPainter.DrawBackground(g, new Rectangle(0, 0, 40, 40), appearance, 96)))
            {
                Assert.Equal(Fill.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
            }
        }

        [Fact]
        public void A_rounded_background_leaves_the_corner_transparent()
        {
            var appearance = new ElementAppearance { Background = Fill, Corners = new CornerRadius(10) };

            using (var bitmap = Render(40, g =>
                SkinPainter.DrawBackground(g, new Rectangle(0, 0, 40, 40), appearance, 96)))
            {
                // Die Ecke wird ausgespart, die Mitte nicht.
                Assert.Equal(0, bitmap.GetPixel(0, 0).A);
                Assert.Equal(Fill.ToArgb(), bitmap.GetPixel(20, 20).ToArgb());
            }
        }

        [Fact]
        public void The_corner_radius_grows_with_dpi()
        {
            var appearance = new ElementAppearance { Background = Fill, Corners = new CornerRadius(6) };

            using (var at96 = Render(60, g =>
                SkinPainter.DrawBackground(g, new Rectangle(0, 0, 60, 60), appearance, 96)))
            using (var at192 = Render(60, g =>
                SkinPainter.DrawBackground(g, new Rectangle(0, 0, 60, 60), appearance, 192)))
            {
                // Bei (3,3) ist der kleine Radius schon gefüllt, der doppelte noch nicht.
                Assert.NotEqual(0, at96.GetPixel(3, 3).A);
                Assert.Equal(0, at192.GetPixel(3, 3).A);
            }
        }

        [Fact]
        public void A_border_paints_on_the_edge_but_not_in_the_centre()
        {
            var appearance = new ElementAppearance
            {
                Background = Color.Transparent,
                BorderColor = Edge,
                BorderWidth = 1,
                Corners = CornerRadius.None
            };

            using (var bitmap = Render(40, g =>
                SkinPainter.DrawBorder(g, new Rectangle(0, 0, 40, 40), appearance, 96)))
            {
                Assert.Equal(Edge.ToArgb(), bitmap.GetPixel(20, 0).ToArgb());
                Assert.Equal(0, bitmap.GetPixel(20, 20).A);
            }
        }

        [Fact]
        public void A_zero_width_border_paints_nothing()
        {
            var appearance = new ElementAppearance
            {
                Background = Color.Transparent,
                BorderColor = Edge,
                BorderWidth = 0
            };

            using (var bitmap = Render(40, g =>
                SkinPainter.DrawBorder(g, new Rectangle(0, 0, 40, 40), appearance, 96)))
            {
                Assert.Equal(0, bitmap.GetPixel(20, 0).A);
            }
        }

        [Fact]
        public void An_empty_rectangle_is_survived_without_an_exception()
        {
            var appearance = new ElementAppearance { Background = Fill, BorderColor = Edge, BorderWidth = 1 };

            using (var bitmap = new Bitmap(10, 10))
            using (var g = Graphics.FromImage(bitmap))
            {
                SkinPainter.DrawBackground(g, new Rectangle(0, 0, 0, 0), appearance, 96);
                SkinPainter.DrawBorder(g, new Rectangle(0, 0, 0, 0), appearance, 96);
            }
        }

        [Fact]
        public void Text_is_measured_larger_at_higher_dpi()
        {
            var appearance = new ElementAppearance { Font = new FontSpec("Segoe UI", 9f), ForeColor = Edge };

            using (var bitmap = new Bitmap(10, 10))
            using (var g = Graphics.FromImage(bitmap))
            {
                var at96 = SkinPainter.MeasureText(g, "Beispiel", appearance, 96);
                var at192 = SkinPainter.MeasureText(g, "Beispiel", appearance, 192);

                Assert.True(at192.Width > at96.Width);
                Assert.True(at192.Height > at96.Height);
            }
        }

        [Fact]
        public void Null_or_empty_text_measures_to_nothing_and_paints_nothing()
        {
            var appearance = new ElementAppearance { Font = new FontSpec("Segoe UI", 9f), ForeColor = Edge };

            using (var bitmap = new Bitmap(10, 10))
            using (var g = Graphics.FromImage(bitmap))
            {
                Assert.Equal(Size.Empty, SkinPainter.MeasureText(g, null, appearance, 96));
                Assert.Equal(Size.Empty, SkinPainter.MeasureText(g, "", appearance, 96));

                SkinPainter.DrawText(g, null, new Rectangle(0, 0, 10, 10), appearance, 96, ContentAlignment.MiddleCenter);
            }
        }

        [Fact]
        public void An_ampersand_is_measured_as_a_literal_character_not_a_mnemonic_prefix()
        {
            var appearance = new ElementAppearance { Font = new FontSpec("Segoe UI", 9f), ForeColor = Edge };

            using (var bitmap = new Bitmap(10, 10))
            using (var g = Graphics.FromImage(bitmap))
            {
                var withAmpersand = SkinPainter.MeasureText(g, "A&B", appearance, 96);
                var withoutAmpersand = SkinPainter.MeasureText(g, "AB", appearance, 96);

                // Ohne TextFormatFlags.NoPrefix behandelt TextRenderer "&" als
                // Tastaturkürzel-Marker (verschwindet, unterstreicht das
                // folgende Zeichen statt sich selbst zu zeigen) -- "A&B" mäße
                // dann genauso breit wie "AB". Live an einem Verwender-Fenster
                // gefunden: ein Reitertitel "Module & Maps" zeigte
                // "Module _Maps" statt des echten "&"-Zeichens.
                Assert.True(withAmpersand.Width > withoutAmpersand.Width);
            }
        }

        // --- Finding 3: Einzugs-Helfer bislang nur mit BorderWidth == 0 geprüft ---
        //
        // Jeder mitgelieferte Test-Skin (StubSkin, PanelOnlySkin, LabelOnlySkin,
        // PerStateButtonSkin) setzt BorderWidth = 0. Damit hätte man den
        // Rahmen-Term aus GetContentRectangle streichen können, ohne dass die
        // Suite das bemerkt hätte. Die folgenden Tests verwenden erwartete Werte,
        // die von Hand ausgerechnet sind (nicht über SkinPainter selbst), damit
        // sie einen echten Fehler auch wirklich anzeigen — siehe die
        // Mutationsprobe im Abschlussbericht.

        [Fact]
        public void GetContentRectangle_insets_by_padding_plus_border_with_hand_computed_numbers()
        {
            // Padding(4) auf allen Seiten + BorderWidth 2 auf allen Seiten, dpi 96
            // (keine Skalierung) => Einzug von Hand: 4 + 2 = 6 auf jeder Seite.
            var appearance = new ElementAppearance
            {
                Padding = new Padding(4),
                BorderWidth = 2
            };

            var content = SkinPainter.GetContentRectangle(new Rectangle(0, 0, 100, 50), appearance, 96);

            Assert.Equal(6, content.Left);
            Assert.Equal(6, content.Top);
            Assert.Equal(88, content.Width);   // 100 - 6 - 6
            Assert.Equal(38, content.Height);  // 50 - 6 - 6
        }

        [Fact]
        public void InflateByPadding_adds_padding_plus_border_with_hand_computed_numbers()
        {
            // Padding(4) + BorderWidth 2, dpi 96: von Hand erwartet: +6 je Seite,
            // also +12 auf Breite und Höhe zusammen.
            var appearance = new ElementAppearance
            {
                Padding = new Padding(4),
                BorderWidth = 2
            };

            var inflated = SkinPainter.InflateByPadding(new Size(10, 20), appearance, 96);

            Assert.Equal(22, inflated.Width);   // 10 + 4 + 4 + 2 + 2
            Assert.Equal(32, inflated.Height);  // 20 + 4 + 4 + 2 + 2
        }

        /// <summary>
        /// Pinnt die (durch Finding 1 vereinheitlichte) Konvention von DrawPaddedText:
        /// Padding + Rahmenbreite, nicht nur Padding. Bei Padding = 0 und
        /// BorderWidth = 10 darf unter der neuen Konvention in den äußeren 10px
        /// (dem reinen Rahmenband) kein Textpixel liegen — unter der alten,
        /// Padding-only-Konvention wäre das Band 0px breit gewesen und der Text
        /// hätte bis an den Rand reichen dürfen.
        /// </summary>
        [Fact]
        public void DrawPaddedText_insets_by_border_too_not_just_padding()
        {
            var background = Color.FromArgb(255, 5, 5, 5);
            var textColor = Color.FromArgb(255, 250, 250, 250);
            var appearance = new ElementAppearance
            {
                Padding = new Padding(0),
                BorderWidth = 10,
                Font = new FontSpec("Segoe UI", 9f),
                ForeColor = textColor
            };

            using (var bitmap = new Bitmap(60, 30))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(background);
                SkinPainter.DrawPaddedText(g, "M", new Rectangle(0, 0, 60, 30), appearance, 96, ContentAlignment.TopLeft);

                // Reines Rahmenband: die ersten 10 Spalten UND die ersten 10 Zeilen
                // dürfen keinen einzigen Textpixel enthalten.
                for (int x = 0; x < 10; x++)
                {
                    for (int y = 0; y < 30; y++)
                    {
                        Assert.True(bitmap.GetPixel(x, y).ToArgb() == background.ToArgb(),
                            string.Format("Textpixel bei ({0},{1}) liegt im Rahmenband — DrawPaddedText zieht die Rahmenbreite nicht ab.", x, y));
                    }
                }
                for (int x = 0; x < 60; x++)
                {
                    for (int y = 0; y < 10; y++)
                    {
                        Assert.True(bitmap.GetPixel(x, y).ToArgb() == background.ToArgb(),
                            string.Format("Textpixel bei ({0},{1}) liegt im Rahmenband — DrawPaddedText zieht die Rahmenbreite nicht ab.", x, y));
                    }
                }

                // Gegenprobe: im Inhaltsbereich (ab (10,10)) muss tatsächlich etwas
                // gezeichnet worden sein, sonst würde der obige Test nur deshalb
                // bestehen, weil gar kein Text gemalt wurde.
                bool foundText = false;
                for (int x = 10; x < 60 && !foundText; x++)
                {
                    for (int y = 10; y < 30 && !foundText; y++)
                    {
                        if (bitmap.GetPixel(x, y).ToArgb() != background.ToArgb())
                            foundText = true;
                    }
                }
                Assert.True(foundText, "Es wurde kein Text im Inhaltsbereich gefunden.");
            }
        }
    }
}
