using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UIFramework.Controls;
using UIFramework.Core.Controls;
using UIFramework.Core.Dpi;
using UIFramework.Core.Rendering;
using UIFramework.Core.Skinning;
using UIFramework.Grid.Layout;

namespace UIFramework.Grid
{
    /// <summary>
    /// Ein virtualisiertes Grid: Es zeichnet nur, was man sieht — auch bei einer
    /// Million Zeilen.
    ///
    /// Rechnet selbst nichts. Sichtfenster, Spaltenlage, Treffer und Auswahl
    /// liegen in RowViewport, ColumnLayout, GridHitTest und GridSelection, die
    /// alle ohne Fenster prüfbar sind. Dieses Control verdrahtet sie und zeichnet.
    ///
    /// Zellen sind keine Controls: Ein Control je Zelle wäre bei einer Million
    /// Zeilen nicht bloß langsam, sondern unmöglich. Deshalb ist SkinPainter
    /// statisch — er bekommt Graphics, Rectangle und ElementAppearance, sonst
    /// nichts (siehe Core-Spec, "Offene Punkte für Teilprojekt 2").
    /// </summary>
    [ToolboxItem(true)]
    public class GridControl : SkinnedControl
    {
        private IGridDataSource _dataSource;
        private int _verticalOffset;
        private int _horizontalOffset;

        private readonly SkinScrollBar _vertical;
        private readonly SkinScrollBar _horizontal;

        /// <summary>
        /// Schneidet die Rückkopplung durch: Leiste meldet Scroll → Grid setzt
        /// Versatz → Grid gleicht Leiste ab → Leiste meldet Scroll. Ohne diesen
        /// Merker eine Endlosschleife. Dasselbe Muster wie der
        /// ReferenceEquals-Merker in SkinnedForm.ApplyCaptionIfChanged.
        /// </summary>
        private bool _syncing;

        private IGridCellEditor _editor;
        private int _editRow = -1;
        private int _editColumn = -1;
        private bool _closingEditor;

        public GridControl()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;

            // VOR Size: Das Setzen von Size löst synchron OnSizeChanged aus, und
            // dessen ClampOffsets liest CurrentColumnLayout, das wiederum Columns
            // braucht. Zu spät initialisiert, träfe das eine ArgumentNullException
            // mitten im Konstruktor.
            Columns = new GridColumnCollection();
            Columns.Changed += OnColumnsChanged;

            Selection = new GridSelection();
            Selection.Changed += OnSelectionChanged;

            _vertical = new SkinScrollBar { Orientation = Orientation.Vertical, Visible = false };
            _vertical.Scroll += (s, e) => { if (!_syncing) VerticalOffset = _vertical.Value; };
            Controls.Add(_vertical);

            _horizontal = new SkinScrollBar { Orientation = Orientation.Horizontal, Visible = false };
            _horizontal.Scroll += (s, e) => { if (!_syncing) HorizontalOffset = _horizontal.Value; };
            Controls.Add(_horizontal);

            Size = new Size(400, 300);
        }

