using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Controls;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;

namespace UIFramework.Controls
{
    /// <summary>
    /// Die Menüleiste einer Anwendung: eine Reihe von Top-Level-Einträgen
    /// ("&amp;Datei", "&amp;Ansicht", …), deren Untermenüs über einen
    /// <see cref="MenuController"/> laufen. Drei Wege führen zu einem
    /// Eintrag: ein Klick (<see cref="OnMouseDown"/> öffnet/toggelt sein
    /// Dropdown), Alt+Mnemonic (<see cref="ProcessMnemonic"/> öffnet das
    /// passende Dropdown mit dem ersten Eintrag vorausgewählt) und ein
    /// App-weites Tastenkürzel (<see cref="ProcessShortcut"/> — feuert auch
    /// bei geschlossenem Menü, siehe SkinnedForm.ProcessCmdKey). Die Leiste
    /// selbst nimmt dabei NIE den Fokus (SetStyle(ControlStyles.Selectable,
    /// false) im Konstruktor, dieselbe Technik wie die waagerechte Grid-
    /// Bildlaufleiste) — solange eine Kette offen ist, bedient ausschließlich
    /// der MenuController über seinen MessageFilter die Tastatur.
    /// </summary>
    public class MenuBar : SkinnedControl, IShortcutHandler, IBarHome
    {
        private readonly List<MenuEntry> _items = new List<MenuEntry>();
        private readonly MenuController _controller;
        private Rectangle[] _itemBounds = new Rectangle[0];
        private int _hoverIndex = -1;

        public MenuBar()
        {
            // Kerninvariante der Spec: die Leiste nimmt nie den Fokus.
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;

            _controller = new MenuController(this);
            _controller.Closed += (s, e) => Invalidate();
        }

        /// <summary>Die Top-Level-Einträge, von links nach rechts.</summary>
        public IList<MenuEntry> Items
        {
            get { return _items; }
        }

        protected override string ElementKey
        {
            get { return ElementKeys.MenuBar; }
        }

        protected override bool ShowFocusRing
        {
            get { return false; }
        }

        protected override void PaintContent(Graphics g, ElementAppearance appearance)
        {
            var content = SkinPainter.GetContentRectangle(ClientRectangle, appearance, DeviceDpi);

            // Messen IMMER mit Normal (Muster MenuContent.Measure): sonst
            // sprängen Item-Breiten je nach Hover/Auswahl auseinander, weil
            // Padding/Rahmen einer anderen Erscheinung leicht abweichen könnten.
            var measureAppearance = SkinManager.Current.GetAppearance(ElementKeys.MenuBarItem, ElementState.Normal);

            _itemBounds = new Rectangle[_items.Count];
            int x = content.Left;

            for (int i = 0; i < _items.Count; i++)
            {
                var entry = _items[i];
                var itemSize = SkinPainter.InflateByPadding(
                    SkinPainter.MeasureMnemonicText(g, entry.Text, measureAppearance, DeviceDpi),
                    measureAppearance, DeviceDpi);

                var bounds = new Rectangle(x, content.Top, itemSize.Width, content.Height);
                _itemBounds[i] = bounds;
                x = bounds.Right;

                ElementState state;
                if (!entry.Enabled) state = ElementState.Disabled;
                else if (i == _controller.BarIndex) state = ElementState.Selected;
                else if (i == _hoverIndex) state = ElementState.Hovered;
                else state = ElementState.Normal;

                var itemAppearance = SkinManager.Current.GetAppearance(ElementKeys.MenuBarItem, state);
                SkinPainter.DrawBackground(g, bounds, itemAppearance, DeviceDpi);
                SkinPainter.DrawMnemonicText(g, entry.Text, bounds, itemAppearance, DeviceDpi,
                    ContentAlignment.MiddleCenter);
            }
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var itemAppearance = SkinManager.Current.GetAppearance(ElementKeys.MenuBarItem, ElementState.Normal);
            var barAppearance = CurrentAppearance;

            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                var textSize = SkinPainter.MeasureMnemonicText(g, "Xg", itemAppearance, DeviceDpi);
                var itemSize = SkinPainter.InflateByPadding(textSize, itemAppearance, DeviceDpi);
                var barSize = SkinPainter.InflateByPadding(itemSize, barAppearance, DeviceDpi);

                // Breite: Elternvorgabe — die Leiste ist so breit wie ihr Host
                // (typischerweise Dock=Top), nicht wie ihr eigener Inhalt.
                return new Size(proposedSize.Width, barSize.Height);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int hit = HitTest(e.Location);

            if (hit != _hoverIndex)
            {
                _hoverIndex = hit;
                Invalidate();

                // Hot-Tracking: bei offener Kette folgt das Dropdown der Maus
                // über die Leiste ohne erneuten Klick — nur beim tatsächlichen
                // Wechsel auf ein ANDERES selektierbares Item, sonst öffnete
                // jede Mausbewegung über demselben Item die Kette neu und würfe
                // die Hover-Auswahl im gerade offenen Dropdown weg.
                if (_controller.IsOpen && hit >= 0 && hit != _controller.BarIndex && IsBarItemSelectable(hit))
                    _controller.OpenBarDropdown(this, hit, false);
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int hit = HitTest(e.Location);
                if (hit >= 0 && IsBarItemSelectable(hit))
                {
                    if (_controller.IsOpen && _controller.BarIndex == hit)
                        _controller.CloseAll();
                    else
                        _controller.OpenBarDropdown(this, hit, false);
                }
            }

            base.OnMouseDown(e);
        }

