using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Interop;
using UIFramework.Core.Skinning;
using UIFramework.Core.Skinning.Skins;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class SkinnedFormTests : IDisposable
    {
        public SkinnedFormTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void A_form_registers_itself_with_the_skin_manager()
        {
            using (new SkinnedForm())
            {
                Assert.Equal(1, SkinManager.RegisteredCount);
            }
        }

        [Fact]
        public void A_disposed_form_is_no_longer_registered()
        {
            var form = new SkinnedForm();
            form.Dispose();

            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [Fact]
        public void Without_a_handle_nothing_is_pushed_to_the_window_manager()
        {
            using (var form = new SkinnedForm())
            {
                // Kein Handle erzwungen: es gibt kein Fenster zum Einfärben.
                Assert.Equal(0, form.CaptionApplyCount);
            }
        }

        [Fact]
        public void Creating_the_handle_applies_the_caption_once()
        {
            using (var form = new SkinnedForm())
            {
                var unused = form.Handle;

                Assert.Equal(1, form.CaptionApplyCount);
            }
        }

        [Fact]
        public void Repainting_without_a_skin_change_does_not_touch_the_window_manager_again()
        {
            using (var form = new SkinnedForm())
            {
                var unused = form.Handle;
                int afterHandle = form.CaptionApplyCount;

                form.Invalidate();
                form.Invalidate();
                form.Invalidate();

                // Der Merker muss greifen: ohne ihn ginge bei jedem Repaint
                // ein Schwung P/Invoke-Aufrufe raus.
                Assert.Equal(afterHandle, form.CaptionApplyCount);
            }
        }

        [Fact]
        public void Switching_the_skin_applies_the_new_caption()
        {
            using (var form = new SkinnedForm())
            {
                var unused = form.Handle;
                int afterHandle = form.CaptionApplyCount;

                SkinManager.Current = new DarkSkin();

                Assert.Equal(afterHandle + 1, form.CaptionApplyCount);
            }
        }

        [Fact]
        public void The_forms_own_background_takes_the_windows_appearance()
        {
            using (var form = new SkinnedForm())
            {
                var unused = form.Handle;

                var appearance = SkinManager.Current.GetAppearance(ElementKeys.Window, ElementState.Normal);

                // Nicht nur die Titelleiste: die Fläche des Fensters selbst
                // soll denselben Ton tragen, sonst blitzt an ungeskinnten
                // Rändern (z. B. den abgerundeten Ecken eines SkinPanel mit
                // Dock=Fill) das Windows-Standardgrau durch.
                Assert.Equal(appearance.Background, form.BackColor);
            }
        }

        [Fact]
        public void Switching_the_skin_updates_the_forms_background_too()
        {
            using (var form = new SkinnedForm())
            {
                var unused = form.Handle;

                SkinManager.Current = new DarkSkin();

                var appearance = SkinManager.Current.GetAppearance(ElementKeys.Window, ElementState.Normal);

                Assert.Equal(appearance.Background, form.BackColor);
            }
        }

        [Fact]
        public void Recreating_the_handle_reapplies_the_caption_to_the_new_window()
        {
            using (var form = new SkinnedForm())
            {
                var unused = form.Handle;
                int afterFirstHandle = form.CaptionApplyCount;
                IntPtr firstHandle = form.Handle;

                // ShowInTaskbar erzwingt bei WinForms ein neues HWND
                // (RecreateHandle) — ein alltäglicher Fall, kein Sonderweg
                // (RightToLeft tut dasselbe). Das neue Fenster trägt zunächst
                // wieder die helle Windows-Standardleiste; ohne das
                // Zurücksetzen von _applied in OnHandleCreated würde der
                // ReferenceEquals-Merker das erneute Anwenden überspringen,
                // weil er noch die alte Appearance-Instanz kennt.
                form.ShowInTaskbar = false;

                Assert.NotEqual(firstHandle, form.Handle);
                Assert.Equal(afterFirstHandle + 1, form.CaptionApplyCount);
            }
        }

        [Fact]
        public void A_skin_change_pushes_the_right_colour_to_the_right_dwm_attribute()
        {
            var calls = new List<Tuple<int, int>>();
            Func<IntPtr, int, int, bool> original = Dwm.Setter;
            try
            {
                using (var form = new SkinnedForm())
                {
                    var unused = form.Handle;

                    // Erst jetzt einhängen: die Handle-Erzeugung oben soll nicht
                    // mitgezählt werden, nur der Skin-Wechsel unten.
                    Dwm.Setter = (handle, attribute, value) =>
                    {
                        calls.Add(Tuple.Create(attribute, value));
                        return true;
                    };

                    SkinManager.Current = new DarkSkin();
                    var appearance = new DarkSkin().GetAppearance(ElementKeys.Window, ElementState.Normal);

                    // attribute(35) = Titelleiste ← Background,
                    // attribute(36) = Titeltext   ← ForeColor,
                    // attribute(34) = Rahmen      ← BorderColor,
                    // attribute(20) = Dark-Mode-Schalter ← 1, weil DarkSkin
                    // eine dunkle Leiste ist (siehe IsDarkCaption).
                    Assert.Contains(Tuple.Create(35, Dwm.ToColorRef(appearance.Background)), calls);
                    Assert.Contains(Tuple.Create(36, Dwm.ToColorRef(appearance.ForeColor)), calls);
                    Assert.Contains(Tuple.Create(34, Dwm.ToColorRef(appearance.BorderColor)), calls);
                    Assert.Contains(Tuple.Create(20, 1), calls);
                }
            }
            finally
            {
                // Prozessweiter Zustand: ein fehlschlagender Test darf die
                // restliche Suite nicht vergiften.
                Dwm.Setter = original;
            }
        }

        [Fact]
        public void A_dark_caption_is_detected_from_the_skin_colours_alone()
        {
            // Heller Text auf dunkler Leiste heißt: dunkle Leiste.
            var dark = new ElementAppearance
            {
                Background = Color.FromArgb(255, 20, 20, 20),
                ForeColor = Color.FromArgb(255, 240, 240, 240)
            };

            var light = new ElementAppearance
            {
                Background = Color.FromArgb(255, 240, 240, 240),
                ForeColor = Color.FromArgb(255, 20, 20, 20)
            };

            Assert.True(SkinnedForm.IsDarkCaption(dark));
            Assert.False(SkinnedForm.IsDarkCaption(light));
        }

        [Fact]
        public void The_built_in_skins_are_classified_the_way_a_human_would()
        {
            var light = new LightSkin().GetAppearance(ElementKeys.Window, ElementState.Normal);
            var dark = new DarkSkin().GetAppearance(ElementKeys.Window, ElementState.Normal);

            Assert.False(SkinnedForm.IsDarkCaption(light));
            Assert.True(SkinnedForm.IsDarkCaption(dark));
        }

        /// <summary>Testklasse: ein Control, das IShortcutHandler implementiert und
        /// die zuletzt gefragte Tastenkombination merkt — steht für eine MenuBar
        /// oder einen anderen künftigen Shortcut-Handler, ohne dass dieses Projekt
        /// (Core) die Controls-Assembly kennen muss.</summary>
        private sealed class RecordingShortcutHandler : Control, IShortcutHandler
        {
            public Keys LastKeyData { get; private set; }

            public bool ProcessShortcut(Keys keyData)
            {
                LastKeyData = keyData;
                return true;
            }
        }

        [Fact]
        public void ProcessCmdKey_reaches_a_shortcut_handler_anywhere_in_the_tree()
        {
            using (var form = new SkinnedForm())
            {
                var panel = new Panel();
                var handler = new RecordingShortcutHandler();        // private Testklasse: Control + IShortcutHandler
                panel.Controls.Add(handler);
                form.Controls.Add(panel);

                Assert.True(form.PerformShortcutForTests(Keys.Control | Keys.Q));
                Assert.Equal(Keys.Control | Keys.Q, handler.LastKeyData);
            }
        }

        [Fact]
        public void ProcessCmdKey_without_a_handler_declines()
        {
            using (var form = new SkinnedForm())
            {
                Assert.False(form.PerformShortcutForTests(Keys.Control | Keys.Q));
            }
        }
    }
}
