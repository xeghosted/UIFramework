using System;
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

        [Fact]
        public void A_fresh_appearance_is_not_frozen()
        {
            Assert.False(new ElementAppearance().IsFrozen);
        }

        [Fact]
        public void Freezing_marks_it_frozen()
        {
            var appearance = new ElementAppearance();

            appearance.Freeze();

            Assert.True(appearance.IsFrozen);
        }

        [Fact]
        public void Freezing_twice_is_harmless()
        {
            // Kein Selbstzweck: StubSkin gibt EINE Erscheinung an vier
            // Define-Aufrufe, und SkinBaseTests konstruiert seinen SparseSkin
            // in jedem Test neu, obwohl dessen Erscheinungen statisch sind.
            // Beide frieren dieselbe Instanz mehrfach ein.
            var appearance = new ElementAppearance();

            appearance.Freeze();
            appearance.Freeze();

            Assert.True(appearance.IsFrozen);
        }

        [Fact]
        public void Every_setter_throws_once_frozen()
        {
            var appearance = new ElementAppearance();
            appearance.Freeze();

            Assert.Throws<InvalidOperationException>(() => appearance.Background = Color.Red);
            Assert.Throws<InvalidOperationException>(() => appearance.BackgroundGradientEnd = Color.Red);
            Assert.Throws<InvalidOperationException>(() => appearance.BorderColor = Color.Red);
            Assert.Throws<InvalidOperationException>(() => appearance.BorderWidth = 3);
            Assert.Throws<InvalidOperationException>(() => appearance.Corners = new CornerRadius(3));
            Assert.Throws<InvalidOperationException>(() => appearance.ForeColor = Color.Red);
            Assert.Throws<InvalidOperationException>(() => appearance.Font = new FontSpec("Arial", 12f));
            Assert.Throws<InvalidOperationException>(() => appearance.Padding = new Padding(3));
        }

        [Fact]
        public void The_message_says_what_went_wrong_and_why_it_matters()
        {
            var appearance = new ElementAppearance();
            appearance.Freeze();

            var error = Assert.Throws<InvalidOperationException>(() => appearance.Background = Color.Red);

            Assert.Contains("eingefroren", error.Message);
        }

        [Fact]
        public void Freezing_locks_the_setters_but_not_the_getters()
        {
            var appearance = new ElementAppearance
            {
                Background = Color.White,
                BorderWidth = 2,
                Corners = new CornerRadius(4)
            };

            appearance.Freeze();

            Assert.Equal(Color.White, appearance.Background);
            Assert.Equal(2, appearance.BorderWidth);
            Assert.Equal(4, appearance.Corners.TopLeft);
            Assert.False(appearance.HasGradient);
        }
    }
}
