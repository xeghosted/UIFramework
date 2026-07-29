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
            string[] elements =
            {
                ElementKeys.Button, ElementKeys.Panel, ElementKeys.Label, ElementKeys.Focus, ElementKeys.Window,
                ElementKeys.Grid, ElementKeys.GridHeader, ElementKeys.GridCell,
                ElementKeys.ScrollBar, ElementKeys.ScrollBarThumb,
                ElementKeys.TextBox, ElementKeys.ComboBox, ElementKeys.Tab,
                ElementKeys.EditorButton, ElementKeys.CheckBox, ElementKeys.CheckBoxIndicator,
                ElementKeys.CalendarDay, ElementKeys.CalendarHeader, ElementKeys.CalendarToday,
                ElementKeys.MenuBar, ElementKeys.MenuBarItem, ElementKeys.MenuPopup,
                ElementKeys.MenuItem, ElementKeys.MenuSeparator,
            };
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
            string[] elements =
            {
                ElementKeys.Button, ElementKeys.Panel, ElementKeys.Label, ElementKeys.Window,
                ElementKeys.Grid, ElementKeys.GridHeader, ElementKeys.GridCell,
                ElementKeys.ScrollBar, ElementKeys.ScrollBarThumb,
                ElementKeys.TextBox, ElementKeys.ComboBox, ElementKeys.Tab,
                ElementKeys.EditorButton, ElementKeys.CheckBox, ElementKeys.CheckBoxIndicator,
                ElementKeys.CalendarDay, ElementKeys.CalendarHeader, ElementKeys.CalendarToday,
                ElementKeys.MenuBar, ElementKeys.MenuBarItem, ElementKeys.MenuPopup,
                ElementKeys.MenuItem, ElementKeys.MenuSeparator,
            };

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

        [Theory]
        [MemberData(nameof(AllSkins))]
        public void Grid_text_is_readable_against_its_own_background(ISkin skin)
        {
            // Nur "irgendein anderer ARGB-Wert" ließe zwei gleich helle, aber
            // andersfarbige Töne durchgehen — auf einer echten Zelle kaum lesbar.
            // Dieselbe Schwelle wie beim Titeltext (siehe den Window-Test oben).
            foreach (var element in new[] { ElementKeys.GridHeader, ElementKeys.GridCell })
            {
                var appearance = skin.GetAppearance(element, ElementState.Normal);

                float distance = Math.Abs(
                    appearance.ForeColor.GetBrightness() - appearance.Background.GetBrightness());

                Assert.True(distance > 0.2f,
                    skin.Name + "/" + element + ": Text " + appearance.ForeColor +
                    " hebt sich zu wenig von " + appearance.Background + " ab (Abstand " + distance + ").");
            }
        }

        [Theory]
        [MemberData(nameof(AllSkins))]
        public void ComboBoxList_is_defined_and_opaque(ISkin skin)
        {
            var appearance = skin.GetAppearance(ElementKeys.ComboBoxList, ElementState.Normal);

            Assert.NotSame(SkinBase.FallbackAppearance, appearance);
            Assert.Equal(255, appearance.Background.A);
        }

        [Theory]
        [MemberData(nameof(AllSkins))]
        public void A_selected_cell_still_reads(ISkin skin)
        {
            // Der Selected-Zustand tauscht Fläche UND Text (siehe Spec: der
            // Zeilenhintergrund IST der Zellhintergrund). Wer nur die Fläche
            // umfärbt und die Textfarbe vergisst, bekommt dunkel auf dunkel —
            // und kein anderer Test sieht es.
            var selected = skin.GetAppearance(ElementKeys.GridCell, ElementState.Selected);

            float distance = Math.Abs(
                selected.ForeColor.GetBrightness() - selected.Background.GetBrightness());

            Assert.True(distance > 0.2f,
                skin.Name + ": ausgewählte Zelle — Text " + selected.ForeColor +
                " auf " + selected.Background + " (Abstand " + distance + ").");
        }

        [Theory]
        [MemberData(nameof(AllSkins))]
        public void The_checkedit_control_is_borderless_and_reads_like_a_label_while_its_indicator_keeps_the_border(ISkin skin)
        {
            // Befund F2: CheckEdit sollte wie ein normales Checkbox-Control
            // aussehen — kein Rahmen ums ganze Control, Text wie ein Label.
            // Nur die gezeichnete Box (CheckBoxIndicator) behält die klassische
            // Checkbox-Optik (Rahmen, eigene Fläche je Zustand).
            var label = skin.GetAppearance(ElementKeys.Label, ElementState.Normal);
            var checkBox = skin.GetAppearance(ElementKeys.CheckBox, ElementState.Normal);
            var indicator = skin.GetAppearance(ElementKeys.CheckBoxIndicator, ElementState.Normal);

            Assert.Equal(0, checkBox.BorderWidth);
            Assert.Equal(label.ForeColor, checkBox.ForeColor);

            // NotSame zuerst: sonst bestünde die BorderWidth-Prüfung unten
            // zufällig auch dann, wenn CheckBoxIndicator gar nicht definiert
            // wäre und nur auf die (ebenfalls BorderWidth==1) Notfarbe fiele.
            Assert.NotSame(SkinBase.FallbackAppearance, indicator);
            Assert.True(indicator.BorderWidth >= 1,
                skin.Name + ": CheckBoxIndicator/Normal sollte weiterhin einen Rahmen haben.");
        }

        [Theory]
        [MemberData(nameof(AllSkins))]
        public void Menu_item_text_reads_in_normal_and_hovered_state(ISkin skin)
        {
            // Hovered ist bei Menüs der EINE Hervorhebungszustand (Maus UND
            // Tastatur-Auswahl teilen ihn) — unlesbar hieße: blindes Navigieren.
            foreach (var state in new[] { ElementState.Normal, ElementState.Hovered })
            {
                var appearance = skin.GetAppearance(ElementKeys.MenuItem, state);

                float distance = Math.Abs(
                    appearance.ForeColor.GetBrightness() - appearance.Background.GetBrightness());

                Assert.True(distance > 0.2f,
                    skin.Name + "/MenuItem/" + state + ": Text " + appearance.ForeColor +
                    " auf " + appearance.Background + " (Abstand " + distance + ").");
            }
        }
    }
}
