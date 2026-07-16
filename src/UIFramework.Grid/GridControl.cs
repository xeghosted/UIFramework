using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
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

                _dataSource = value;

                // Der alte Versatz zeigt womöglich weit hinter das Ende der neuen
                // Quelle — dann stünde das Grid vor einer leeren Fläche, ohne dass
                // der Anwender versteht, warum. Dasselbe für die Auswahl.
                ClampOffsets();
                Selection.TrimTo(RowCount);
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
            }
        }

        internal RowViewport CurrentRowViewport
        {
            get
            {
                int height = ClientSize.Height - HeaderHeight;
                return new RowViewport(RowHeight, height, _verticalOffset, RowCount);
            }
        }

        internal ColumnLayout CurrentColumnLayout
        {
            get { return new ColumnLayout(Columns, _horizontalOffset, ClientSize.Width, DeviceDpi); }
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

            // Die beiden Höhen EINMAL je Zeichendurchgang messen und durchreichen.
            // Jedes Lesen von RowHeight/HeaderHeight erzeugt eine Bitmap samt
            // Graphics; ungebremst gelesen (CurrentRowViewport, DrawHeader,
            // DrawRows) wären das ein halbes Dutzend je Bild. Kein Cache-Feld:
            // das müsste bei Skin- UND DPI-Wechsel verworfen werden, und wer
            // eins vergisst, hat nach dem Monitorwechsel eine falsche
            // Zeilenhöhe, die kein Test bei 96 dpi sieht.
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
