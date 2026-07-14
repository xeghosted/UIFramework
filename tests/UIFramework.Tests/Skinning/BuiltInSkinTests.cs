using System.Collections.Generic;
using UIFramework.Core.Skinning;
using UIFramework.Core.Skinning.Skins;
using Xunit;

namespace UIFramework.Tests.Skinning
{
    public class BuiltInSkinTests
    {
        public static IEnumerable<object[]> AllSkins()
        {
            yield return new object[] { new LightSkin() };
            yield return new object[] { new DarkSkin() };
        }

        [Theory]
        [MemberData(nameof(AllSkins))]
        public void Every_element_and_state_is_defined_without_hitting_the_fallback(ISkin skin)
        {
            string[] elements = { ElementKeys.Button, ElementKeys.Panel, ElementKeys.Label, ElementKeys.Focus };
            ElementState[] states =
            {
                ElementState.Normal, ElementState.Hovered, ElementState.Pressed,
                ElementState.Selected, ElementState.Disabled
            };

            foreach (var element in elements)
            {
                foreach (var state in states)
                {
                    var appearance = skin.GetAppearance(element, state);

                    // Die Rückfallkette darf auf Normal zurückfallen, aber die
                    // eingebaute Notfarbe darf ein mitgelieferter Skin nie erreichen.
                    Assert.NotSame(SkinBase.FallbackAppearance, appearance);
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllSkins))]
        public void Every_appearance_is_opaque_and_has_a_font(ISkin skin)
        {
            string[] elements = { ElementKeys.Button, ElementKeys.Panel, ElementKeys.Label };

            foreach (var element in elements)
            {
                var appearance = skin.GetAppearance(element, ElementState.Normal);

                Assert.Equal(255, appearance.Background.A);
                Assert.NotNull(appearance.Font.Family);
            }
        }

        [Fact]
        public void The_two_skins_have_distinct_names()
        {
            Assert.NotEqual(new LightSkin().Name, new DarkSkin().Name);
        }

        [Fact]
        public void Dark_is_actually_darker_than_light()
        {
            var light = new LightSkin().GetAppearance(ElementKeys.Panel, ElementState.Normal);
            var dark = new DarkSkin().GetAppearance(ElementKeys.Panel, ElementState.Normal);

            Assert.True(dark.Background.GetBrightness() < light.Background.GetBrightness());
        }
    }
}