        protected override string ElementKey
        {
            get { return ElementKeys.Grid; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IGridDataSource DataSource
        {
            get { return _dataSource; }
            set
            {
                if (ReferenceEquals(_dataSource, value)) return;

                // Abbruch OHNE Schreiben (Spec 3b): Die alte Quelle ist Geschichte —
                // in sie zu schreiben wäre genauso falsch wie in die neue.
                CancelEdit();

                _dataSource = value;

                // Zeilenindizes bedeuten nach einem Quellenwechsel nichts mehr
                // Verlaessliches -- eine sortierte oder gefilterte Ansicht
                // mischt Zeilen oder laesst sie aus. TrimTo waere hier die
                // falsche Antwort: Es liesse eine noch-im-Bereich-liegende,
                // aber jetzt FALSCHE Zeile markiert stehen. Eine neue Quelle
                // ist ein Neuanfang fuer die Auswahl.
                Selection.Clear();
                ClampOffsets();
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public GridColumnCollection Columns { get; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GridSelection Selection { get; }

        /// <summary>
        /// Die Zeilenhöhe in physischen Pixeln — aus dem Skin abgeleitet, nicht
        /// gesetzt: Texthöhe der Zellschrift plus deren Innenabstand, DPI-skaliert.
        /// Genau der Weg, den SkinButton.GetPreferredSize geht. Damit folgt die
        /// Zeilenhöhe dem Skin, ohne dass dieses Control eine Zahl kennt.
        /// </summary>
        [Browsable(false)]
        public int RowHeight
        {
            get { return MeasuredHeight(ElementKeys.GridCell); }
        }

        /// <summary>Die Kopfhöhe in physischen Pixeln — analog zu <see cref="RowHeight"/>.</summary>
        [Browsable(false)]
        public int HeaderHeight
        {
            get { return MeasuredHeight(ElementKeys.GridHeader); }
        }

        /// <summary>Senkrechter Versatz in physischen Pixeln.</summary>
        internal int VerticalOffset
        {
            get { return _verticalOffset; }
            set
            {
                int clamped = ClampVertical(value);
                if (_verticalOffset == clamped) return;

                _verticalOffset = clamped;
                Invalidate();
                SyncScrollBars();
            }
        }

        /// <summary>Waagerechter Versatz in physischen Pixeln.</summary>
        internal int HorizontalOffset
        {
            get { return _horizontalOffset; }
            set
            {
                int clamped = ClampHorizontal(value);
                if (_horizontalOffset == clamped) return;

                _horizontalOffset = clamped;
                Invalidate();
                SyncScrollBars();
            }
        }

        /// <summary>Die senkrechte Bildlaufleiste — für Tests.</summary>
        internal SkinScrollBar VerticalScrollBar
        {
            get { return _vertical; }
        }

        /// <summary>Die waagerechte Bildlaufleiste — für Tests.</summary>
        internal SkinScrollBar HorizontalScrollBar
        {
            get { return _horizontal; }
        }

        internal RowViewport CurrentRowViewport
        {
            get { return new RowViewport(RowHeight, CurrentReservation.ViewportHeight, _verticalOffset, RowCount); }
        }

        internal ColumnLayout CurrentColumnLayout
        {
            get { return new ColumnLayout(Columns, _horizontalOffset, CurrentReservation.ViewportWidth, DeviceDpi); }
        }

        /// <summary>
        /// Sichtbarkeit der Leisten und die dem Inhalt verbleibende Fläche.
        /// Die Sonden mit Sichtfenster 0 sind billig und bewusst: TotalHeight/
        /// TotalWidth hängen vom Sichtfenster nicht ab, und die Gesamtmaß-
        /// Formeln bleiben so an ihrer einen Stelle statt hier dupliziert.
        /// Misst RowHeight/HeaderHeight bei jedem Zugriff neu, kein Cache-Feld
        /// (Begründung siehe DrawTo). Anders als offene-punkte.md bisher
        /// behauptete, betrifft das GDI+-Rauschen damit jetzt auch den
        /// Zeichenpfad: sowohl CurrentRowViewport als auch CurrentColumnLayout
        /// lesen hier durch — macht zusammen rund sieben statt vormals vier
        /// Messungen je Bild.
        /// </summary>
        internal ScrollBarReservation CurrentReservation
        {
            get
            {
                int rowHeight = RowHeight;
                int contentHeight = new RowViewport(rowHeight, 0, 0, RowCount).TotalHeight;
                int contentWidth = new ColumnLayout(Columns, 0, 0, DeviceDpi).TotalWidth;

                return new ScrollBarReservation(
                    ClientSize.Width, ClientSize.Height, HeaderHeight,
                    contentWidth, contentHeight,
                    DpiScale.Scale(SkinScrollBar.ThicknessLogical, DeviceDpi));
            }
        }

        private int RowCount
        {
            get { return _dataSource == null ? 0 : _dataSource.RowCount; }
        }

        /// <summary>
        /// Zeichnet das Grid. Öffentlich, damit ein Test es ohne Fenster in eine
        /// Bitmap zeichnen kann — genau so misst GridVirtualizationTests, dass
        /// nur sichtbare Zellen gelesen werden. Ohne diese Naht könnte kein Test
        /// die Virtualisierung je prüfen, denn die Suite läuft kopflos.
        /// </summary>
        public void DrawTo(Graphics g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));

            int dpi = DeviceDpi;
            var grid = SkinManager.Current.GetAppearance(ElementKeys.Grid, ElementState.Normal);

            SkinPainter.DrawBackground(g, ClientRectangle, grid, dpi);

            // Die beiden Höhen hier EINMAL messen und an DrawHeader/DrawRows als
            // Parameter durchreichen, statt sie dort je erneut zu lesen. Jedes
            // Lesen von RowHeight/HeaderHeight erzeugt eine Bitmap samt
            // Graphics. Das erspart die Messungen NUR innerhalb der beiden
            // Draw-Methoden — die gleich folgenden CurrentColumnLayout/
            // CurrentRowViewport lesen über CurrentReservation seit der
            // Bildlaufleisten-Reservierung beide zusätzlich erneut, macht
            // zusammen rund sieben statt vormals vier Messungen je Bild (siehe
            // offene-punkte.md). Kein Korrektheitsproblem, aber ein Kandidat
            // dafür, die Reservierung ebenfalls hier einmal zu messen und
            // durchzureichen, sollte es je auffallen. Kein Cache-Feld: das
            // müsste bei Skin- UND DPI-Wechsel verworfen werden, und wer eins
            // vergisst, hat nach dem Monitorwechsel eine falsche Zeilenhöhe,
            // die kein Test bei 96 dpi sieht.
            int headerHeight = HeaderHeight;
            int rowHeight = RowHeight;

            var columns = CurrentColumnLayout;
            DrawHeader(g, columns, dpi, headerHeight);
            DrawRows(g, columns, dpi, headerHeight, rowHeight);

            SkinPainter.DrawBorder(g, ClientRectangle, grid, dpi);
        }

        /// <summary>
        /// Zeichnet das Grid und sonst nichts. Ruft bewusst NICHT base.OnPaint:
        /// SkinnedControl malte dort Hintergrund und Rahmen des Grid-Elements,
        /// die DrawTo ohnehin malt — die volle Fläche käme je Bild zweimal unter
        /// den Pinsel, in genau dem Control, dessen Zweck Geschwindigkeit ist.
        ///
        /// Dass DrawTo damit vollständig ist, ist zugleich die Bedingung dafür,
        /// dass GridVirtualizationTests etwas wert ist: Der Test misst exakt den
        /// Pfad, den auch das echte Fenster nimmt.
        ///
        /// Kein Fokusring: Beim Grid zeigt die Auswahl, wo man ist.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            DrawTo(e.Graphics);
        }

        private void DrawHeader(Graphics g, ColumnLayout columns, int dpi, int height)
        {
            var appearance = SkinManager.Current.GetAppearance(ElementKeys.GridHeader, ElementState.Normal);

            for (int i = 0; i < columns.VisibleColumnCount; i++)
            {
                int index = columns.FirstVisibleColumn + i;
                var bounds = new Rectangle(columns.ColumnLeft(index), 0, columns.ColumnWidth(index), height);

                SkinPainter.DrawBackground(g, bounds, appearance, dpi);
                SkinPainter.DrawBorder(g, bounds, appearance, dpi);

                string text = Columns[index].Header;
                if (!string.IsNullOrEmpty(text))
                    SkinPainter.DrawPaddedText(g, text, bounds, appearance, dpi, ContentAlignment.MiddleLeft);
            }

            if (_reorderTarget >= 0 && _reorderingColumn >= 0 && _reorderTarget != _reorderingColumn)
            {
                // Die Marke sitzt an der Kante, an der die Spalte landen wird:
                // beim Ziehen nach rechts hinter dem Ziel, nach links davor.
                int edge = _reorderTarget > _reorderingColumn
                    ? columns.ColumnLeft(_reorderTarget) + columns.ColumnWidth(_reorderTarget)
                    : columns.ColumnLeft(_reorderTarget);

                var marker = SkinManager.Current.GetAppearance(ElementKeys.GridHeader, ElementState.Pressed);
                int width = DpiScale.Scale(2, dpi);

                SkinPainter.DrawBackground(g, new Rectangle(edge - width / 2, 0, width, height), marker, dpi);
            }
        }

        private void DrawRows(Graphics g, ColumnLayout columns, int dpi, int headerHeight, int rowHeight)
        {
            if (_dataSource == null || Columns.Count == 0) return;

            var rows = CurrentRowViewport;

            var normal = SkinManager.Current.GetAppearance(ElementKeys.GridCell, ElementState.Normal);
            var selected = SkinManager.Current.GetAppearance(ElementKeys.GridCell, ElementState.Selected);
            var disabled = SkinManager.Current.GetAppearance(ElementKeys.GridCell, ElementState.Disabled);

            // Die Erscheinungen EINMAL vor der Schleife holen, nicht je Zelle:
            // GetAppearance ist zwar nur ein Dictionary-Zugriff, aber bei 30 Zeilen
            // mal 10 Spalten mal 60 Bildern je Sekunde sind das 18.000 Zugriffe je
            // Sekunde für dieselben drei Werte.
            for (int r = 0; r < rows.VisibleRowCount; r++)
            {
                int rowIndex = rows.FirstVisibleRow + r;
                int top = headerHeight + rows.RowTop(rowIndex);

                var appearance = !Enabled
                    ? disabled
                    : Selection.IsSelected(rowIndex) ? selected : normal;

                for (int c = 0; c < columns.VisibleColumnCount; c++)
                {
                    int columnIndex = columns.FirstVisibleColumn + c;
                    var bounds = new Rectangle(
                        columns.ColumnLeft(columnIndex), top,
                        columns.ColumnWidth(columnIndex), rowHeight);

                    SkinPainter.DrawBackground(g, bounds, appearance, dpi);
                    SkinPainter.DrawBorder(g, bounds, appearance, dpi);

                    object value = _dataSource.GetValue(rowIndex, Columns[columnIndex].Key);
                    if (value == null) continue;

                    SkinPainter.DrawPaddedText(g, value.ToString(), bounds, appearance, dpi,
                                               ContentAlignment.MiddleLeft);
                }
            }
        }

        private int MeasuredHeight(string elementKey)
        {
            var appearance = SkinManager.Current.GetAppearance(elementKey, ElementState.Normal);

            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                var textSize = SkinPainter.MeasureText(g, "Xg", appearance, DeviceDpi);
                return SkinPainter.InflateByPadding(textSize, appearance, DeviceDpi).Height;
            }
        }

        private int ClampVertical(int value)
        {
            int max = CurrentRowViewport.MaxScrollOffset;
            if (value < 0) return 0;
            return value > max ? max : value;
        }

        private int ClampHorizontal(int value)
        {
            int max = CurrentColumnLayout.MaxScrollOffset;
            if (value < 0) return 0;
            return value > max ? max : value;
        }

        private void ClampOffsets()
        {
            _verticalOffset = ClampVertical(_verticalOffset);
            _horizontalOffset = ClampHorizontal(_horizontalOffset);
            SyncScrollBars();
        }

        /// <summary>
        /// Bringt die Leisten auf den Stand des Grids. Setzt _syncing, damit das
        /// dabei ausgelöste Scroll nicht zurückschlägt.
        /// </summary>
        private void SyncScrollBars()
        {
            if (_vertical == null || _horizontal == null) return;   // während des Konstruktors

            _syncing = true;
            try
            {
                var reservation = CurrentReservation;
                var rows = CurrentRowViewport;
                var columns = CurrentColumnLayout;

                int barThickness = DpiScale.Scale(SkinScrollBar.ThicknessLogical, DeviceDpi);

                // Bereich und Wert IMMER nachziehen, auch wenn die Leiste gerade
                // verschwindet: Sonst bliebe ihr Value auf dem Stand von vor dem
                // Schrumpfen stehen (unsichtbar, aber mit einem Wert, der zur
                // neuen Quelle nicht mehr passt) und risse beim naechsten
                // Sichtbarwerden einen falschen Sprung mit sich.
                _vertical.Visible = reservation.VerticalVisible;
                _vertical.Bounds = new Rectangle(
                    ClientSize.Width - barThickness, HeaderHeight,
                    barThickness, reservation.ViewportHeight > 0 ? reservation.ViewportHeight : 1);
                _vertical.Minimum = 0;
                _vertical.Maximum = rows.TotalHeight;
                _vertical.LargeChange = reservation.ViewportHeight > 0 ? reservation.ViewportHeight : 1;
                _vertical.SmallChange = RowHeight;
                _vertical.Value = _verticalOffset;

                // Die waagerechte Leiste endet an der nutzbaren Breite — sonst
                // läge ihr Ende unter der senkrechten Leiste in der Ecke.
                _horizontal.Visible = reservation.HorizontalVisible;
                _horizontal.Bounds = new Rectangle(
                    0, ClientSize.Height - barThickness,
                    reservation.ViewportWidth > 0 ? reservation.ViewportWidth : 1, barThickness);
                _horizontal.Minimum = 0;
                _horizontal.Maximum = columns.TotalWidth;
                _horizontal.LargeChange = reservation.ViewportWidth > 0 ? reservation.ViewportWidth : 1;
                _horizontal.SmallChange = RowHeight;
                _horizontal.Value = _horizontalOffset;
            }
            finally
            {
                _syncing = false;
            }
        }

        /// <summary>
        /// Ein Mausrad-Schritt über dem Grid. Öffentlich wie bei SkinScrollBar,
        /// damit ein Test ihn ohne echtes Rad auslösen kann.
        /// </summary>
        public void PerformWheel(int delta)
        {
            _vertical.PerformWheel(delta);
            VerticalOffset = _vertical.Value;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            PerformWheel(e.Delta);
            base.OnMouseWheel(e);
        }

        /// <summary>Greifbreite einer Spaltentrennlinie, logisch.</summary>
        private const int DividerGripLogical = 4;

        /// <summary>
        /// Ein Klick an dieser Stelle. Öffentlich, damit ein Test klicken kann,
        /// ohne ein Fenster zu erzeugen — die Suite läuft kopflos.
        /// </summary>
        public void PerformClick(Point point, Keys modifiers)
        {
            // Klick auf eine andere Zelle (oder daneben) bestätigt den laufenden
            // Editor (Spec 3b, Lebenszyklus) — BEVOR die Auswahl wandert.
            CommitEdit();

            var hit = HitTestAt(point);

            switch (hit.Region)
            {
                case GridRegion.Cell:
                    if ((modifiers & Keys.Control) == Keys.Control) Selection.Toggle(hit.RowIndex);
                    else if ((modifiers & Keys.Shift) == Keys.Shift) Selection.ExtendTo(hit.RowIndex);
                    else Selection.Select(hit.RowIndex);
                    break;

                case GridRegion.EmptyBelowRows:
                    // Bewusst daneben geklickt — nicht die letzte Zeile wählen.
                    Selection.Clear();
                    break;

                // Header und HeaderDivider fassen die Auswahl nicht an. Ziehen
                // laeuft ueber BeginReorder/BeginResize (siehe OnMouseDown, die
                // vor PerformClick greifen); ein Kopf-Klick ohne Zug meldet sich
                // stattdessen als HeaderClick aus EndReorder (Teilprojekt 2b) --
                // PerformClick erreicht GridRegion.Header darum nie ueber die Maus.
            }
        }

        /// <summary>Läuft gerade eine Zellbearbeitung? Pro Grid höchstens eine.</summary>
        [Browsable(false)]
        public bool IsEditing
        {
            get { return _editor != null; }
        }

        /// <summary>
        /// Bearbeitbar ist eine Zelle genau dann, wenn: Quelle schreibbar UND
        /// Spalte nicht ReadOnly UND Spalte hat eine Editor-Fabrik (Spec 3b).
        /// Sonst verhält sich das Grid exakt wie vor 3b.
        /// </summary>
        internal bool CanEditCell(int rowIndex, int columnIndex)
        {
            if (!Enabled) return false;
            if (rowIndex < 0 || rowIndex >= RowCount) return false;
            if (columnIndex < 0 || columnIndex >= Columns.Count) return false;
            if (!(_dataSource is IWritableGridDataSource)) return false;

            var column = Columns[columnIndex];
            return !column.ReadOnly && column.EditorFactory != null;
        }

        /// <summary>Das Zellrechteck in Client-Koordinaten — dieselben Quellen wie
        /// das Zeichnen (ColumnLayout/RowViewport), damit Editor und Zelle nie
        /// auseinanderlaufen.</summary>
        internal Rectangle CellBounds(int rowIndex, int columnIndex)
        {
            var columns = CurrentColumnLayout;
            var rows = CurrentRowViewport;

            return new Rectangle(
                columns.ColumnLeft(columnIndex),
                HeaderHeight + rows.RowTop(rowIndex),
                columns.ColumnWidth(columnIndex),
                RowHeight);
        }

        public void BeginEdit(int rowIndex, int columnIndex)
        {
            BeginEdit(rowIndex, columnIndex, null);
        }

        /// <summary>
        /// Öffnet den Zelleditor über der Zelle — DataGridView-Mechanik: ein
        /// echtes Kind-Control exakt auf dem Zellrechteck, kein In-die-Zelle-Malen.
        /// seedText ist das Lostippen-Zeichen (ersetzt den geladenen Wert).
        /// </summary>
        public void BeginEdit(int rowIndex, int columnIndex, string seedText)
        {
            if (!CanEditCell(rowIndex, columnIndex)) return;

            CommitEdit();                 // höchstens ein Editor je Grid
            EnsureRowVisible(rowIndex);   // erst scrollen, DANN platzieren

            var editor = Columns[columnIndex].EditorFactory();
            if (editor == null || editor.EditorControl == null) return;

            _editor = editor;
            _editRow = rowIndex;
            _editColumn = columnIndex;

            editor.EditValue = _dataSource.GetValue(rowIndex, Columns[columnIndex].Key);
            if (seedText != null) editor.BeginWith(seedText);

            var control = editor.EditorControl;
            control.Bounds = CellBounds(rowIndex, columnIndex);

            editor.ConfirmRequested += OnEditorConfirmRequested;
            editor.CancelRequested += OnEditorCancelRequested;
            control.PreviewKeyDown += OnEditorPreviewKeyDown;

            Controls.Add(control);
            control.BringToFront();       // über den Leisten-Geschwistern

            // Kern-Editoren sind selbst nicht fokussierbar — dort übernimmt das
            // erste fokussierbare Kind (der native Kern) per SelectNextControl.
            if (!control.Focus()) control.SelectNextControl(null, true, true, true, false);
        }

        public void CommitEdit()
        {
            if (_editor == null || _closingEditor) return;

            object value = _editor.EditValue;

            // Schreiben VOR dem Schließen: Wirft die App-Quelle, schlägt die
            // Ausnahme durch und der Editor bleibt ehrlich offen (Spec 3b,
            // Fehlerbehandlung) — nichts wird verschluckt oder halb geschlossen.
            ((IWritableGridDataSource)_dataSource).SetValue(_editRow, Columns[_editColumn].Key, value);

            CloseEditor();
        }

        public void CancelEdit()
        {
            if (_editor == null || _closingEditor) return;
            CloseEditor();
        }

        private void CloseEditor()
        {
            _closingEditor = true;
            try
            {
                var editor = _editor;
                var control = editor.EditorControl;

                _editor = null;
                _editRow = -1;
                _editColumn = -1;

                // Erst abhängen, dann entfernen: Das Entfernen löst Fokuswechsel
                // aus, deren LostFocus-Confirm sonst reentrant zurückschlüge.
                editor.ConfirmRequested -= OnEditorConfirmRequested;
                editor.CancelRequested -= OnEditorCancelRequested;
                control.PreviewKeyDown -= OnEditorPreviewKeyDown;

                Controls.Remove(control);
                control.Dispose();

                Invalidate();
                if (CanFocus) Focus();
            }
            finally
            {
                _closingEditor = false;
            }
        }

        private void OnEditorConfirmRequested(object sender, EventArgs e)
        {
            CommitEdit();
        }

        private void OnEditorCancelRequested(object sender, EventArgs e)
        {
            CancelEdit();
        }

        private void OnEditorPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            // Tab-Wanderung kommt in Task 6 — der Handler existiert ab hier, damit
            // An-/Abmelden symmetrisch bleiben.
        }

