using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace UIFramework.Controls
{
    /// <summary>
    /// Schmale Schnittstelle auf die noch nicht existierende MenuBar (Task 9):
    /// So ist der Controller hier bereits mit einem Test-Fake baubar und
    /// testbar, ohne von der MenuBar abzuhängen.
    /// </summary>
    internal interface IBarHome
    {
        int BarItemCount { get; }
        IList<MenuEntry> BarItems(int index);
        Rectangle BarItemScreenBounds(int index);
        bool IsBarItemSelectable(int index);
    }

    /// <summary>
    /// Der Menü-Modus: öffnet/schließt die Popup-Kette, verdrahtet Maus- und
    /// Tastatur-Meldungen der einzelnen Level auf die reine MenuChainState-
    /// Maschine und setzt deren Entscheidungen in Fenster um (öffnen,
    /// schließen, platzieren). Ein MenuController pro offener Sitzung — die
    /// MenuBar (Task 9) und PopupMenu (Task 10) besitzen je eine Instanz.
    /// </summary>
    internal sealed class MenuController : IDisposable
    {
        /// <summary>Ein aufgeklapptes Level: sein Fenster, sein Inhalt und —
        /// für den Hover-Timer — der Elterneintrag, dessen Untermenü es zeigt
        /// (null bei Level 0). Damit erkennt der Timer, ob ein bereits offenes
        /// tieferes Level noch zum gerade gehoverten Eintrag passt oder
        /// veraltet (Wechsel auf einen anderen Elter derselben Ebene) ist.</summary>
        private sealed class ChainLevel
        {
            public PopupHost Host;
            public MenuContent Content;
            public MenuEntry ParentEntry;
        }

        private readonly Control _owner;
        private readonly List<ChainLevel> _chain = new List<ChainLevel>();
        private readonly Timer _hoverTimer;

        private Form _ownerForm;
        private bool _formHooked;
        private MenuModeFilter _filter;
        private MenuChainState _state;
        private IBarHome _bar;
        private int _hoverLevel = -1;
        private int _hoverIndex = -1;

        public event EventHandler Closed;

        public MenuController(Control owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            _owner = owner;
            BarIndex = -1;

            _hoverTimer = new Timer { Interval = 300 };
            _hoverTimer.Tick += (s, e) => OnHoverTimerTick();
        }

        public bool IsOpen { get { return _state != null; } }
        public int BarIndex { get; private set; }
        public int ChainDepth { get { return _chain.Count; } }

        private bool FilterInstalled { get { return _filter != null; } }

        // ---- Öffnen -----------------------------------------------------

        public void OpenBarDropdown(IBarHome bar, int barIndex, bool selectFirst)
        {
            if (bar == null) throw new ArgumentNullException(nameof(bar));

            CloseChainOnly();
            _ownerForm = _owner.FindForm();
            _bar = bar;
            BarIndex = barIndex;
            _state = new MenuChainState(bar.BarItemCount, barIndex);

            var anchorBounds = bar.BarItemScreenBounds(barIndex);
            var workArea = WorkArea();
            OpenLevel(bar.BarItems(barIndex), null, anchorBounds.Width,
                size => MenuPlacement.PlaceDropdown(anchorBounds, size, workArea));

            if (selectFirst)
            {
                _state.SelectFirstInDeepest();
                MirrorSelectionInDeepestContent();
            }

            InstallFilterIfNeeded();
            HookOwnerFormIfNeeded();
            if (!_owner.IsDisposed) _owner.Invalidate();
        }

        public void OpenContext(IList<MenuEntry> entries, Point screenLocation)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            CloseChainOnly();
            _ownerForm = _owner.FindForm();
            _bar = null;
            BarIndex = -1;
            _state = new MenuChainState(0, -1);

            var workArea = WorkArea();
            OpenLevel(entries, null, 0,
                size => MenuPlacement.PlaceContextMenu(screenLocation, size, workArea));

            InstallFilterIfNeeded();
            HookOwnerFormIfNeeded();
            if (!_owner.IsDisposed) _owner.Invalidate();
        }

        /// <summary>Öffnet EIN neues, tieferes Level: misst mit der Grafik/DPI
        /// des Besitzer-Controls, platziert über die übergebene Funktion (der
        /// Aufrufer kennt die passende MenuPlacement-Variante), zeigt das
        /// Popup nicht-aktivierend und verdrahtet dessen Ereignisse mit dem
        /// gefangenen Level-Index. parentEntry ist der Elter, dessen Kinder
        /// dieses Level zeigt (null bei einem Wurzel-Level).</summary>
        private void OpenLevel(IList<MenuEntry> entries, MenuEntry parentEntry, int anchorWidth,
            Func<Size, Rectangle> place)
        {
            int level = _chain.Count;
            var content = new MenuContent(entries);

            Size size;
            using (var g = _owner.CreateGraphics())
                size = content.Measure(g, _owner.DeviceDpi, anchorWidth);

            var bounds = place(size);
            var host = new PopupHost(content, true);
            content.HoveredIndexChanged += index => OnHover(level, index);
            content.EntryClicked += entry => OnEntryClicked(level, entry);

            host.ShowPopupAt(_ownerForm, bounds);

            _chain.Add(new ChainLevel { Host = host, Content = content, ParentEntry = parentEntry });
            _state.PushLevel(entries);
        }

        /// <summary>Platzierung eines Untermenüs rechts (oder links, wenn es
        /// rechts nicht passt) neben der Zeile seines Elterneintrags.</summary>
        private void OpenSubmenuFor(MenuEntry entry, bool selectFirst)
        {
            int parentLevel = _chain.Count - 1;
            int index = _chain[parentLevel].Content.Entries.IndexOf(entry);
            var rowBounds = ScreenRowBounds(parentLevel, index);
            var workArea = WorkArea();

            OpenLevel(entry.Items, entry, 0,
                size => MenuPlacement.PlaceSubmenu(rowBounds, size, workArea));

            if (selectFirst)
            {
                _state.SelectFirstInDeepest();
                MirrorSelectionInDeepestContent();
            }
        }

        // ---- Tastatur -----------------------------------------------------

        public void HandleKey(Keys keyData)
        {
            if (_state == null) return;

            var modifiers = keyData & Keys.Modifiers;
            var action = (modifiers != Keys.None && modifiers != Keys.Alt)
                ? MenuKeyAction.Of(MenuKeyActionKind.None)
                : _state.HandleKey(keyData & Keys.KeyCode);

            switch (action.Kind)
            {
                case MenuKeyActionKind.SelectionChanged:
                    MirrorSelectionInDeepestContent();
                    break;
                case MenuKeyActionKind.OpenSubmenu:
                    OpenSubmenuFor(action.Entry, true);
                    break;
                case MenuKeyActionKind.CloseLevel:
                    CloseDeepestPopupOnly();
                    break;
                case MenuKeyActionKind.CloseAll:
                    CloseAll();
                    break;
                case MenuKeyActionKind.SwitchBar:
                    OpenBarDropdown(_bar, action.NewBarIndex, true);
                    break;
                case MenuKeyActionKind.Execute:
                    ExecuteEntry(action.Entry);
                    break;
            }
        }

        // ---- Maus: Hover und Klick -----------------------------------------

        /// <summary>HoveredIndexChanged eines Levels: Auswahl im Modell nur bei
        /// einer echten Zeile setzen (index -1 heißt "Maus über keiner Zeile",
        /// das verwirft keine vorhandene Auswahl), danach IMMER spiegeln —
        /// auch tiefere, jetzt möglicherweise veraltete Level auf -1 — und den
        /// Hover-Timer für diese Zeile neu starten.</summary>
        private void OnHover(int level, int index)
        {
            if (index >= 0) _state.SetSelection(level, index);

            _chain[level].Content.SelectedIndex = _state.SelectedIndex(level);
            for (int i = level + 1; i < _chain.Count; i++)
                _chain[i].Content.SelectedIndex = -1;

            _hoverLevel = level;
            _hoverIndex = index;
            _hoverTimer.Stop();
            _hoverTimer.Start();
        }

        private void OnHoverTimerTick()
        {
            _hoverTimer.Stop();
            if (_hoverLevel < 0 || _hoverLevel >= _chain.Count) return;

            var entries = _chain[_hoverLevel].Content.Entries;
            if (_hoverIndex < 0 || _hoverIndex >= entries.Count) return;

            var entry = entries[_hoverIndex];
            if (!entry.IsSelectable) return;

            int childLevel = _hoverLevel + 1;

            if (entry.HasChildren)
            {
                bool submenuIsDeepest = childLevel < _chain.Count
                    && childLevel == _chain.Count - 1
                    && ReferenceEquals(_chain[childLevel].ParentEntry, entry);

                if (!submenuIsDeepest)
                {
                    CloseDeeperThan(_hoverLevel);
                    OpenSubmenuFor(entry, false);
                }
            }
            else if (childLevel < _chain.Count)
            {
                CloseDeeperThan(_hoverLevel);
            }
        }

        /// <summary>EntryClicked eines Levels: ein Elter klappt SEIN Untermenü
        /// sofort auf (kein Warten auf den Hover-Timer), ein Blatt führt
        /// direkt aus.</summary>
        private void OnEntryClicked(int level, MenuEntry entry)
        {
            if (entry.HasChildren)
            {
                _hoverTimer.Stop();
                CloseDeeperThan(level);
                OpenSubmenuFor(entry, false);
            }
            else
            {
                ExecuteEntry(entry);
            }
        }

        // ---- Ausführen ------------------------------------------------------

        /// <summary>Erst ALLES schließen (Filter weg, Fenster zu — ein
        /// werfender Handler kann so nichts vom Menü-Modus leaken), dann
        /// CheckOnClick togglen, dann Click feuern. Bewusst NICHT in einem
        /// eigenen try: Die Ausnahme des Handlers soll zur Anwendung
        /// durchschlagen, der Modus ist zu diesem Zeitpunkt bereits abgebaut.
        /// Auch von außerhalb einer offenen Kette aufrufbar (App-Kürzel bei
        /// geschlossenem Menü) — CloseAll() ist dafür idempotent.</summary>
        public void ExecuteEntry(MenuEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            CloseAll();
            if (entry.CheckOnClick) entry.Checked = !entry.Checked;
            entry.PerformClick();
        }

        // ---- Schließen ------------------------------------------------------

        /// <summary>Schließt die Popup-Kette, OHNE die Sitzung zu beenden —
        /// Filter, Timer-Existenz und Form-Hook bleiben unberührt, kein
        /// Closed-Ereignis. Für den unmittelbaren Wiederaufbau (neue Leiste
        /// per SwitchBar, neue Kontextmenü-Anfrage während eine andere noch
        /// offen ist).</summary>
        private void CloseChainOnly()
        {
            _hoverTimer.Stop();
            _hoverLevel = -1;
            _hoverIndex = -1;
            for (int i = _chain.Count - 1; i >= 0; i--)
            {
                _chain[i].Host.Close();
                _chain[i].Host.Dispose();
            }
            _chain.Clear();
            _state = null;
        }

        /// <summary>Schließt alle Level unterhalb (ausschließlich) level — für
        /// den Hover-Timer (veraltetes tieferes Level) und für einen sofortigen
        /// Klick auf einen anderen Elter derselben Ebene.</summary>
        private void CloseDeeperThan(int level)
        {
            _state.TruncateTo(level + 1);
            for (int i = _chain.Count - 1; i > level; i--)
            {
                _chain[i].Host.Close();
                _chain[i].Host.Dispose();
                _chain.RemoveAt(i);
            }
        }

        /// <summary>Nur das tiefste Popup schließen — für CloseLevel: Die
        /// Maschine hat zu diesem Zeitpunkt bereits gepoppt, hier folgt nur
        /// noch das Fenster.</summary>
        private void CloseDeepestPopupOnly()
        {
            int i = _chain.Count - 1;
            if (i < 0) return;
            _chain[i].Host.Close();
            _chain[i].Host.Dispose();
            _chain.RemoveAt(i);
        }

        public void CloseAll()
        {
            if (!IsOpen && !FilterInstalled) return;
            try
            {
                // Reihenfolge: erst den Filter (kein Routing mehr), dann Timer,
                // dann Fenster — ein Fehler beim Fenster-Schließen darf den
                // Filter nicht überleben lassen.
            }
            finally
            {
                RemoveFilterIfInstalled();
                _hoverTimer.Stop();
                _hoverLevel = -1;
                _hoverIndex = -1;
                UnhookOwnerForm();
                for (int i = _chain.Count - 1; i >= 0; i--)
                {
                    _chain[i].Host.Close();
                    _chain[i].Host.Dispose();
                }
                _chain.Clear();
                _state = null;
                BarIndex = -1;
                if (!_owner.IsDisposed) _owner.Invalidate();
                var closed = Closed;
                if (closed != null) closed(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            CloseAll();
            _hoverTimer.Dispose();
        }

        // ---- Hilfsmethoden ----------------------------------------------

        private Rectangle WorkArea()
        {
            return Screen.FromControl(_owner).WorkingArea;
        }

        /// <summary>Bildschirm-Rechteck einer Zeile: Zeilen-Rechteck des
        /// Contents (Popup-lokal) plus Fensterposition des Popups.</summary>
        private Rectangle ScreenRowBounds(int level, int index)
        {
            var chainLevel = _chain[level];
            var row = chainLevel.Content.RowBounds(index);
            var location = chainLevel.Host.Location;
            return new Rectangle(location.X + row.X, location.Y + row.Y, row.Width, row.Height);
        }

        private void MirrorSelectionInDeepestContent()
        {
            int deepest = _chain.Count - 1;
            if (deepest < 0) return;
            _chain[deepest].Content.SelectedIndex = _state.SelectedIndex(deepest);
        }

        private void InstallFilterIfNeeded()
        {
            if (_filter != null) return;
            _filter = new MenuModeFilter(this);
            Application.AddMessageFilter(_filter);
        }

        private void RemoveFilterIfInstalled()
        {
            if (_filter == null) return;
            Application.RemoveMessageFilter(_filter);
            _filter = null;
        }

        private void HookOwnerFormIfNeeded()
        {
            if (_formHooked) return;
            if (_ownerForm != null)
            {
                _ownerForm.Deactivate += OnOwnerFormDeactivateOrDisposed;
                _ownerForm.Disposed += OnOwnerFormDeactivateOrDisposed;
            }
            _formHooked = true;
        }

        private void UnhookOwnerForm()
        {
            if (!_formHooked) return;
            if (_ownerForm != null)
            {
                _ownerForm.Deactivate -= OnOwnerFormDeactivateOrDisposed;
                _ownerForm.Disposed -= OnOwnerFormDeactivateOrDisposed;
            }
            _formHooked = false;
        }

        private void OnOwnerFormDeactivateOrDisposed(object sender, EventArgs e)
        {
            CloseAll();
        }

        public bool IsWindowInChain(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;
            if (_owner.IsHandleCreated && _owner.Handle == hwnd) return true;

            for (int i = 0; i < _chain.Count; i++)
                if (_chain[i].Host.IsHandleCreated && _chain[i].Host.Handle == hwnd) return true;

            return false;
        }

        // ---- Nur für Tests --------------------------------------------------

        internal bool FilterInstalledForTests { get { return FilterInstalled; } }

        internal void FireHoverTimerForTests()
        {
            OnHoverTimerTick();
        }

        internal MenuContent ContentAtForTests(int level)
        {
            return _chain[level].Content;
        }

        internal void SimulateHoverForTests(int level, int index)
        {
            OnHover(level, index);
        }

        internal void SimulateEntryClickForTests(int level, int entryIndex)
        {
            OnEntryClicked(level, _chain[level].Content.Entries[entryIndex]);
        }
    }
}
