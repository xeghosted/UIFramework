using System;
using System.Drawing;
using UIFramework.Core.Skinning;
using Xunit;

namespace UIFramework.Tests.Skinning
{
    public class FontSpecTests
    {
        [Fact]
        public void Equal_values_are_equal_and_share_a_hash_code()
        {
            var a = new FontSpec("Segoe UI", 9f, FontStyle.Bold);
            var b = new FontSpec("Segoe UI", 9f, FontStyle.Bold);

            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Different_size_is_not_equal()
        {
            var a = new FontSpec("Segoe UI", 9f);
            var b = new FontSpec("Segoe UI", 10f);

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Different_style_is_not_equal()
        {
            var a = new FontSpec("Segoe UI", 9f, FontStyle.Regular);
            var b = new FontSpec("Segoe UI", 9f, FontStyle.Italic);

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Empty_family_is_rejected()
        {
            Assert.Throws<ArgumentException>(() => new FontSpec("  ", 9f));
        }

        [Fact]
        public void Non_positive_size_is_rejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FontSpec("Segoe UI", 0f));
        }

        [Fact]
        public void NaN_size_is_rejected()
        {
            // sizeInPoints <= 0f ist fuer NaN falsch (jeder Vergleich mit NaN
            // ist falsch) - der Konstruktor liesse NaN bisher unbemerkt durch.
            Assert.Throws<ArgumentOutOfRangeException>(() => new FontSpec("Arial", float.NaN));
        }

        [Fact]
        public void The_operators_agree_with_Equals()
        {
            var a = new FontSpec("Segoe UI", 9f, FontStyle.Bold);
            var b = new FontSpec("Segoe UI", 9f, FontStyle.Bold);
            var different = new FontSpec("Arial", 9f, FontStyle.Bold);

            Assert.True(a == b);
            Assert.False(a != b);
            Assert.False(a == different);
            Assert.True(a != different);
        }
    }
}
