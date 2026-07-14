using System;
using System.Drawing;
using UIFramework.Core.Skinning;
using Xunit;

namespace UIFramework.Tests.Skinning
{
    public class SkinBaseTests
    {
        private sealed class SparseSkin : SkinBase
        {
            public static readonly ElementAppearance ButtonNormal =
                new ElementAppearance { Background = Color.FromArgb(255, 10, 10, 10) };

            public static readonly ElementAppearance ButtonHovered =
                new ElementAppearance { Background = Color.FromArgb(255, 20, 20, 20) };

            public SparseSkin()
            {
                Define(ElementKeys.Button, ElementState.Normal, ButtonNormal);
                Define(ElementKeys.Button, ElementState.Hovered, ButtonHovered);
                // Pressed/Disabled/Selected sind bewusst NICHT definiert.
                // Panel ist bewusst gar nicht definiert.
            }

            public override string Name
            {
                get { return "Sparse"; }
            }
        }

        [Fact]
        public void A_defined_element_and_state_is_returned_directly()
        {
            var skin = new SparseSkin();

            Assert.Same(SparseSkin.ButtonHovered, skin.GetAppearance(ElementKeys.Button, ElementState.Hovered));
        }

        [Fact]
        public void An_undefined_state_falls_back_to_Normal_of_the_same_element()
        {
            var skin = new SparseSkin();

            Assert.Same(SparseSkin.ButtonNormal, skin.GetAppearance(ElementKeys.Button, ElementState.Pressed));
        }

        [Fact]
        public void An_entirely_unknown_element_falls_back_to_the_built_in_default()
        {
            var skin = new SparseSkin();

            Assert.Same(SkinBase.FallbackAppearance, skin.GetAppearance("VoelligUnbekanntesElement", ElementState.Normal));
        }

        [Fact]
        public void The_fallback_never_produces_an_invisible_control()
        {
            Assert.NotEqual(0, SkinBase.FallbackAppearance.Background.A);
            Assert.NotEqual(0, SkinBase.FallbackAppearance.ForeColor.A);
        }

        [Fact]
        public void A_null_element_key_is_a_programming_error_and_throws()
        {
            var skin = new SparseSkin();

            Assert.Throws<ArgumentNullException>(() => skin.GetAppearance(null, ElementState.Normal));
        }

        [Fact]
        public void Defining_the_same_element_and_state_twice_overwrites()
        {
            var skin = new OverwritingSkin();

            Assert.Same(OverwritingSkin.Second, skin.GetAppearance(ElementKeys.Panel, ElementState.Normal));
        }

        private sealed class OverwritingSkin : SkinBase
        {
            public static readonly ElementAppearance First = new ElementAppearance();
            public static readonly ElementAppearance Second = new ElementAppearance();

            public OverwritingSkin()
            {
                Define(ElementKeys.Panel, ElementState.Normal, First);
                Define(ElementKeys.Panel, ElementState.Normal, Second);
            }

            public override string Name
            {
                get { return "Overwriting"; }
            }
        }
    }
}