        // ---- Nur für Tests --------------------------------------------------

        internal IGridCellEditor CurrentEditorForTests
        {
            get { return _editor; }
        }

        internal int EditRowForTests
        {
            get { return _editRow; }
        }

        internal int EditColumnForTests
        {
            get { return _editColumn; }
        }

        internal GridHit HitTestAt(Point point)
        {
            return GridHitTest.At(point, HeaderHeight, CurrentRowViewport, CurrentColumnLayout,
                                  DpiScale.Scale(DividerGripLogical, DeviceDpi));
        }

        private int _resizingColumn = -1;

        internal bool IsResizing
        {
            get { return _resizingColumn >= 0; }
        }

        private int _reorderingColumn = -1;
        private int _reorderTarget = -1;

        internal int ReorderingColumn
        {
            get { return _reorderingColumn; }
        }

        /// <summary>Wo die Einfügemarke steht. -1, wenn gerade keine gilt.</summary>
        internal int ReorderTargetIndex
        {
            get { return _reorderTarget; }
        }

        /// <summary>Greift einen Spaltenkopf zum Umordnen — nicht an der Trennlinie, dort wird gezogen.</summary>
        public void BeginReorder(Point point)
        {
            var hit = HitTestAt(point);

            _reorderingColumn = hit.Region == GridRegion.Header && hit.ColumnIndex >= 0
                ? hit.ColumnIndex
                : -1;
            _reorderTarget = -1;
        }

