using System;
using System.Globalization;
using UIFramework.Controls.Editing;
using Xunit;

namespace UIFramework.Tests.Controls.Editing
{
    public class DateBehaviorTests
    {
        private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

        [Fact]
        public void A_short_date_parses_with_the_culture()
        {
            Assert.Equal(new DateTime(2026, 7, 28),
                DateBehavior.ParseOrFallback("28.07.2026", null, De));
        }

        [Fact]
        public void Empty_text_means_no_value_not_a_fallback()
        {
            Assert.Null(DateBehavior.ParseOrFallback("", new DateTime(2026, 1, 1), De));
            Assert.Null(DateBehavior.ParseOrFallback("   ", new DateTime(2026, 1, 1), De));
        }

        [Fact]
        public void Unparsable_text_falls_back_silently()
        {
            var fallback = new DateTime(2026, 1, 1);
            Assert.Equal(fallback, DateBehavior.ParseOrFallback("kein datum", fallback, De));
        }

        [Fact]
        public void A_time_portion_is_cut_to_the_pure_date()
        {
            Assert.Equal(new DateTime(2026, 7, 28),
                DateBehavior.ParseOrFallback("28.07.2026 13:45", null, De));
        }

        [Fact]
        public void Format_writes_the_cultures_short_date_and_empty_for_null()
        {
            Assert.Equal("28.07.2026", DateBehavior.Format(new DateTime(2026, 7, 28), De));
            Assert.Equal("", DateBehavior.Format(null, De));
        }
    }
}
