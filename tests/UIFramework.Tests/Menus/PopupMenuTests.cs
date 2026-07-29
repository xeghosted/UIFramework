using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Skinning;
using UIFramework.Tests.TestSupport;
using Xunit;

namespace UIFramework.Tests.Menus
{
    [Collection(SkinManagerCollection.Name)]
    public class PopupMenuTests : IDisposable
    {
        private readonly Control _owner = new Control();

        public void Dispose()
        {
            _owner.Dispose();
            SkinManager.ResetForTests();
        }

        [Fact]
        public void Show_opens_one_level_at_the_given_location()
        {
            using (var menu = new PopupMenu())
            {
                menu.Items.Add(new MenuEntry("&Kopieren"));
                _owner.CreateControl();

                menu.Show(_owner, new Point(200, 200));

                Assert.True(menu.ControllerForTests.IsOpen);
                Assert.Equal(1, menu.ControllerForTests.ChainDepth);
                Assert.Equal(-1, menu.ControllerForTests.BarIndex);   // kein Leisten-Kontext
            }
        }

        [Fact]
        public void Show_with_empty_items_does_nothing()
        {
            using (var menu = new PopupMenu())
            {
                _owner.CreateControl();

                menu.Show(_owner, new Point(200, 200));

                Assert.Null(menu.ControllerForTests);                  // gar nicht erst erzeugt
            }
        }

        [Fact]
        public void Show_requires_an_owner()
        {
            using (var menu = new PopupMenu())
            {
                menu.Items.Add(new MenuEntry("X"));

                Assert.Throws<ArgumentNullException>(() => menu.Show(null, Point.Empty));
            }
        }

        [Fact]
        public void A_second_Show_replaces_the_open_chain()
        {
            using (var menu = new PopupMenu())
            {
                menu.Items.Add(new MenuEntry("X"));
                _owner.CreateControl();

                menu.Show(_owner, new Point(200, 200));
                menu.Show(_owner, new Point(300, 300));

                Assert.True(menu.ControllerForTests.IsOpen);
                Assert.Equal(1, menu.ControllerForTests.ChainDepth);
            }
        }

        [Fact]
        public void Dispose_closes_an_open_menu()
        {
            var menu = new PopupMenu();
            menu.Items.Add(new MenuEntry("X"));
            _owner.CreateControl();
            menu.Show(_owner, new Point(200, 200));
            var controller = menu.ControllerForTests;

            menu.Dispose();

            Assert.False(controller.IsOpen);
        }

        [Fact]
        public void ShowBelow_opens_left_aligned_under_the_anchor()
        {
            using (var menu = new PopupMenu())
            {
                menu.Items.Add(new MenuEntry("&Eins"));
                _owner.CreateControl();

                menu.ShowBelow(_owner, new Rectangle(300, 200, 120, 30));

                Assert.True(menu.ControllerForTests.IsOpen);
                Assert.Equal(1, menu.ControllerForTests.ChainDepth);
            }
        }

        [Fact]
        public void ShowBelow_requires_an_owner_and_ignores_empty_items()
        {
            using (var menu = new PopupMenu())
            {
                Assert.Throws<ArgumentNullException>(
                    () => menu.ShowBelow(null, new Rectangle(0, 0, 10, 10)));
                _owner.CreateControl();
                menu.ShowBelow(_owner, new Rectangle(0, 0, 10, 10));   // leer -> No-op
                Assert.Null(menu.ControllerForTests);
            }
        }
    }
}