        public void DragReorder(Point point)
        {
            if (_reorderingColumn < 0) return;

            var hit = HitTestAt(point);

            // Außerhalb des Kopfes gibt es kein Ziel — die Marke verschwindet und
            // ein Loslassen bricht ab, statt irgendwohin zu fallen.
            int target = hit.Region == GridRegion.Header || hit.Region == GridRegion.HeaderDivider
                ? hit.ColumnIndex
                : -1;

            if (_reorderTarget == target) return;

            _reorderTarget = target;
            Invalidate();
        }

        public void EndReorder()
        {
            if (_reorderingColumn >= 0)
            {
                if (_reorderTarget >= 0 && _reorderTarget != _reorderingColumn)
                {
                    Columns.Move(_reorderingColumn, _reorderTarget);
                }
                else
                {
                    // Kein Zug fand statt -- ein reiner Klick auf den Kopf.
                    // Das ist der Sortierklick von Teilprojekt 2b: GridControl
                    // kennt kein Sortieren, meldet nur, dass hier geklickt
                    // wurde. PerformClick erreicht GridRegion.Header nie, weil
                    // BeginReorder jedem Kopf-Klick zuvorkommt (siehe dort).
                    OnHeaderClick(_reorderingColumn);
                }
            }

            _reorderingColumn = -1;
            _reorderTarget = -1;
            Invalidate();
        }

