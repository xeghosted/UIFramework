using System;
using System.Drawing;
using System.Globalization;
using UIFramework.Controls;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class CalendarContentTests
    {
        private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

        private static CalendarContent July2026(DateTime? selected = null)
        {
            return new CalendarContent(new DateTime(2026, 7, 1), selected, De);
        }

        private static Size Measured(CalendarContent content)
        {
            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
                return content.Measure(g, 96, anchorWidth: 0);
        }

        [Fact]
        public void The_sheet_is_seven_columns_wide_and_nine_rows_tall()
        {
            var content = July2026();
            var size = Measured(content);

            Assert.Equal(0, size.Width % 7);
            Assert.Equal(0, size.Height % 9);
        }

        [Fact]
        public void Clicking_an_in_month_day_chooses_it_and_requests_close()
        {
            var content = July2026();
            Measured(content);

            DateTime chosen = DateTime.MinValue;
            bool closeAsked = false;
            content.DateChosen += d => chosen = d;
            content.CloseRequested += (s, e) => closeAsked = true;

            // de-DE: Woche beginnt Montag; der 1. Juli 2026 (Mittwoch) liegt in
            // Zeile 0, Spalte 2 des Blatts.
            var cell = content.DayCellForTests(0, 2);
            content.HandleMouseClick(new Point(cell.Left + cell.Width / 2, cell.Top + cell.Height / 2));

            Assert.Equal(new DateTime(2026, 7, 1), chosen);
            Assert.True(closeAsked);
        }

        [Fact]
        public void Clicking_a_neighbour_month_day_does_nothing()
        {
            var content = July2026();
            Measured(content);

            bool anything = false;
            content.DateChosen += d => anything = true;
            content.CloseRequested += (s, e) => anything = true;

            // Zeile 0, Spalte 0 ist der 29. Juni — Nachbarmonat, Disabled.
            var cell = content.DayCellForTests(0, 0);
            content.HandleMouseClick(new Point(cell.Left + 2, cell.Top + 2));

            Assert.False(anything);
        }

        [Fact]
        public void The_next_arrow_turns_the_sheet_one_month_forward()
        {
            var content = July2026();
            Measured(content);

            bool repainted = false;
            content.VisualChanged += (s, e) => repainted = true;

            var arrow = content.NextArrowForTests();
            content.HandleMouseClick(new Point(arrow.Left + 2, arrow.Top + 2));

            Assert.True(repainted);

            // Nach dem Blättern wählt dieselbe Zelle (0,2) einen August-Tag:
            // August 2026 beginnt am Samstag, Zelle (0,2) ist der 29. Juli —
            // Nachbarmonat, klickt also NICHT. Zelle (1,0) ist der 3. August.
            DateTime chosen = DateTime.MinValue;
            content.DateChosen += d => chosen = d;
            var cell = content.DayCellForTests(1, 0);
            content.HandleMouseClick(new Point(cell.Left + 2, cell.Top + 2));

            Assert.Equal(new DateTime(2026, 8, 3), chosen);
        }

        [Fact]
        public void The_today_row_chooses_today()
        {
            var content = July2026();
            Measured(content);

            DateTime chosen = DateTime.MinValue;
            content.DateChosen += d => chosen = d;

            var row = content.TodayRowForTests();
            content.HandleMouseClick(new Point(row.Left + 4, row.Top + row.Height / 2));

            Assert.Equal(DateTime.Today, chosen);
        }
    }
}
