using System;
using UIFramework.Controls.Editing;
using Xunit;

namespace UIFramework.Tests.Controls.Editing
{
    public class MonthGridTests
    {
        // Juli 2026: der 1. ist ein Mittwoch. Mit Wochenstart Montag beginnt
        // das Blatt am Montag, 29. Juni.

        [Fact]
        public void The_sheet_starts_on_the_first_day_of_week_before_or_on_the_first()
        {
            var grid = new MonthGrid(2026, 7, DayOfWeek.Monday);

            Assert.Equal(new DateTime(2026, 6, 29), grid.CellAt(0, 0));
        }

        [Fact]
        public void A_month_starting_on_the_week_start_begins_in_cell_zero()
        {
            // Juni 2026: der 1. ist ein Montag — keine Vorlaufzellen.
            var grid = new MonthGrid(2026, 6, DayOfWeek.Monday);

            Assert.Equal(new DateTime(2026, 6, 1), grid.CellAt(0, 0));
        }

        [Fact]
        public void Cells_advance_day_by_day_row_by_row()
        {
            var grid = new MonthGrid(2026, 7, DayOfWeek.Monday);

            Assert.Equal(new DateTime(2026, 7, 1), grid.CellAt(0, 2));
            Assert.Equal(new DateTime(2026, 7, 6), grid.CellAt(1, 0));
            Assert.Equal(new DateTime(2026, 8, 9), grid.CellAt(5, 6));   // letzte der 42 Zellen
        }

        [Fact]
        public void IsInMonth_separates_neighbour_month_days()
        {
            var grid = new MonthGrid(2026, 7, DayOfWeek.Monday);

            Assert.False(grid.IsInMonth(new DateTime(2026, 6, 30)));
            Assert.True(grid.IsInMonth(new DateTime(2026, 7, 31)));
            Assert.False(grid.IsInMonth(new DateTime(2026, 8, 1)));
        }

        [Fact]
        public void Navigation_wraps_across_the_year_boundary()
        {
            var grid = new MonthGrid(2026, 1, DayOfWeek.Monday);

            var previous = grid.PreviousMonth();
            Assert.Equal(2025, previous.Year);
            Assert.Equal(12, previous.Month);

            var next = new MonthGrid(2026, 12, DayOfWeek.Monday).NextMonth();
            Assert.Equal(2027, next.Year);
            Assert.Equal(1, next.Month);
        }

        [Fact]
        public void The_week_start_follows_the_given_culture_convention()
        {
            // US-Konvention: Woche beginnt Sonntag — Juli 2026 startet dann am 28. Juni.
            var grid = new MonthGrid(2026, 7, DayOfWeek.Sunday);

            Assert.Equal(new DateTime(2026, 6, 28), grid.CellAt(0, 0));
        }
    }
}
