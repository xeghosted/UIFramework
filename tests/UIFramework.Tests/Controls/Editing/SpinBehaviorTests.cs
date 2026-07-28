using System.Globalization;
using UIFramework.Controls.Editing;
using Xunit;

namespace UIFramework.Tests.Controls.Editing
{
    public class SpinBehaviorTests
    {
        private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

        [Theory]
        [InlineData(5, 0, 10, 5)]
        [InlineData(-3, 0, 10, 0)]
        [InlineData(42, 0, 10, 10)]
        public void Clamp_pins_the_value_into_the_range(decimal value, decimal min, decimal max, decimal expected)
        {
            Assert.Equal(expected, SpinBehavior.Clamp(value, min, max));
        }

        [Fact]
        public void Parsing_a_valid_number_respects_the_culture()
        {
            Assert.Equal(3.5m, SpinBehavior.ParseOrFallback("3,5", 0m, 0m, 10m, De));
            Assert.Equal(3.5m, SpinBehavior.ParseOrFallback("3.5", 0m, 0m, 10m, En));
        }

        [Fact]
        public void Parsing_clamps_into_the_range()
        {
            Assert.Equal(10m, SpinBehavior.ParseOrFallback("99", 5m, 0m, 10m, De));
        }

        [Fact]
        public void Unparsable_text_falls_back_silently()
        {
            Assert.Equal(5m, SpinBehavior.ParseOrFallback("abc", 5m, 0m, 10m, De));
            Assert.Equal(5m, SpinBehavior.ParseOrFallback("", 5m, 0m, 10m, De));
        }

        [Fact]
        public void A_fallback_outside_the_range_is_clamped_too()
        {
            // Passiert, wenn MinValue nachträglich angehoben wurde.
            Assert.Equal(0m, SpinBehavior.ParseOrFallback("abc", -7m, 0m, 10m, De));
        }

        [Fact]
        public void The_key_filter_allows_digits_sign_and_the_cultures_decimal_separator()
        {
            Assert.True(SpinBehavior.IsCharAllowed('7', De));
            Assert.True(SpinBehavior.IsCharAllowed('-', De));
            Assert.True(SpinBehavior.IsCharAllowed('+', De));
            Assert.True(SpinBehavior.IsCharAllowed(',', De));
            Assert.False(SpinBehavior.IsCharAllowed('.', De));   // Punkt ist de-DE-Gruppenzeichen, kein Dezimaltrenner
            Assert.True(SpinBehavior.IsCharAllowed('.', En));
            Assert.False(SpinBehavior.IsCharAllowed('x', De));
        }
    }
}
