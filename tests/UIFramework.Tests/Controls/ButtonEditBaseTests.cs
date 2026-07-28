using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Controls
{
    [Collection(SkinManagerCollection.Name)]
    public class ButtonEditBaseTests : IDisposable
    {
        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        private sealed class TwoButtonEditor : ButtonEditBase
        {
            public int UpClicks;
            public int DownClicks;
            public int Confirms;

            public TwoButtonEditor()
            {
                AddButton(EditorGlyph.ArrowUp, () => UpClicks++);
                AddButton(EditorGlyph.ArrowDown, () => DownClicks++);
            }

            protected override string ElementKey
            {
                get { return ElementKeys.TextBox; }
            }

            protected override bool IsCharAllowed(char c)
            {
                return char.IsDigit(c);
            }

            protected override void OnEditConfirmed()
            {
                Confirms++;
            }
        }

        [Fact]
        public void Buttons_sit_right_aligned_as_squares_of_the_content_height()
        {
            using (var editor = new TwoButtonEditor())
            {
                editor.Size = new Size(200, 30);
                editor.CreateControl();

                var first = editor.ButtonBoundsForTests(0);    // ganz rechts
                var second = editor.ButtonBoundsForTests(1);   // links daneben

                Assert.Equal(first.Height, first.Width);
                Assert.Equal(second.Height, second.Width);
                Assert.Equal(first.Left, second.Right);
                Assert.True(first.Right <= editor.ClientRectangle.Right);
            }
        }

        [Fact]
        public void The_text_core_keeps_clear_of_the_button_zone()
        {
            using (var editor = new TwoButtonEditor())
            {
                editor.Size = new Size(200, 30);
                editor.CreateControl();

                var second = editor.ButtonBoundsForTests(1);
                Assert.True(editor.InnerTextBoxForTestsBounds().Right <= second.Left);
            }
        }

        [Fact]
        public void Clicking_a_button_fires_its_action()
        {
            using (var editor = new TwoButtonEditor())
            {
                editor.Size = new Size(200, 30);
                editor.CreateControl();

                editor.ClickButtonForTests(0);
                editor.ClickButtonForTests(1);
                editor.ClickButtonForTests(1);

                Assert.Equal(1, editor.UpClicks);
                Assert.Equal(2, editor.DownClicks);
            }
        }

        [Fact]
        public void Confirming_raises_hook_and_public_event()
        {
            using (var editor = new TwoButtonEditor())
            {
                int publicRaises = 0;
                editor.EditConfirmed += (s, e) => publicRaises++;

                editor.ConfirmForTests();

                Assert.Equal(1, editor.Confirms);
                Assert.Equal(1, publicRaises);
            }
        }

        [Fact]
        public void The_char_filter_of_the_derived_class_is_asked()
        {
            using (var editor = new TwoButtonEditor())
            {
                Assert.False(editor.FilterBlocksForTests('5'));
                Assert.True(editor.FilterBlocksForTests('x'));
                Assert.False(editor.FilterBlocksForTests('\b'));   // Steuertasten passieren immer
            }
        }
    }
}
