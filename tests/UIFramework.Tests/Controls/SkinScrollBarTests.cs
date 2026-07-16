using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class SkinScrollBarTests
    {
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
    }
}
