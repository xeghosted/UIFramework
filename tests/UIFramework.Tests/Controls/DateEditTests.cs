using System;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class DateEditTests : IDisposable
    {
        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void Setting_Value_writes_the_short_date_and_raises_once()
        {
            using (var edit = new DateEdit())
            {
                int raises = 0;
                edit.ValueChanged += (s, e) => raises++;

                edit.Value = new DateTime(2026, 7, 28);

                Assert.Equal(1, raises);
                Assert.False(string.IsNullOrEmpty(edit.TextForTests()));

                edit.Value = new DateTime(2026, 7, 28);
                Assert.Equal(1, raises);
            }
        }

        [Fact]
        public void Confirming_empty_text_clears_the_value()
        {
            using (var edit = new DateEdit { Value = new DateTime(2026, 7, 28) })
            {
                edit.SetTextForTests("");
                edit.ConfirmForTests();

                Assert.Null(edit.Value);
            }
        }

        [Fact]
        public void Confirming_garbage_falls_back_to_the_last_value()
        {
            var last = new DateTime(2026, 7, 28);
            using (var edit = new DateEdit { Value = last })
            {
                edit.SetTextForTests("kein datum");
                edit.ConfirmForTests();

                Assert.Equal(last, edit.Value);
                Assert.Equal(edit.FormatForTests(last), edit.TextForTests());
            }
        }

        [Fact]
        public void Clicking_the_calendar_button_while_the_popup_closes_underneath_does_not_reopen_it()
        {
            using (var edit = new DateEdit())
            {
                edit.CreateControl();

                edit.ClickButtonForTests(0);                   // öffnet den Kalender
                Assert.True(edit.IsPopupOpenForTests);

                edit.PressButtonForTests(0);                   // MouseDown auf dem Kalender-Knopf
                edit.RaisePopupDeactivateForTests();           // Popup verliert die Aktivierung ...
                Application.DoEvents();                        // ... aufgeschobenes Close (Task-12) läuft durch

                edit.ReleaseButtonForTests(0);                 // MouseUp: ToggleCalendar() feuert

                Assert.False(edit.IsPopupOpenForTests);        // darf NICHT wieder offen sein
            }
        }

        [Fact]
        public void Opening_the_calendar_first_commits_unconfirmed_typed_text()
        {
            using (var edit = new DateEdit { Value = new DateTime(2020, 1, 1) })
            {
                edit.SetTextForTests(edit.FormatForTests(new DateTime(2026, 7, 28)));

                edit.ClickButtonForTests(0);   // öffnet den Kalender

                // Sonst öffnete er auf 2020, obwohl gerade 2026-07-28 getippt wurde.
                Assert.Equal(new DateTime(2026, 7, 28), edit.Value);
            }
        }
    }
}