        /// <summary>
        /// Ein Kopf wurde angeklickt, nicht gezogen. GridControl kennt weder
        /// Sortieren noch SortedSource -- nur diese eine, sortierunabhaengige
        /// Meldung. Was der Klick bedeutet, entscheidet der Aufrufer.
        /// </summary>
        public event EventHandler<int> HeaderClick;

        protected virtual void OnHeaderClick(int columnIndex)
        {
            var handler = HeaderClick;
            if (handler != null) handler(this, columnIndex);
        }

        /// <summary>Greift eine Trennlinie, falls dort eine liegt.</summary>
        public void BeginResize(Point point)
        {
            var hit = HitTestAt(point);
            _resizingColumn = hit.Region == GridRegion.HeaderDivider ? hit.ColumnIndex : -1;
        }

        /// <summary>
        /// Zieht die gegriffene Trennlinie hierher. Die Maus liefert physische
        /// Pixel, GridColumn.Width ist logisch — hier treffen beide Welten
        /// aufeinander, und nur hier wird zurückgerechnet.
        /// </summary>
        public void DragResize(Point point)
        {
            if (_resizingColumn < 0) return;

            var columns = CurrentColumnLayout;
            int physical = point.X - columns.ColumnLeft(_resizingColumn);
            if (physical < 1) physical = 1;

            // Die Umkehrung von DpiScale.Scale. Über ScaleF, nicht über eigene
            // Arithmetik — die DPI-Rechnung gehört in die DPI-Schicht.
            float factor = DpiScale.ScaleF(1f, DeviceDpi);
            int logical = (int)Math.Round(physical / factor, MidpointRounding.AwayFromZero);

            // Die Klemmung auf MinWidth macht GridColumn selbst.
            Columns[_resizingColumn].Width = logical;
        }

