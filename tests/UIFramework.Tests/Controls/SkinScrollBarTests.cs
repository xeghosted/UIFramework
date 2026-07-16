using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class SkinScrollBarTests : IDisposable
    {
        /// <summary>
        /// Wie SkinButtonTests.PerStateButtonSkin: pro Zustand eine andere Farbe,
        /// damit ein falsch aufgeloester Hover-Zustand ein sichtbar falsches
        /// Pixel ergibt statt zufaellig zu bestehen.
        /// </summary>
        private sealed class PerStateThumbSkin : SkinBase
        {
            public static readonly Color NormalColor = Color.FromArgb(255, 10, 20, 30);
            public static readonly Color HoveredColor = Color.FromArgb(255, 40, 50, 60);

            public PerStateThumbSkin()
            {
                Define(ElementKeys.ScrollBarThumb, ElementState.Normal, Appearance(NormalColor));
                Define(ElementKeys.ScrollBarThumb, ElementState.Hovered, Appearance(HoveredColor));
            }

            public override string Name
            {
                get { return "PerStateThumb"; }
            }

            private static ElementAppearance Appearance(Color background)
            {
                return new ElementAppearance
                {
                    Background = background,
                    BackgroundGradientEnd = null,
                    BorderColor = background,
                    BorderWidth = 0,
                    Corners = CornerRadius.None,
                    ForeColor = Color.FromArgb(255, 255, 255, 255),
                    Font = new UIFramework.Core.Skinning.FontSpec("Segoe UI", 9f),
                    Padding = new Padding(0)
                };
            }
        }

        public SkinScrollBarTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void A_fresh_scrollbar_is_vertical()
        {
            using (var bar = new SkinScrollBar())
            {
                Assert.Equal(Orientation.Vertical, bar.Orientation);
            }
        }

        [Fact]
        public void Value_is_clamped_to_the_reachable_range()
        {
            using (var bar = new SkinScrollBar())
            {
                bar.Minimum = 0;
                bar.Maximum = 400;
                bar.LargeChange = 100;

                bar.Value = 9999;

                // Groesster erreichbarer Wert = 400 - 100 + 1
                Assert.Equal(301, bar.Value);
            }
        }

        [Fact]
        public void Value_below_the_minimum_is_clamped_too()
        {
            using (var bar = new SkinScrollBar())
            {
                bar.Minimum = 10;
                bar.Maximum = 400;
                bar.LargeChange = 100;

                bar.Value = -5;

                Assert.Equal(10, bar.Value);
            }
        }

        [Fact]
        public void Scroll_fires_when_the_value_really_changes()
        {
            using (var bar = new SkinScrollBar())
            {
                bar.Minimum = 0;
                bar.Maximum = 400;
                bar.LargeChange = 100;

                int fired = 0;
                bar.Scroll += (s, e) => fired++;

                bar.Value = 50;

                Assert.Equal(1, fired);
            }
        }

        [Fact]
        public void Scroll_does_not_fire_when_the_value_is_set_to_what_it_already_is()
        {
            using (var bar = new SkinScrollBar())
            {
                bar.Minimum = 0;
                bar.Maximum = 400;
                bar.LargeChange = 100;
                bar.Value = 50;

                int fired = 0;
                bar.Scroll += (s, e) => fired++;

                bar.Value = 50;

                Assert.Equal(0, fired);
            }
        }

        [Fact]
        public void Scroll_does_not_fire_when_a_clamp_swallows_the_change()
        {
            // Value = 9999 landet auf 301. Ein zweites Value = 8888 landet
            // ebenfalls auf 301 — der Wert aendert sich nicht, also darf kein
            // Ereignis feuern. Sonst laeuft das Grid bei gehaltener Maus am
            // Ende der Liste in einen Sturm von Neuzeichnungen.
            using (var bar = new SkinScrollBar())
            {
                bar.Minimum = 0;
                bar.Maximum = 400;
                bar.LargeChange = 100;
                bar.Value = 9999;

                int fired = 0;
                bar.Scroll += (s, e) => fired++;

                bar.Value = 8888;

                Assert.Equal(0, fired);
            }
        }

        [Fact]
        public void Lowering_the_maximum_pulls_a_now_unreachable_value_back()
        {
            // Passiert bei jedem Filtern (2b): die Quelle schrumpft, der alte
            // Wert liegt hinter dem neuen Ende. Bliebe er stehen, zeigte das
            // Grid ins Leere.
            using (var bar = new SkinScrollBar())
            {
                bar.Minimum = 0;
                bar.Maximum = 400;
                bar.LargeChange = 100;
                bar.Value = 301;

                bar.Maximum = 200;

                Assert.Equal(101, bar.Value);
            }
        }

        [Fact]
        public void An_empty_range_reports_nothing_to_scroll()
        {
            using (var bar = new SkinScrollBar())
            {
                bar.Minimum = 0;
                bar.Maximum = 0;
                bar.LargeChange = 10;

                Assert.False(bar.Geometry.IsScrollable);
            }
        }

        [Fact]
        public void The_wheel_moves_by_small_change()
        {
            using (var bar = new SkinScrollBar())
            {
                bar.Minimum = 0;
                bar.Maximum = 400;
                bar.LargeChange = 100;
                bar.SmallChange = 3;
                bar.Value = 50;

                // Ein Rasterschritt nach unten = -120 Delta.
                bar.PerformWheel(-120);

                Assert.Equal(53, bar.Value);
            }
        }

        [Fact]
        public void The_wheel_up_moves_the_other_way()
        {
            using (var bar = new SkinScrollBar())
            {
                bar.Minimum = 0;
                bar.Maximum = 400;
                bar.LargeChange = 100;
                bar.SmallChange = 3;
                bar.Value = 50;

                bar.PerformWheel(120);

                Assert.Equal(47, bar.Value);
            }
        }

        [Fact]
        public void A_zero_wheel_delta_leaves_the_value_untouched()
        {
            // Bug: delta / MouseWheelScrollDelta ist bei delta==0 ebenfalls 0, und
            // der Fallback fuer kleine-aber-nicht-null-Deltas (0 > 0 ist false)
            // griff bisher auch hier und scrollte faelschlich um einen SmallChange
            // zurueck. PerformWheel ist public, damit GridControl Radereignisse
            // weiterreichen kann — ein weitergereichtes oder aufsummiertes Delta
            // von 0 ist ein realistischer Eingabewert, kein theoretischer.
            using (var bar = new SkinScrollBar())
            {
                bar.Minimum = 0;
                bar.Maximum = 400;
                bar.LargeChange = 100;
                bar.SmallChange = 3;
                bar.Value = 50;

                bar.PerformWheel(0);

                Assert.Equal(50, bar.Value);
            }
        }

        // --- Mausbedienung -------------------------------------------------
        //
        // Geometrie fuer alle folgenden Tests, sofern nicht anders angegeben:
        // TrackLength 300, Minimum 0, Maximum 300, LargeChange 100 ->
        // range 201, thumbLength 100, freeTrack 200. Bei Value 0 beginnt der
        // Daumen bei 0 (Rinne 0..100), bei Value 100 bei 100 (Rinne 100..200).
        // Die genauen Zahlen wurden gegen ScrollBarGeometry durchgerechnet,
        // nicht geraten.

        [Fact]
        public void Grabbing_the_thumb_and_dragging_down_moves_the_value_forward()
        {
            using (var bar = new ProbeSkinScrollBar())
            {
                bar.Width = 20;
                bar.Height = 300;
                bar.Minimum = 0;
                bar.Maximum = 300;
                bar.LargeChange = 100;

                bar.RaiseMouseDown(new Point(10, 10));   // im Daumen (0..100)
                bar.RaiseMouseMove(new Point(10, 110));  // 100px weiter unten

                Assert.Equal(101, bar.Value);
            }
        }

        [Fact]
        public void Grabbing_the_thumb_and_dragging_up_moves_the_value_back()
        {
            using (var bar = new ProbeSkinScrollBar())
            {
                bar.Width = 20;
                bar.Height = 300;
                bar.Minimum = 0;
                bar.Maximum = 300;
                bar.LargeChange = 100;
                bar.Value = 150;   // Daumen liegt jetzt bei 149..249

                bar.RaiseMouseDown(new Point(10, 159));  // im Daumen, 10px nach dessen Anfang
                bar.RaiseMouseMove(new Point(10, 119));  // 40px nach oben

                Assert.Equal(110, bar.Value);
            }
        }

        [Fact]
        public void The_grab_point_inside_the_thumb_is_respected_while_dragging()
        {
            // Griff NICHT am Daumenanfang (0), sondern 30px hinein — sonst
            // bestuende dieser Test auch, wenn _dragGrabOffset verlorenginge und
            // der Daumen einfach unter den Zeiger springe.
            using (var bar = new ProbeSkinScrollBar())
            {
                bar.Width = 20;
                bar.Height = 300;
                bar.Minimum = 0;
                bar.Maximum = 300;
                bar.LargeChange = 100;

                bar.RaiseMouseDown(new Point(10, 30));   // Daumen 0..100, Griff bei 30
                bar.RaiseMouseMove(new Point(10, 80));   // 50px weiter unten

                // Der Daumen muss um genau die 50px gewandert sein wie der
                // Zeiger, nicht mit seinem Anfang auf den Zeiger springen (das
                // waere ThumbOffset 80).
                Assert.Equal(50, bar.Geometry.ThumbOffset);
                Assert.Equal(50, bar.Value);
            }
        }

        [Fact]
        public void Dragging_the_thumb_moves_the_value_forward_when_horizontal()
        {
            // Dieselbe Rechnung wie beim vertikalen Test, nur entlang X — genau
            // die Achsenwahl, die dieses Control zu ScrollBarGeometry hinzufuegt.
            using (var bar = new ProbeSkinScrollBar())
            {
                bar.Orientation = Orientation.Horizontal;
                bar.Height = 20;
                bar.Width = 300;
                bar.Minimum = 0;
                bar.Maximum = 300;
                bar.LargeChange = 100;

                bar.RaiseMouseDown(new Point(10, 5));    // im Daumen (0..100)
                bar.RaiseMouseMove(new Point(110, 5));   // 100px weiter rechts

                Assert.Equal(101, bar.Value);
            }
        }

        [Fact]
        public void Clicking_the_groove_below_the_thumb_pages_forward_by_large_change()
        {
            using (var bar = new ProbeSkinScrollBar())
            {
                bar.Width = 20;
                bar.Height = 300;
                bar.Minimum = 0;
                bar.Maximum = 300;
                bar.LargeChange = 100;
                // Daumen bei Value 0: 0..100. Klick bei 250 liegt in der Rinne
                // dahinter.
                bar.RaiseMouseDown(new Point(10, 250));

                Assert.Equal(100, bar.Value);
            }
        }

        [Fact]
        public void Clicking_the_groove_above_the_thumb_pages_back_by_large_change()
        {
            using (var bar = new ProbeSkinScrollBar())
            {
                bar.Width = 20;
                bar.Height = 300;
                bar.Minimum = 0;
                bar.Maximum = 300;
                bar.LargeChange = 100;
                bar.Value = 100;   // Daumen jetzt bei 100..200
                // Klick bei 50 liegt in der Rinne davor.
                bar.RaiseMouseDown(new Point(10, 50));

                Assert.Equal(0, bar.Value);
            }
        }

        [Fact]
        public void A_groove_click_does_not_start_a_drag()
        {
            // Ein Klick in die Rinne setzt _thumbPressed nie — eine anschliessende
            // Mausbewegung darf den Wert deshalb nicht weiter veraendern.
            using (var bar = new ProbeSkinScrollBar())
            {
                bar.Width = 20;
                bar.Height = 300;
                bar.Minimum = 0;
                bar.Maximum = 300;
                bar.LargeChange = 100;

                bar.RaiseMouseDown(new Point(10, 250));  // Rinnenklick, Value wird 100
                Assert.Equal(100, bar.Value);

                bar.RaiseMouseMove(new Point(10, 10));

                Assert.Equal(100, bar.Value);
            }
        }

        [Fact]
        public void A_drag_continues_after_the_pointer_leaves_the_bar_and_stops_on_mouse_up()
        {
            // Bewusstes Verhalten (siehe Klassenkommentar/Review): wer den Daumen
            // greift und seitlich aus der Leiste hinauszieht, erwartet, dass das
            // Scrollen weitergeht. OnMouseLeave darf _thumbPressed deshalb nicht
            // loeschen; erst OnMouseUp beendet den Zug.
            using (var bar = new ProbeSkinScrollBar())
            {
                bar.Width = 20;
                bar.Height = 300;
                bar.Minimum = 0;
                bar.Maximum = 300;
                bar.LargeChange = 100;

                bar.RaiseMouseDown(new Point(10, 30));   // Daumen 0..100, Griff bei 30
                bar.RaiseMouseLeave();

                // Haelfte 1: der Zug ueberlebt das Verlassen der Leiste.
                bar.RaiseMouseMove(new Point(10, 80));
                Assert.Equal(50, bar.Value);

                bar.RaiseMouseUp(new Point(10, 80));

                // Haelfte 2: nach MouseUp bewegt eine weitere Mausbewegung nichts mehr.
                bar.RaiseMouseMove(new Point(10, 200));
                Assert.Equal(50, bar.Value);
            }
        }

        [Fact]
        public void Hovering_over_the_thumb_paints_the_hovered_colour_and_leaving_clears_it()
        {
            // _thumbHovered ist privat — der Beweis laeuft ueber den einzigen
            // erreichbaren, wirklich beobachtbaren Effekt: die tatsaechlich
            // gezeichnete Farbe des Daumens (wie SkinButtonTests es fuer Hovered/
            // Pressed vormacht). Keine neue oeffentliche Eigenschaft dafuer.
            SkinManager.Current = new PerStateThumbSkin();

            using (var bar = new ProbeSkinScrollBar())
            {
                bar.Width = 20;
                bar.Height = 300;
                bar.Minimum = 0;
                bar.Maximum = 300;
                bar.LargeChange = 100;
                // Daumen bei Value 0: 0..100, volle Breite (0..20).

                using (var bitmap = new Bitmap(20, 300))
                {
                    bar.DrawToBitmap(bitmap, new Rectangle(0, 0, 20, 300));
                    Assert.Equal(PerStateThumbSkin.NormalColor.ToArgb(), bitmap.GetPixel(10, 50).ToArgb());
                }

                bar.RaiseMouseMove(new Point(10, 50));   // Zeiger steht im Daumen

                using (var bitmap = new Bitmap(20, 300))
                {
                    bar.DrawToBitmap(bitmap, new Rectangle(0, 0, 20, 300));
                    Assert.Equal(PerStateThumbSkin.HoveredColor.ToArgb(), bitmap.GetPixel(10, 50).ToArgb());
                }

                bar.RaiseMouseLeave();

                using (var bitmap = new Bitmap(20, 300))
                {
                    bar.DrawToBitmap(bitmap, new Rectangle(0, 0, 20, 300));
                    Assert.Equal(PerStateThumbSkin.NormalColor.ToArgb(), bitmap.GetPixel(10, 50).ToArgb());
                }
            }
        }
    }
}
