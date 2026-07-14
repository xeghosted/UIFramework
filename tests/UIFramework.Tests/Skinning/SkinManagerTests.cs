using System;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Skinning
{
    [Collection(SkinManagerCollection.Name)]
    public class SkinManagerTests : IDisposable
    {
        public SkinManagerTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void There_is_always_a_current_skin_even_untouched()
        {
            Assert.NotNull(SkinManager.Current);
        }

        [Fact]
        public void Setting_the_current_skin_takes_effect()
        {
            var skin = new StubSkin(Color.Red, "Test");

            SkinManager.Current = skin;

            Assert.Same(skin, SkinManager.Current);
        }

        [Fact]
        public void Setting_the_current_skin_to_null_is_a_programming_error()
        {
            Assert.Throws<ArgumentNullException>(() => SkinManager.Current = null);
        }

        [Fact]
        public void Changing_the_skin_raises_SkinChanged()
        {
            int raised = 0;
            EventHandler handler = (s, e) => raised++;
            SkinManager.SkinChanged += handler;

            try
            {
                SkinManager.Current = new StubSkin(Color.Red);

                Assert.Equal(1, raised);
            }
            finally
            {
                SkinManager.SkinChanged -= handler;
            }
        }

        [Fact]
        public void Setting_the_same_skin_again_raises_nothing()
        {
            var skin = new StubSkin(Color.Red);
            SkinManager.Current = skin;

            int raised = 0;
            EventHandler handler = (s, e) => raised++;
            SkinManager.SkinChanged += handler;

            try
            {
                SkinManager.Current = skin;

                Assert.Equal(0, raised);
            }
            finally
            {
                SkinManager.SkinChanged -= handler;
            }
        }

        [Fact]
        public void Registering_a_control_counts_it()
        {
            using (var control = new Control())
            {
                SkinManager.Register(control);

                Assert.Equal(1, SkinManager.RegisteredCount);
            }
        }

        [Fact]
        public void Registering_the_same_control_twice_counts_it_once()
        {
            using (var control = new Control())
            {
                SkinManager.Register(control);
                SkinManager.Register(control);

                Assert.Equal(1, SkinManager.RegisteredCount);
            }
        }

        [Fact]
        public void Unregistering_removes_it()
        {
            using (var control = new Control())
            {
                SkinManager.Register(control);
                SkinManager.Unregister(control);

                Assert.Equal(0, SkinManager.RegisteredCount);
            }
        }

        [Fact]
        public void A_collected_control_does_not_keep_being_registered()
        {
            RegisterAControlAndLetItGo();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Das ist der eigentliche Punkt der schwachen Referenzen: selbst wenn
            // Unregister nie gerufen wird, hält der SkinManager nichts am Leben.
            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RegisterAControlAndLetItGo()
        {
            var control = new Control();
            SkinManager.Register(control);
            // Bewusst kein Dispose, bewusst kein Unregister.
        }

        [Fact]
        public void Registering_null_is_a_programming_error()
        {
            Assert.Throws<ArgumentNullException>(() => SkinManager.Register(null));
        }
    }
}