        public void EndResize()
        {
            _resizingColumn = -1;
        }

        /// <summary>
        /// Passt die Spaltenbreite an — an den Kopf und die AKTUELL SICHTBAREN
        /// Zeilen, nicht an alle.
        ///
        /// Das ist keine Bequemlichkeit, sondern die Bedingung dafür, dass dieses
        /// Grid sein Versprechen hält: Die breiteste Zelle unter einer Million zu
        /// suchen hieße eine Million Textmessungen — ein Doppelklick, der die
        /// Anwendung sekundenlang einfriert, ausgerechnet hier. Die Breite passt
        /// damit zu dem, was der Anwender in diesem Moment vor sich hat; scrollt
        /// er weiter und klickt erneut, ergibt sich eine andere. Das ist die
        /// ehrliche Folge und kein Fehler.
        /// </summary>
        public void AutoFitColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= Columns.Count) return;

            var column = Columns[columnIndex];
            var headerAppearance = SkinManager.Current.GetAppearance(ElementKeys.GridHeader, ElementState.Normal);
            var cellAppearance = SkinManager.Current.GetAppearance(ElementKeys.GridCell, ElementState.Normal);

            int widest = 0;

            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))
            {
                if (!string.IsNullOrEmpty(column.Header))
                {
                    var size = SkinPainter.MeasureText(g, column.Header, headerAppearance, DeviceDpi);
                    widest = SkinPainter.InflateByPadding(size, headerAppearance, DeviceDpi).Width;
                }

                if (_dataSource != null)
                {
                    var rows = CurrentRowViewport;

                    for (int r = 0; r < rows.VisibleRowCount; r++)
                    {
                        int rowIndex = rows.FirstVisibleRow + r;
                        object value = _dataSource.GetValue(rowIndex, column.Key);
                        if (value == null) continue;

                        var size = SkinPainter.MeasureText(g, value.ToString(), cellAppearance, DeviceDpi);
                        int width = SkinPainter.InflateByPadding(size, cellAppearance, DeviceDpi).Width;
                        if (width > widest) widest = width;
                    }
                }
            }

            if (widest <= 0) return;

            float factor = DpiScale.ScaleF(1f, DeviceDpi);
            column.Width = (int)Math.Round(widest / factor, MidpointRounding.AwayFromZero);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (IsResizing)
            {
                DragResize(e.Location);
            }
            else if (_reorderingColumn >= 0)
            {
                DragReorder(e.Location);
            }
            else
            {
                var hit = HitTestAt(e.Location);
                Cursor = hit.Region == GridRegion.HeaderDivider ? Cursors.VSplit : Cursors.Default;
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                EndResize();
                EndReorder();
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            var hit = HitTestAt(e.Location);
            if (hit.Region == GridRegion.HeaderDivider) AutoFitColumn(hit.ColumnIndex);
            else if (hit.Region == GridRegion.Cell) BeginEdit(hit.RowIndex, hit.ColumnIndex);

            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (CanFocus) Focus();

                BeginResize(e.Location);
                if (IsResizing) { base.OnMouseDown(e); return; }

                BeginReorder(e.Location);
                if (_reorderingColumn >= 0) { base.OnMouseDown(e); return; }

                PerformClick(e.Location, ModifierKeys);
            }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// Ein Tastendruck. Öffentlich, damit ein Test tippen kann, ohne ein
        /// Fenster und eine Nachrichtenschleife zu haben.
        /// </summary>
        public void PerformKey(Keys keyData)
        {
            int rowCount = RowCount;
            if (rowCount == 0) return;

            var key = keyData & Keys.KeyCode;

            if (key == Keys.F2)
            {
                BeginEditAtCurrentRow(null);
                return;
            }

            bool shift = (keyData & Keys.Shift) == Keys.Shift;

            int current = Selection.CurrentRow;
            int perPage = CurrentRowViewport.VisibleRowCount;
            if (perPage < 1) perPage = 1;

            int target;

            switch (key)
            {
                case Keys.Down:
                    target = current < 0 ? 0 : current + 1;
                    break;
                case Keys.Up:
                    target = current < 0 ? 0 : current - 1;
                    break;
                case Keys.PageDown:
                    target = current < 0 ? 0 : current + (perPage - 1);
                    break;
                case Keys.PageUp:
                    target = current < 0 ? 0 : current - (perPage - 1);
                    break;
                case Keys.Home:
                    target = 0;
                    break;
                case Keys.End:
                    target = rowCount - 1;
                    break;
                default:
                    return;
            }

            if (target < 0) target = 0;
            if (target >= rowCount) target = rowCount - 1;

            if (shift) Selection.ExtendTo(target);
            else Selection.Select(target);

            EnsureRowVisible(target);
        }

        /// <summary>
        /// Zieht die Sicht so weit mit, dass diese Zeile ganz im Bild liegt.
        /// Ohne das wandert die Auswahl bei jedem Pfeiltastendruck am Rand aus
        /// dem Sichtfenster, und der Anwender sieht seinen eigenen Cursor nicht.
        /// </summary>
        public void EnsureRowVisible(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= RowCount) return;

            int rowHeight = RowHeight;
            int viewportHeight = CurrentReservation.ViewportHeight;
            if (viewportHeight <= 0) return;

            int top = rowIndex * rowHeight;
            int bottom = top + rowHeight;

            if (top < _verticalOffset)
            {
                VerticalOffset = top;
            }
            else if (bottom > _verticalOffset + viewportHeight)
            {
                // Die Zeile soll UNTEN bündig stehen, nicht oben — sonst springt
                // die Sicht bei jedem Schritt um eine ganze Seite.
                VerticalOffset = bottom - viewportHeight;
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            var key = keyData & Keys.KeyCode;

            // Ohne das bekommt das Control die Pfeiltasten nie: WinForms deutet
            // sie sonst als Wechsel zum nächsten Control.
            switch (key)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            PerformKey(e.KeyData);
            base.OnKeyDown(e);
        }

        /// <summary>F2 und Lostippen: Die Auswahl ist zeilenbasiert (kein
        /// Spalten-Cursor) — bearbeitet wird die ERSTE bearbeitbare Spalte der
        /// aktuellen Zeile (Plan-Entscheidung 5).</summary>
        private void BeginEditAtCurrentRow(string seedText)
        {
            int row = Selection.CurrentRow;
            if (row < 0) return;

            for (int c = 0; c < Columns.Count; c++)
            {
                if (CanEditCell(row, c))
                {
                    BeginEdit(row, c, seedText);
                    return;
                }
            }
        }

        /// <summary>
        /// Lostippen: das erste Zeichen wandert in den Editor (ersetzt den Wert,
        /// Caret ans Ende — BeginWith). Öffentlich als Test-Naht wie PerformKey.
        /// Editoren ohne Textkern ignorieren den Seed; die Aktivierung entspricht
        /// dann F2 (Plan-Entscheidung 4).
        /// </summary>
        public void PerformTyping(char c)
        {
            if (char.IsControl(c)) return;
            if (IsEditing) return;

            BeginEditAtCurrentRow(c.ToString());
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            PerformTyping(e.KeyChar);
            base.OnKeyPress(e);
        }

        private void OnColumnsChanged(object sender, EventArgs e)
        {
            ClampOffsets();
            Invalidate();
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            Invalidate();
        }

        /// <summary>
        /// Die Leisten-Bounds setzt SyncScrollBars bereits in physischen Pixeln
        /// (über DeviceDpi). Die DPI-Autoskalierung der Form würde sie als
        /// gewöhnliche Kind-Bounds ein ZWEITES Mal skalieren und die senkrechte
        /// Leiste damit aus dem Client hinausschieben (bei 125 % lag sie auf
        /// x=1169 von 950 — am echten Fenster unsichtbar, bis der erste Resize
        /// sie zurückholte). Kein Test bei 96 dpi sieht das; der Scale()-Test in
        /// GridScrollingTests stellt es nach.
        /// </summary>
        protected override bool ScaleChildren
        {
            get { return false; }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            ClampOffsets();
            base.OnSizeChanged(e);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            // Zeilenhöhe und Spaltenbreiten sind logisch formuliert und wachsen
            // von selbst mit — aber der Versatz ist physisch und muss neu geklemmt
            // werden, sonst zeigt das Grid nach dem Monitorwechsel ins Leere.
            ClampOffsets();
            base.OnDpiChangedAfterParent(e);
        }
    }
}
