using System;
using System.Drawing;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class SpinEditTests : IDisposable
    {
        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void Setting_Value_clamps_and_raises_the_event_once()
        {
            using (var spin = new SpinEdit { MinValue = 0, MaxValue = 10 })
            {
                int raises = 0;
                spin.ValueChanged += (s, e) => raises++;

                spin.Value = 42;

                Assert.Equal(10, spin.Value);
                Assert.Equal(1, raises);

                spin.Value = 10;   // gleicher (geklemmter) Wert: kein zweites Ereignis
                Assert.Equal(1, raises);
            }
        }

        [Fact]
        public void The_up_button_steps_by_Increment_and_the_down_button_back()
        {
            using (var spin = new SpinEdit { MinValue = 0, MaxValue = 10, Increment = 3 })
            {
                spin.ClickButtonForTests(0);   // Knopf 0 = ganz rechts = Auf
                Assert.Equal(3, spin.Value);

                spin.ClickButtonForTests(1);   // Ab
                Assert.Equal(0, spin.Value);

                spin.ClickButtonForTests(1);   // klemmt an MinValue
                Assert.Equal(0, spin.Value);
            }
        }

        [Fact]
        public void Confirming_parses_the_text_and_falls_back_when_unparsable()
        {
            using (var spin = new SpinEdit { MinValue = 0, MaxValue = 100, Value = 7 })
            {
                spin.SetTextForTests("42");
                spin.ConfirmForTests();
                Assert.Equal(42, spin.Value);

                spin.SetTextForTests("quark");
                spin.ConfirmForTests();
                Assert.Equal(42, spin.Value);                    // stiller Rückfall
                Assert.Equal("42", spin.TextForTests());         // Text zeigt wieder den Wert
            }
        }

        [Fact]
        public void Raising_MinValue_pulls_the_current_value_up()
        {
            using (var spin = new SpinEdit { MinValue = 0, MaxValue = 10, Value = 2 })
            {
                spin.MinValue = 5;
                Assert.Equal(5, spin.Value);
            }
        }
    }
}
