using System;
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
            string[] elements = { ElementKeys.Button, ElementKeys.Panel, ElementKeys.Label, ElementKeys.Focus, ElementKeys.Window };
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
            string[] elements = { ElementKeys.Button, ElementKeys.Panel, ElementKeys.Label, ElementKeys.Window };

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

        [Theory]
        [MemberData(nameof(AllSkins))]
        public void The_window_caption_text_is_readable_against_the_caption(ISkin skin)
        {
            var window = skin.GetAppearance(ElementKeys.Window, ElementState.Normal);

            // Nur "irgendein anderer ARGB-Wert" lässt zwei gleich helle, aber
            // andersfarbige Töne durchgehen — auf einer echten Titelleiste kaum
            // auseinanderzuhalten. Lesbar heißt: ein echter Helligkeitsabstand.
            // Genau davon hängt SkinnedForm.IsDarkCaption ab (Helligkeitsvergleich),
            // also prüft dieser Test dieselbe Größe.
            float difference = Math.Abs(window.Background.GetBrightness() - window.ForeColor.GetBrightness());
            Assert.True(difference > 0.2f,
                "Titelleiste und Titeltext müssen sich deutlich in der Helligkeit unterscheiden, sonst ist der Titel nicht lesbar. Unterschied war " + difference);
        }

        [Fact]
        public void The_dark_window_caption_is_darker_than_the_light_one()
        {
            var light = new LightSkin().GetAppearance(ElementKeys.Window, ElementState.Normal);
            var dark = new DarkSkin().GetAppearance(ElementKeys.Window, ElementState.Normal);

            Assert.True(dark.Background.GetBrightness() < light.Background.GetBrightness());
        }

        [Theory]
        [MemberData(nameof(AllSkins))]
        public void The_window_row_does_not_deny_the_border_it_will_get_anyway(ISkin skin)
        {
            var window = skin.GetAppearance(ElementKeys.Window, ElementState.Normal);

            // BorderWidth steuert für Window nichts — die Rahmengeometrie
            // gehört Windows (siehe ElementKeys.Window). Aber BorderColor geht
            // sehr wohl an DWMWA_BORDER_COLOR, und Windows zeichnet daraufhin
            // einen Rahmen exakt 1 logische Einheit breit. Der Wert muss also
            // 1 sein, nicht bloß ungleich 0: Wer ihn im Skin-Editor
            // (Teilprojekt 6) sieht, soll ablesen, was Windows tatsächlich
            // zeichnet. Bedeutungslos heißt nicht beliebig, solange der Wert
            // jemandem angezeigt wird.
            Assert.Equal(1, window.BorderWidth);
        }

        [Theory]
        [MemberData(nameof(AllSkins))]
        public void A_label_does_not_paint_a_box_onto_the_panel_it_sits_on(ISkin skin)
        {
            // Ein SkinLabel sitzt fast immer auf einem SkinPanel. Malt es einen
            // anderen Ton, erscheint um jeden Text ein sichtbarer Kasten.
            //
            // Kein anderer Test kann das sehen, und das ist strukturell: Alle
            // prüfen jede Erscheinung nur gegen sich selbst (Deckkraft, Schrift,
            // Text gegen den EIGENEN Hintergrund), nie gegen die Erscheinung,
            // auf der sie liegt. Genau in diese Lücke ist der dunkle Skin
            // gefallen — sein Label behielt die Grundfläche, während Panel und
            // Window auf die erhöhte wechselten.
            //
            // Beide Label-Zustände gegen Panel/Normal: Ein deaktiviertes Label
            // sitzt üblicherweise auf einem normalen Panel (WinForms deaktiviert
            // nicht das Panel, nur weil das Label deaktiviert ist).
            //
            // Transparent wäre ebenfalls in Ordnung — dann malt das Label gar
            // nichts und die Panelfarbe scheint durch (SkinPainter.DrawBackground
            // steigt bei A == 0 aus). Diese Freiheit steht hier offen, damit ein
            // künftiger Skin sie nutzen darf.
            var panel = skin.GetAppearance(ElementKeys.Panel, ElementState.Normal);

            foreach (var state in new[] { ElementState.Normal, ElementState.Disabled })
            {
                var label = skin.GetAppearance(ElementKeys.Label, state);

                Assert.True(
                    label.Background.A == 0 || label.Background == panel.Background,
                    skin.Name + ": Label/" + state + " malt " + label.Background +
                    ", das Panel darunter aber " + panel.Background + ".");
            }
        }
    }
}