        protected override bool ProcessMnemonic(char charCode)
        {
            char wanted = char.ToUpperInvariant(charCode);
            for (int i = 0; i < _items.Count; i++)
            {
                if (Mnemonics.FromText(_items[i].Text) != wanted) continue;
                if (!IsBarItemSelectable(i)) continue;

                _controller.OpenBarDropdown(this, i, true);
                return true;
            }
            // Gleiche Bedingung wie Klick und Hot-Tracking: ein Top-Level-
            // Eintrag ohne Kinder oder disabled klappt auch per Alt nichts auf.
            return base.ProcessMnemonic(charCode);
        }

        /// <summary>App-weites Kürzel — feuert auch bei geschlossenem Menü
        /// (SkinnedForm.ProcessCmdKey ruft dies über alle IShortcutHandler
        /// seiner Hierarchie).</summary>
        public bool ProcessShortcut(Keys keyData)
        {
            var hit = MenuShortcuts.Find(_items, keyData);
            if (hit == null) return false;

            _controller.ExecuteEntry(hit);
            return true;
        }

        private int HitTest(Point location)
        {
            for (int i = 0; i < _itemBounds.Length; i++)
                if (_itemBounds[i].Contains(location)) return i;
            return -1;
        }

        /// <summary>Ein Top-Level-Eintrag ohne Kinder klappt nichts auf — weder
        /// per Klick noch per Alt+Mnemonic noch per Hot-Tracking.</summary>
        private bool IsBarItemSelectable(int index)
        {
            return _items[index].IsSelectable && _items[index].HasChildren;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _controller.Dispose();
            base.Dispose(disposing);
        }

        // ---- IBarHome (internal — nur der MenuController kennt dieses Interface) --

        int IBarHome.BarItemCount
        {
            get { return _items.Count; }
        }

        IList<MenuEntry> IBarHome.BarItems(int index)
        {
            return _items[index].Items;
        }

        Rectangle IBarHome.BarItemScreenBounds(int index)
        {
            return RectangleToScreen(_itemBounds[index]);
        }

        bool IBarHome.IsBarItemSelectable(int index)
        {
            return IsBarItemSelectable(index);
        }

        // ---- Nur für Tests --------------------------------------------------

        internal Rectangle[] ItemBoundsForTests()
        {
            return _itemBounds;
        }

        internal MenuController ControllerForTests
        {
            get { return _controller; }
        }

        /// <summary>ProcessMnemonic ist protected und sonst nicht erreichbar.</summary>
        internal bool PerformMnemonicForTests(char charCode)
        {
            return ProcessMnemonic(charCode);
        }
    }
}
