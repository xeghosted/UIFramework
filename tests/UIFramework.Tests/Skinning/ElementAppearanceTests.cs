using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Skinning;
using Xunit;

namespace UIFramework.Tests.Skinning
{
    public class ElementAppearanceTests
    {
        [Fact]
        public void Without_a_gradient_end_it_has_no_gradient()
        {
            var appearance = new ElementAppearance { Background = Color.White };

            Assert.False(appearance.HasGradient);
        }

        [Fact]
        public void With_a_gradient_end_it_has_a_gradient()
        {
            var appearance = new ElementAppearance
            {
                Background = Color.White,
                BackgroundGradientEnd = Color.Gray
            };

            Assert.True(appearance.HasGradient);
        }

        [Fact]
        public void Defaults_are_harmless_rather_than_null()
        {
            var appearance = new ElementAppearance();

            Assert.Equal(0, appearance.BorderWidth);
            Assert.True(appearance.Corners.IsZero);
            Assert.Equal(Padding.Empty, appearance.Padding);
        }
    }
}
