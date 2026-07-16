using System;
using UIFramework.Core.Skinning;
using Xunit;

namespace UIFramework.Tests.Skinning
{
    public class CornerRadiusTests
    {
        [Fact]
        public void Uniform_constructor_sets_all_four_corners()
        {
            var radius = new CornerRadius(4);

            Assert.Equal(4, radius.TopLeft);
            Assert.Equal(4, radius.TopRight);
            Assert.Equal(4, radius.BottomRight);
            Assert.Equal(4, radius.BottomLeft);
        }

        [Fact]
        public void Per_corner_constructor_assigns_clockwise_from_top_left()
        {
            var radius = new CornerRadius(1, 2, 3, 4);

            Assert.Equal(1, radius.TopLeft);
            Assert.Equal(2, radius.TopRight);
            Assert.Equal(3, radius.BottomRight);
            Assert.Equal(4, radius.BottomLeft);
        }

        [Fact]
        public void None_is_zero_on_every_corner()
        {
            Assert.True(CornerRadius.None.IsZero);
        }

        [Fact]
        public void A_single_non_zero_corner_means_not_zero()
        {
            Assert.False(new CornerRadius(0, 0, 1, 0).IsZero);
        }

        [Fact]
        public void Negative_radius_is_rejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CornerRadius(-1));
        }

        /// <summary>
        /// Finding 5: die Meldung nannte bislang immer "topLeft", selbst wenn ein
        /// anderer Parameter der eigentliche Übeltäter war. Jeder Parameter muss
        /// sich selbst korrekt benennen.
        /// </summary>
        [Fact]
        public void Negative_topLeft_is_reported_by_its_own_name()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new CornerRadius(-1, 0, 0, 0));
            Assert.Equal("topLeft", ex.ParamName);
        }

        [Fact]
        public void Negative_topRight_is_reported_by_its_own_name()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new CornerRadius(0, -1, 0, 0));
            Assert.Equal("topRight", ex.ParamName);
        }

        [Fact]
        public void Negative_bottomRight_is_reported_by_its_own_name()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new CornerRadius(0, 0, -1, 0));
            Assert.Equal("bottomRight", ex.ParamName);
        }

        [Fact]
        public void Negative_bottomLeft_is_reported_by_its_own_name()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new CornerRadius(0, 0, 0, -1));
            Assert.Equal("bottomLeft", ex.ParamName);
        }

        [Fact]
        public void Equal_values_are_equal_and_share_a_hash_code()
        {
            var a = new CornerRadius(1, 2, 3, 4);
            var b = new CornerRadius(1, 2, 3, 4);

            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void The_operators_agree_with_Equals()
        {
            var a = new CornerRadius(1, 2, 3, 4);
            var b = new CornerRadius(1, 2, 3, 4);
            var different = new CornerRadius(4, 3, 2, 1);

            Assert.True(a == b);
            Assert.False(a != b);
            Assert.False(a == different);
            Assert.True(a != different);
        }
    }
}
