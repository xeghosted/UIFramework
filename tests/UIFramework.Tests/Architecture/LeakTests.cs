using System;
using System.Runtime.CompilerServices;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Architecture
{
    [Collection(SkinManagerCollection.Name)]
    public class LeakTests : IDisposable
    {
        public LeakTests()
        {
            SkinManager.ResetForTests();
        }

        public void Dispose()
        {
            SkinManager.ResetForTests();
        }

        [Fact]
        public void A_thousand_disposed_controls_leave_nothing_registered()
        {
            for (int i = 0; i < 1000; i++)
            {
                using (var button = new SkinButton())
                using (var panel = new SkinPanel())
                using (var label = new SkinLabel())
                {
                    button.Text = "x";
                    label.Text = "y";
                    panel.Controls.Add(button);
                }
            }

            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [Fact]
        public void A_thousand_forgotten_controls_leave_nothing_registered_either()
        {
            CreateAndForget(1000);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Ohne die schwachen Referenzen stünde hier 1000 — und jedes dieser
            // Controls hinge für die Lebensdauer der App im Speicher.
            Assert.Equal(0, SkinManager.RegisteredCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateAndForget(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var button = new SkinButton();
                GC.KeepAlive(button);
                // Bewusst kein Dispose: prüft das Netz, nicht den Normalfall.
            }
        }

        [Fact]
        public void Switching_the_skin_a_thousand_times_does_not_grow_the_registration_list()
        {
            using (var button = new SkinButton())
            {
                for (int i = 0; i < 1000; i++)
                {
                    SkinManager.Current = new StubSkin(
                        System.Drawing.Color.FromArgb(255, i % 256, 0, 0), "Skin" + i);
                }

                Assert.Equal(1, SkinManager.RegisteredCount);
            }
        }
    }
}
