using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using XHexEditor.Providers;

namespace XHexEditor.Wpf
{
    internal class HexEditorInternal : Panel, IScrollInfo, INotifyPropertyChanged
    {
        #region RenderInfo

        struct RenderInfo
        {
            public ArraySegment<byte> Buffer;
            public ProviderModifications Modifications;

            public int BytesPerLine;
            public int FirstDisplayedLine;
            public int DisplayedLineCount;

            public double LineHeight;

            public double GetLineYPosition(int line) => this.LineHeight * line;

            public bool TryGetBufferOffset(int line, out int offset)
            {
                offset = this.BytesPerLine * line;
                return (offset < this.Buffer.Count);
            }

            // returns a byte array containing the bytes for the line
            public ArraySegment<byte> GetLineBuffer(int line)
            {
                if (TryGetBufferOffset(line, out int offset))
                {
                    if (offset + this.BytesPerLine > this.Buffer.Count)
                        return this.Buffer.Slice(offset, this.Buffer.Count - offset);

                    return this.Buffer.Slice(offset, this.BytesPerLine);
                }

                return ArraySegment<byte>.Empty;
            }

            public IEnumerable<ProviderRange> GetLineModifications(int line)
            {
                long providerOffset = (this.FirstDisplayedLine + line) * this.BytesPerLine;

                foreach ((long curOffset, long count) in this.Modifications.GetModifications(providerOffset, this.BytesPerLine))
                {
                    long offset = curOffset - providerOffset;

                    yield return new ProviderRange((int)offset, (int)(offset + count - 1));
                }                
            }

            public long GetLineProviderOffset(int line) =>
                ((long)(this.FirstDisplayedLine + line)) * this.BytesPerLine;
        }

        #endregion

        #region Dependency properties

        public static readonly DependencyProperty ProviderProperty =
            DependencyProperty.Register(nameof(Provider), typeof(IProvider), typeof(HexEditorInternal),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnProviderChanged));

        public static readonly DependencyProperty HighlightBrushesProperty =
            DependencyProperty.Register(nameof(HighlightBrushes), typeof(ObservableCollection<Brush>), typeof(HexEditorInternal));

        public static readonly DependencyProperty HighlightRangesProperty =
            DependencyProperty.Register(nameof(HighlightRanges), typeof(HighlightRangeCollection), typeof(HexEditorInternal),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback((dObj, args) =>
                {
                    HexEditorInternal ctrl = (HexEditorInternal)dObj;

                    HighlightRangeCollection? oldColl = args.OldValue as HighlightRangeCollection;
                    if (oldColl != null)
                        oldColl.CollectionChanged -= ctrl.HighlightRangeCollection_CollectionChanged;

                    HighlightRangeCollection? newColl = args.NewValue as HighlightRangeCollection;
                    if (newColl != null)
                        newColl.CollectionChanged += ctrl.HighlightRangeCollection_CollectionChanged;

                })));

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(nameof(FontSize), typeof(int), typeof(HexEditorInternal),
                new FrameworkPropertyMetadata(10, (dObj, args) =>
                {
                    HexEditorInternal ctrl = (HexEditorInternal)dObj;
                    ctrl._textMetrics = new TextMetrics(HexEditorInternal.Typeface!, ctrl.FontSize, ctrl.FlowDirection);
                    ctrl._calculatedBytesPerLine = null;
                }));

        public static readonly DependencyProperty SelectedRangeProperty =
            DependencyProperty.Register(nameof(SelectedRange), typeof(ProviderRange), typeof(HexEditorInternal),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault|FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CaretModeProperty =
            DependencyProperty.Register(nameof(CaretMode), typeof(CaretMode), typeof(HexEditorInternal),
                new FrameworkPropertyMetadata(CaretMode.Overwrite, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, 
                                                (dObj, args) => ((HexEditorInternal)dObj).OnCaretPositionChanged()));

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(HexEditorInternal),
                new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    (dObj, args) => ((HexEditorInternal)dObj).SetZoom((double)args.NewValue)),
                    (value) => IsZoomValueValid((double)value));

        public static readonly DependencyProperty BookmarksProperty =
            DependencyProperty.Register(nameof(Bookmarks), typeof(BookmarkCollection), typeof(HexEditorInternal),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback((dObj, args) =>
                {
                    HexEditorInternal ctrl = (HexEditorInternal)dObj;

                    BookmarkCollection? oldColl = args.OldValue as BookmarkCollection;
                    if (oldColl != null)
                        oldColl.CollectionChanged -= ctrl.BookmarksCollection_CollectionChanged;

                    BookmarkCollection? newColl = args.NewValue as BookmarkCollection;
                    if (newColl != null)
                        newColl.CollectionChanged += ctrl.BookmarksCollection_CollectionChanged;
                })));

        #endregion

        #region Construction

        public HexEditorInternal()
        {
            this.DefaultStyleKey = typeof(HexEditorInternal);

            _textMetrics = new TextMetrics(HexEditorInternal.Typeface, this.FontSize, this.FlowDirection);

            SetupCommandBindings();
        }

        #endregion

        #region Fields

        private static readonly Typeface Typeface = new Typeface(new FontFamily("Cascadia Mono"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal,
                                                                 fallbackFontFamily: new FontFamily("Global Monospace"));

        private const double Padding = 5;

        private const double MinZoom = 0.5;
        private const double MaxZoom = 5;
        private const double ZoomStepAmount = 0.1d;


        private ScrollViewer? _scrollOwner;


        private TextMetrics _textMetrics;

        private CaretAdorner? _caretAdorner;

        private byte[]? _screenBuffer;
        private readonly StringBuilder _sbDigits = new StringBuilder();
        private readonly StringBuilder _sbDigitsModified = new StringBuilder();
        private readonly StringBuilder _sbAscii = new StringBuilder();
        private readonly StringBuilder _sbAsciiModified = new StringBuilder();

        private ZoomNotifyAdorner? _zoomNotifyAdorner;
        private DispatcherTimer? _zoomNotifyTimer;

        private ProviderRange? _inProgressSelection;

        private int _verticalScrollOffset;
        private int? _calculatedBytesPerLine;

        private long _caretByteIndex;
        private bool _caretLowNibble;

        private int _nextHighlightBrushIndex = 0;

        private CaretLocationKind _caretLocationKind;

        private static readonly BrushConverter BrushConverter = new BrushConverter();

        #endregion

        #region Properties

        public IProvider Provider
        {
            get => (IProvider)GetValue(ProviderProperty);
            set => SetValue(ProviderProperty, value);
        }

        public ObservableCollection<Brush> HighlightBrushes
        {
            get => (ObservableCollection<Brush>)GetValue(HighlightBrushesProperty);
            set => SetValue(HighlightBrushesProperty, value);
        }

        public HighlightRangeCollection HighlightRanges
        {
            get => (HighlightRangeCollection)GetValue(HighlightRangesProperty);
            set => SetValue(HighlightRangesProperty, value);
        }

        public int FontSize
        {
            get => (int)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public ProviderRange? SelectedRange
        {
            get => (ProviderRange?)GetValue(SelectedRangeProperty);
            set => SetValue(SelectedRangeProperty, value);
        }

        /// <summary>
        /// Total number of lines required to display all data
        /// </summary>
        public long TotalLines => (this.Provider != null) ? CalculateTotalLines(this.Provider.Length, this.BytesPerLine) : 0;

        /// <summary>
        /// Number of lines that can be displayed at once in the control
        /// </summary>
        public int DisplayLines => CalculateDisplayLines(this.ActualHeight, _textMetrics.LineHeight);

        /// <summary>
        /// Maximum value that the <see cref="VerticalScrollOffset"/> property can be
        /// </summary>
        public long MaxVerticalOffset => (this.DisplayLines > this.TotalLines) ? 0 : this.TotalLines - this.DisplayLines;

        /// <summary>
        /// Gets the current scroll vertical offset
        /// </summary>
        public int VerticalScrollOffset => _verticalScrollOffset;

        /// <summary>
        /// Gets the number of bytes that are displayed on a single line
        /// </summary>
        public int BytesPerLine => CalculateMaximumBytesPerLine();

        /// <summary>
        /// X location of the address for a displayed line
        /// </summary>
        public double AddressX => Padding;

        /// <summary>
        /// The width of the location of the address for a displayed line
        /// </summary>
        private double AddressWidth => (_textMetrics.SingleDigitWidth * this.AddressDigits);

        /// <summary>
        /// Gets the number of hex digits to be displayed for the address
        /// </summary>
        public int AddressDigits
        {
            get
            {
                if ((this.Provider?.Length ?? 1) - 1 > 0xFFFFFFFF)
                    return 16;

                return 8;
            }
        }

        /// <summary>
        /// X location for the hex bytes for a displayed line
        /// </summary>
        public double HexBytesX => this.AddressX + this.AddressWidth + _textMetrics.SpaceWidth;

        /// <summary>
        /// Width of the hex bytes for a displayed line
        /// </summary>
        public double HexBytesWidth => ((_textMetrics.HexDigitPairWidth + _textMetrics.SpaceWidth) * this.BytesPerLine) - _textMetrics.SpaceWidth;

        /// <summary>
        /// X location for the ascii bytes for a displayed line
        /// </summary>
        public double AsciiBytesX => this.HexBytesX + this.HexBytesWidth + _textMetrics.SpaceWidth;

        /// <summary>
        /// Width for the ASCII bytes to be displayed
        /// </summary>
        public double AsciiWidth => _textMetrics.AsciiCharWidth * this.BytesPerLine;

        /// <summary>
        /// Gets the insert/overwrite caret mode
        /// </summary>
        public CaretMode CaretMode
        {
            get => (CaretMode)GetValue(CaretModeProperty);
            set => SetValue(CaretModeProperty, value);
        }

        /// <summary>
        /// Gets/sets the zoom level
        /// </summary>
        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        /// <summary>
        /// Determines if the caret location is within the hex digit area or the ascii ares
        /// </summary>
        public CaretLocationKind CaretLocationKind
        {
            get => _caretLocationKind;
            set
            {
                if (_caretLocationKind != value)
                {
                    _caretLocationKind = value;
                    OnCaretPositionChanged();
                }
            }
        }

        /// <summary>
        /// The index into the provider of the byte that the caret is on
        /// </summary>
        public long CaretByteIndex
        {
            get => _caretByteIndex;
            private set
            {
                if (_caretByteIndex != value)
                {
                    _caretByteIndex = value;
                    OnPropertyChanged(nameof(CaretByteIndex));
                }
            }
        }

        /// <summary>
        /// Determines if the cursor is on the high or low nibble of the current byte
        /// </summary>
        public bool CaretLowNibble => _caretLowNibble;

        /// <summary>
        /// X location of the caret
        /// </summary>
        public double CaretX => this.CaretLocationKind == CaretLocationKind.HexArea ?
                                    this.HexBytesX + (_textMetrics.HexDigitPairWidthIncludingSpace * (this.CaretByteIndex % this.BytesPerLine)) + (_caretLowNibble ? _textMetrics.SingleDigitWidth : 0) :
                                    this.AsciiBytesX + (_textMetrics.AsciiCharWidth * (this.CaretByteIndex % this.BytesPerLine));

        /// <summary>
        /// Y location of the caret
        /// </summary>
        public double CaretY => _textMetrics.LineHeight * ((this.CaretByteIndex / this.BytesPerLine) - _verticalScrollOffset);

        /// <summary>
        /// Width of the caret image
        /// </summary>
        public double CaretWidth => this.CaretMode == CaretMode.Insert ? 1 : _textMetrics.SingleDigitWidth;

        /// <summary>
        /// Height of the caret image
        /// </summary>
        public double CaretHeight => _textMetrics.LineHeight;

        public BookmarkCollection Bookmarks
        {
            get => (BookmarkCollection)GetValue(BookmarksProperty);
            set => SetValue(BookmarksProperty, value);
        }

        public bool IsModified => this.Provider.IsModified;

        #endregion

        #region Dependency property change notifications

        private static void OnProviderChanged(DependencyObject dObj, DependencyPropertyChangedEventArgs args)
        {
            HexEditorInternal ctrl = (HexEditorInternal)dObj;
            ctrl.InvalidateMeasure();

            ctrl.MoveCaretToByteIndex(0);
            ctrl.SetVerticalScroll(0);

            IProvider? oldProvider = args.OldValue as IProvider;
            IProvider? newProvider = args.NewValue as IProvider;

            if (oldProvider != null)
            {
                oldProvider.ByteInserted -= ctrl.Provider_ByteInserted;
                oldProvider.ByteDeleted -= ctrl.Provider_ByteDeleted;
            }

            if (newProvider != null)
            {
                newProvider.ByteInserted += ctrl.Provider_ByteInserted;
                newProvider.ByteDeleted += ctrl.Provider_ByteDeleted;
            }

            ctrl.OnPropertyChanged(nameof(IsModified));
        }

        #endregion

        #region Methods

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);

            _caretAdorner ??= new CaretAdorner(this);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(100, 100);  // Should be a mixture of MinHeight and Height?
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return finalSize;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            _calculatedBytesPerLine = null;

            InvalidateVisual(); // If I dont do this then the control is not re-rendered if the user maximizes or minimizes the window

            base.OnRenderSizeChanged(sizeInfo);
        }

        private void OnSelectedRange(ProviderRange range)
        {
            this.SelectedRange = range;

            InvalidateVisual();
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs args)
        {
            base.OnGotKeyboardFocus(args);
            InvalidateVisual();

            if (_caretAdorner is not null)
            {
                UpdateCaretLocationAndSize();

                AdornerLayer layer = AdornerLayer.GetAdornerLayer(this);
                if (layer != null &&
                    (layer.GetAdorners(this)?.Contains(_caretAdorner) ?? false) == false)
                {
                    layer.Add(_caretAdorner);
                }

                _caretAdorner.ResetBlink();
            }
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs args)
        {
            base.OnLostKeyboardFocus(args);
            InvalidateVisual();

            if (_caretAdorner is not null)
            {
                UpdateCaretLocationAndSize();

                RemoveCaretAdorner();
            }
        }

        private void RemoveCaretAdorner()
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(this);
            layer?.Remove(_caretAdorner);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs args)
        {
            base.OnPreviewMouseDown(args);
            
            Focus();

            if (args.ChangedButton == MouseButton.Left)
            {
                HitTestResult hr = HitTest(args.GetPosition(this));
                if (hr.HexColumn != -1 || hr.AsciiColumn != -1)
                {
                    long startAndEndIndex = hr.GetByteIndex(this.VerticalScrollOffset, this.BytesPerLine);
                    if (startAndEndIndex < this.Provider.Length)
                    {
                        _inProgressSelection = new ProviderRange(startAndEndIndex, startAndEndIndex);
                        this.CaptureMouse();
                    }

                    this.SelectedRange = null;
                    InvalidateVisual();
                }
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs args)
        {
            base.OnPreviewMouseMove(args);

            if (Mouse.Captured == this)
            {
                HitTestResult hr = HitTest(args.GetPosition(this));
                if (hr.HexColumn != -1 || hr.AsciiColumn != -1)
                {
                    long endIndex = hr.GetByteIndex(this.VerticalScrollOffset, this.BytesPerLine);
                    if (endIndex >= this.Provider.Length)
                        endIndex = this.Provider.Length - 1;

                    if (endIndex < 0)
                        endIndex = _inProgressSelection!.StartIndex;

                    _inProgressSelection = new ProviderRange(_inProgressSelection!.StartIndex, endIndex);

                    InvalidateVisual();

                    Point mouseLocation = args.GetPosition(this);
                    if (mouseLocation.Y < 0)
                        AdjustVerticalScroll(-1);
                    else if (mouseLocation.Y > this.ActualHeight)
                        AdjustVerticalScroll(1);
                }
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs args)
        {
            base.OnPreviewMouseUp(args);

            if (Mouse.Captured == this)
            {
                this.ReleaseMouseCapture();
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs args)
        {
            base.OnLostMouseCapture(args);

            if (_inProgressSelection != null)
            {
                MoveCaretToByteIndex(_inProgressSelection.EndIndex);

                if (_inProgressSelection.Count > 1)
                    OnSelectedRange(_inProgressSelection.Normalize());
            }

            _inProgressSelection = null;

            InvalidateVisual();
        }

        private bool DeleteSelection()
        {
            if (this.SelectedRange == null)
                return false;

            for (int i = 0; i < this.SelectedRange.Count; i++)
                this.Provider.Delete(this.SelectedRange.StartIndex);

            MoveCaretToByteIndex(this.SelectedRange.StartIndex);
            this.SelectedRange = null;

            return true;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs args)
        {
            if (this.Provider is null)
            {
                base.OnPreviewKeyDown(args);
                return;
            }

            if (args.Key == Key.Left || args.Key == Key.Right || args.Key == Key.Up || args.Key == Key.Down ||
                args.Key == Key.Home || args.Key == Key.End ||
                args.Key == Key.Back || args.Key == Key.Delete || args.Key == Key.Insert)
            {
                switch (args.Key)
                {
                    case Key.Left:
                        MoveCaretLeft();
                        break;

                    case Key.Right:
                        MoveCaretRight();
                        break;

                    case Key.Up:
                        MoveCaretUp();
                        break;

                    case Key.Down:
                        MoveCaretDown();
                        break;

                    case Key.Home:
                        {
                            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                                MoveCaretToByteIndex(0);
                            else
                                MoveCaretToStartOfCurrentLine();
                        }
                        break;

                    case Key.End:
                        {
                            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                                MoveCaretToByteIndex(this.Provider.Length);
                            else
                                MoveCaretToEndOfCurrentLine();
                        }
                        break;

                    case Key.Back:
                        {
                            if (DeleteSelection() == false)
                            {
                                if (CaretByteIndex > 0)
                                {
                                    MoveCaretLeft();
                                    DeleteCurrentByte();
                                }
                            }
                        }
                        break;

                    case Key.Delete:
                        {
                            if (DeleteSelection() == false)
                                DeleteCurrentByte();
                        }
                        break;

                    case Key.Insert:
                        this.CaretMode = this.CaretMode == CaretMode.Overwrite ? CaretMode.Insert : CaretMode.Overwrite;
                        break;
                }

                // When moving the caret around, we dont want it blinking
                if (_caretAdorner is not null)
                    _caretAdorner.ResetBlink();

                InvalidateVisual();

                args.Handled = true;
                return;
            }

            base.OnPreviewKeyDown(args);
        }

        protected override void OnTextInput(TextCompositionEventArgs args)
        {
            if (args.Text.Length == 0)
            {
                base.OnTextInput(args);
                return;
            }

            char ch = args.Text[0];
            if (this.CaretLocationKind == CaretLocationKind.HexArea)
            {
                byte? byteVal = null;

                switch (Char.ToUpperInvariant(ch))
                {
                    case '0': byteVal = 0; break;
                    case '1': byteVal = 1; break;
                    case '2': byteVal = 2; break;
                    case '3': byteVal = 3; break;
                    case '4': byteVal = 4; break;
                    case '5': byteVal = 5; break;
                    case '6': byteVal = 6; break;
                    case '7': byteVal = 7; break;
                    case '8': byteVal = 8; break;
                    case '9': byteVal = 9; break;
                    case 'A': byteVal = 0xa; break;
                    case 'B': byteVal = 0xb; break;
                    case 'C': byteVal = 0xc; break;
                    case 'D': byteVal = 0xd; break;
                    case 'E': byteVal = 0xe; break;
                    case 'F': byteVal = 0xf; break;
                }

                if (byteVal != null)
                {
                    bool insert = (this.CaretMode == CaretMode.Insert && !this.CaretLowNibble);

                    byte[] current = new byte[1];
                    if (!insert && this.Provider.CopyTo(this.CaretByteIndex, current, 0, 1) == 0)
                        current = new byte[] { 0x00 };

                    bool wasCaretLowNibble = this.CaretLowNibble;
                    if (wasCaretLowNibble)
                    {
                        current[0] &= 0xf0;
                        current[0] |= byteVal.Value;
                    }
                    else
                    {
                        current[0] &= 0x0f;
                        current[0] |= (byte)(byteVal.Value << 4);
                    }

                    bool selectionDeleted = DeleteSelection();
                    if(selectionDeleted || insert)
                        InsertAtCurrentByte(current[0]);
                    else
                        ChangeCurrentByte(current[0]);

                    args.Handled = true;

                    if (!wasCaretLowNibble)
                        MoveCaretToLowNibble();
                    else
                        MoveCaretRight();

                    _caretAdorner?.ResetBlink();
                    return;
                }
            }
            else if (this.CaretLocationKind == CaretLocationKind.AsciiArea)
            {
                if (Char.IsAscii(ch) && !Char.IsControl(ch))
                {
                    bool insert = (this.CaretMode == CaretMode.Insert);

                    bool selectionDeleted = DeleteSelection();
                    if (selectionDeleted || insert)
                        InsertAtCurrentByte((byte)ch);
                    else
                        ChangeCurrentByte((byte)ch);

                    args.Handled = true;

                    MoveCaretRight();

                    _caretAdorner?.ResetBlink();
                    return;
                }
            }

            base.OnTextInput(args);
        }

        private void AdjustZoom(double amount)
        {
            double scale = (this.LayoutTransform as ScaleTransform)?.ScaleX ?? 1;
            scale += amount;
            scale = Math.Round(scale, 1);

            SetZoom(scale);
        }

        private static bool IsZoomValueValid(double zoom) =>
            (zoom >= MinZoom && zoom <= MaxZoom);

        private void SetZoom(double scale)
        {
            if (IsZoomValueValid(scale))
            {
                this.LayoutTransform = new ScaleTransform(scale, scale);

                _zoomNotifyTimer?.Stop();

                AdornerLayer layer = AdornerLayer.GetAdornerLayer(this);

                if (_zoomNotifyAdorner is not null)
                    layer.Remove(_zoomNotifyAdorner);

                _zoomNotifyAdorner = new ZoomNotifyAdorner(this, scale);
                _zoomNotifyAdorner.Opacity = 1;
                layer.Add(_zoomNotifyAdorner);

                if (_zoomNotifyTimer == null)
                {
                    _zoomNotifyTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal,
                        (s, a) =>
                        {
                            AdornerLayer layer = AdornerLayer.GetAdornerLayer(this);
                            if (layer is not null && _zoomNotifyAdorner is not null)
                            {
                                DoubleAnimation anim = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(400)));
                                anim.EasingFunction = new QuinticEase() { EasingMode = EasingMode.EaseOut };
                                anim.Completed += (s, e) =>
                                {
                                    if (_zoomNotifyAdorner is not null)
                                    {
                                        layer.Remove(_zoomNotifyAdorner);
                                        _zoomNotifyAdorner = null;
                                    }
                                };

                                _zoomNotifyAdorner.BeginAnimation(Adorner.OpacityProperty, anim);
                            }

                        }, this.Dispatcher);
                }
                else
                {
                    _zoomNotifyTimer.Start();
                }

                InvalidateVisual();

                // Ensure the Zoom property has the current zoom level
                this.Zoom = scale;
            }
        }

        private void UpdateCaretLocationAndSize()
        {
            if (_caretAdorner is null)
                return;

            _caretAdorner.Location = new Point(this.CaretX, this.CaretY);
            _caretAdorner.Size = new Size(this.CaretWidth, this.CaretHeight);

            InvalidateVisual(); // To draw highlight lines around the cursor position
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs args)
        {
            base.OnPreviewMouseLeftButtonDown(args);

            HitTestResult result = HitTest(args.GetPosition(this));
            if (result.HexColumn != -1)
            {
                this.CaretLocationKind = CaretLocationKind.HexArea;
                MoveCaretToByteIndex(result.HexColumn + (((long)(this.VerticalScrollOffset + result.HoverLine)) * this.BytesPerLine));
            }
            else if (result.AsciiColumn != -1)
            {
                this.CaretLocationKind = CaretLocationKind.AsciiArea;
                MoveCaretToByteIndex(result.AsciiColumn + (((long)(this.VerticalScrollOffset + result.HoverLine)) * this.BytesPerLine));
            }
        }

        private HitTestResult HitTest(Point pt)
        {
            double halfSpaceWidth = _textMetrics.SpaceWidth / 2;

            int hexColumn = -1;
            int asciiColumn = -1;
            int hoverLine;

            // Determine which column in hex area
            if (pt.X > this.HexBytesX - halfSpaceWidth && pt.X < this.HexBytesX + this.HexBytesWidth + halfSpaceWidth)
            {
                double x = this.HexBytesX;
                for (int i = 0; i < this.BytesPerLine; i++)
                {
                    if (pt.X >= x - halfSpaceWidth && pt.X < x + _textMetrics.HexDigitPairWidth + halfSpaceWidth)
                    {
                        hexColumn = i;
                        break;
                    }

                    x += _textMetrics.HexDigitPairWidth + _textMetrics.SpaceWidth;
                }
            }
            else
            {
                // Determine which column in ascii area
                if (pt.X > this.AsciiBytesX && pt.X < this.AsciiBytesX + this.AsciiWidth)
                {
                    double x = this.AsciiBytesX;
                    for (int i = 0; i < this.BytesPerLine; i++)
                    {
                        if (pt.X >= x && pt.X < x + _textMetrics.AsciiCharWidth)
                        {
                            asciiColumn = i;
                            break;
                        }

                        x += _textMetrics.AsciiCharWidth;
                    }
                }
            }

            // Determine which line
            hoverLine = (int)(pt.Y / _textMetrics.LineHeight);

            return new HitTestResult(hexColumn, asciiColumn, hoverLine);
        }

        private static long CalculateTotalLines(long totalBytes, int bytesPerLine)
        {
            long res = totalBytes / bytesPerLine;

            // Adding one to account for partial lines, and if there are no partial lines
            // then we still need one more in case the user wants to add bytes at the end
            res++;

            return res;
        }

        private static int CalculateDisplayLines(double controlHeight, double lineHeight) =>
            (int)(controlHeight / lineHeight);  // No partial lines

        /// <summary>
        /// Calculates the maximum number of bytes that can appear on a line
        /// </summary>
        /// <returns></returns>
        private int CalculateMaximumBytesPerLine()
        {
            if (_calculatedBytesPerLine is null)
            {
                double availableWidth = this.ActualWidth - this.AddressX - this.AddressWidth - (Padding * 2);
                double singleHexDigitWidth = _textMetrics.HexDigitPairWidthIncludingSpace;
                double singleASCIIWidth = _textMetrics.AsciiCharWidth;

                int count = 0;
                double curWidth = 0;
                while (curWidth + _textMetrics.SpaceWidth < availableWidth)   // the gap between the hex digits and ascii area
                {
                    curWidth += singleHexDigitWidth + singleASCIIWidth;
                    count++;
                }

                _calculatedBytesPerLine = Math.Max(1, count - 1);
            }

            return _calculatedBytesPerLine.Value;
        }

        /// <summary>
        /// Moves the caret one byte to the left
        /// </summary>
        public void MoveCaretLeft() =>
            MoveCaretToByteIndex(this.CaretByteIndex - 1);

        /// <summary>
        /// Moves the caret one byte to the right
        /// </summary>
        public void MoveCaretRight() =>
            MoveCaretToByteIndex(this.CaretByteIndex + 1);

        /// <summary>
        /// Moves the caret to the byte immediately above the current byte
        /// </summary>
        public void MoveCaretUp() =>
            MoveCaretToByteIndex(this.CaretByteIndex - this.BytesPerLine);

        /// <summary>
        /// Moves the caret to the byte immediately below the current byte
        /// </summary>
        public void MoveCaretDown() =>
            MoveCaretToByteIndex(this.CaretByteIndex + this.BytesPerLine);

        /// <summary>
        /// Moves the caret to the first byte on the current line
        /// </summary>
        public void MoveCaretToStartOfCurrentLine() =>
            MoveCaretToByteIndex(this.CaretByteIndex - (this.CaretByteIndex % this.BytesPerLine));

        /// <summary>
        /// Moves the caret to the first byte on the current line
        /// </summary>
        public void MoveCaretToEndOfCurrentLine() =>
            MoveCaretToByteIndex(this.CaretByteIndex + (this.BytesPerLine - (this.CaretByteIndex % this.BytesPerLine)) - 1);

        /// <summary>
        /// Moves the caret to the specified byte
        /// </summary>
        public void MoveCaretToByteIndex(long index)
        {
            if (index < 0)
                return;

            if (index > this.Provider.Length)
                index = this.Provider.Length;

            this.CaretByteIndex = index;
            _caretLowNibble = false;
            EnsureByteIndexIsVisible(this.CaretByteIndex);

            OnCaretPositionChanged();
        }

        public void MoveCaretToLowNibble()
        {
            if (this.CaretByteIndex == (int)this.Provider.Length)
                return;

            EnsureByteIndexIsVisible(this.CaretByteIndex);
            _caretLowNibble = true;

            OnCaretPositionChanged();
        }

        /// <summary>
        /// Ensures that hte specified byte index is visible, scrolling if necessary
        /// </summary>
        public void EnsureByteIndexIsVisible(long index)
        {
            long line = index / this.BytesPerLine;

            // If its already visibile
            if (line >= _verticalScrollOffset && line < _verticalScrollOffset + this.DisplayLines)
                return;

            if (line > Int32.MaxValue)
                line = (this.Provider.Length / this.BytesPerLine) - this.DisplayLines;

            int lineAsInt32 = (int)line;

            // If we need to scroll down
            if (line >= _verticalScrollOffset + this.DisplayLines)
                AdjustVerticalScroll((lineAsInt32 - (_verticalScrollOffset + this.DisplayLines)) + 1);

            // If we need to scroll up
            else if (line < _verticalScrollOffset + this.DisplayLines)
                AdjustVerticalScroll(lineAsInt32 - _verticalScrollOffset);
        }

        /// <summary>
        /// Vertically scrolls up or down by the specified amount
        /// </summary>
        /// <param name="adjust"></param>
        public void AdjustVerticalScroll(int adjust) =>
            SetVerticalScroll(_verticalScrollOffset + adjust);

        /// <summary>
        /// Sets the vertical scroll offset
        /// </summary>
        /// <param name="value"></param>
        public void SetVerticalScroll(int value)
        {
            if (value < 0)
                value = 0;

            if (value > this.MaxVerticalOffset)
                value = (int)Math.Min(Int32.MaxValue, this.MaxVerticalOffset);

            _verticalScrollOffset = value;
            OnScrollInfoChanged();
            OnCaretPositionChanged();
            OnVisualChanged();
        }

        private void OnCaretPositionChanged() =>
            UpdateCaretLocationAndSize();

        private void OnScrollInfoChanged() =>
            _scrollOwner?.InvalidateScrollInfo();

        private void OnVisualChanged() =>
            this.InvalidateVisual();

        public void ChangeCurrentByte(byte value)
        {
            if (this.CaretByteIndex < this.Provider.Length)
            {
                this.Provider.Modify(this.CaretByteIndex, value);
                OnScrollInfoChanged();
                OnVisualChanged();
            }
            else
            {
                InsertAtCurrentByte(value);
            }

            OnPropertyChanged(nameof(IsModified));
        }

        public void InsertAtCurrentByte(byte value)
        {
            this.Provider.InsertByte(this.CaretByteIndex, value);
            OnScrollInfoChanged();
            OnVisualChanged();

            OnPropertyChanged(nameof(IsModified));
        }

        public void DeleteCurrentByte()
        {
            if (this.CaretByteIndex >= this.Provider.Length)
                return;

            this.Provider.Delete(this.CaretByteIndex);
            OnScrollInfoChanged();
            OnVisualChanged();

            OnPropertyChanged(nameof(IsModified));
        }

        private FormattedText CreateFormattedText(string text, Brush brush) =>
            new FormattedText(text, CultureInfo.CurrentUICulture, this.FlowDirection,
                                HexEditorInternal.Typeface, _textMetrics.EMFontSize, brush, 96);

        private char GetDisplayAsciiChar(byte value) =>
            Char.IsAscii((char)value) && !Char.IsControl((char)value) ? (char)value : '.';

        public async Task SaveChangesAsync(CancellationToken cancellation, IProgress<ApplyChangesProgressInfo>? progress)
        {
            await this.Provider.ApplyChangesAsync(cancellation, progress);
            OnPropertyChanged(nameof(IsModified));
            InvalidateVisual();
        }

        public void ToggleBookmark(long byteIndex, string? name = null)
        {
            if (byteIndex < 0 || byteIndex >= this.Provider.Length)
                return;

            if (this.Bookmarks.Any(o => o.ByteIndex == byteIndex))
            {
                foreach (Bookmark bookmark in this.Bookmarks.Where(o => o.ByteIndex == byteIndex).ToArray()) // There should only be one though?
                {
                    this.Bookmarks.Remove(bookmark);
                }
            }
            else
            {
                this.Bookmarks.Add(new Bookmark(byteIndex, name));
            }
        }

        public bool MoveToPreviousBookmark()
        {
            long? offset = this.Bookmarks.Select(o => (long?)o.ByteIndex).OrderByDescending(o => o).FirstOrDefault(o => o < this.CaretByteIndex);
            if (offset is null)
                offset = this.Bookmarks.Select(o => (long?)o.ByteIndex).LastOrDefault();

            if (offset is not null)
            {
                MoveCaretToByteIndex(offset.Value);
                return true;
            }

            return false;
        }

        public bool MoveToNextBookmark()
        {
            long? offset = this.Bookmarks.Select(o => (long?)o.ByteIndex).OrderBy(o => o).FirstOrDefault(o => o > this.CaretByteIndex);
            if (offset is null)
                offset = this.Bookmarks.Select(o => (long?)o.ByteIndex).FirstOrDefault();

            if (offset is not null)
            {
                MoveCaretToByteIndex(offset.Value);
                return true;
            }
            
            return false;
        }

        private void SetupCommandBindings()
        {
            this.CommandBindings.Add(new CommandBinding(HexEditorCommands.Zoom50Percent, (s, a) => SetZoom(0.5)));
            this.CommandBindings.Add(new CommandBinding(HexEditorCommands.Zoom100Percent, (s, a) => SetZoom(1)));
            this.CommandBindings.Add(new CommandBinding(HexEditorCommands.Zoom150Percent, (s, a) => SetZoom(1.5)));
            this.CommandBindings.Add(new CommandBinding(HexEditorCommands.Zoom200Percent, (s, a) => SetZoom(2)));

            this.CommandBindings.Add(new CommandBinding(HexEditorCommands.ToggleBookmark,
                (s, a) => ToggleBookmark(this.CaretByteIndex),
                (s, a) => a.CanExecute = (this.Provider is not null && this.CaretByteIndex >= 0 && this.CaretByteIndex < this.Provider.Length)));

            this.CommandBindings.Add(new CommandBinding(HexEditorCommands.PreviousBookmark,
                (s, a) => MoveToPreviousBookmark(),
                (s, a) => a.CanExecute = this.Bookmarks?.Any() ?? false));

            this.CommandBindings.Add(new CommandBinding(HexEditorCommands.NextBookmark,
                (s, a) => MoveToNextBookmark(),
                (s, a) => a.CanExecute = this.Bookmarks?.Any() ?? false));

            this.CommandBindings.Add(new CommandBinding(HexEditorCommands.RemoveAllHighlights,
                (s, a) => HighlightRanges?.Clear(),
                (s, a) => a.CanExecute = HighlightRanges?.Any() ?? false));

            this.CommandBindings.Add(new CommandBinding(HexEditorCommands.RemoveHighlight,
                (s, a) => 
                {
                    HighlightRange? range = this.HighlightRanges?.GetFirstThatContainsIndex(this.CaretByteIndex);
                    if (range is not null)
                        this.HighlightRanges!.Remove(range);
                },
                (s, a) => a.CanExecute = (this.HighlightRanges is not null && 
                                          this.HighlightRanges.GetFirstThatContainsIndex(this.CaretByteIndex) is not null)));

            this.CommandBindings.Add(new CommandBinding(HexEditorCommands.HighlightSelection,
                (s, a) =>
                {
                    Brush? brush = null;
                    try
                    {
                        brush = HexEditorInternal.BrushConverter.ConvertFrom(a.Parameter) as Brush;
                    }
                    catch { }

                    if (brush is null)
                        brush = GetNextHighlightBrush();

                    this.HighlightRanges?.Add(new HighlightRange(this.SelectedRange!, brush));
                    this.SelectedRange = null;
                },
                (s, a) => a.CanExecute = this.SelectedRange != null));
        }

        protected override void OnContextMenuOpening(ContextMenuEventArgs args)
        {
            base.OnContextMenuOpening(args);

            HitTestResult result = HitTest(new Point(args.CursorLeft, args.CursorTop));
            long caretByteIndex = -1;

            if (result.HexColumn != -1)
            {
                this.CaretLocationKind = CaretLocationKind.HexArea;
                caretByteIndex = result.HexColumn + (((long)(this.VerticalScrollOffset + result.HoverLine)) * this.BytesPerLine);
            }
            else if (result.AsciiColumn != -1)
            {
                this.CaretLocationKind = CaretLocationKind.AsciiArea;
                caretByteIndex = result.AsciiColumn + (((long)(this.VerticalScrollOffset + result.HoverLine)) * this.BytesPerLine);
            }

            if (caretByteIndex != -1)
            {
                MoveCaretToByteIndex(caretByteIndex);

                // Remove the current selection if the user didnt display the context menu for it
                if (this.SelectedRange != null && this.SelectedRange.IsInRange(caretByteIndex) == false)
                    this.SelectedRange = null;
            }
            else
                this.SelectedRange = null;
            

            // To prevent the control loosing the focus when the menu is displayed, we need to set Focusable
            // to false on the menu itself and all its children
            MakeContextMenuNotFocusable(this.ContextMenu);

            _caretAdorner?.StopBlink();
        }

        protected override void OnContextMenuClosing(ContextMenuEventArgs args)
        {
            base.OnContextMenuClosing(args);

            _caretAdorner?.ResetBlink();
        }

        private void MakeContextMenuNotFocusable(ContextMenu menu)
        {
            if (menu is null)
                return;

            void RemoveFocusable(UIElement element)
            {
                element.Focusable = false;

                if (element is ItemsControl ctrl)
                {
                    foreach (UIElement child in ctrl.Items)
                        RemoveFocusable(child);
                }
            }

            RemoveFocusable(menu);
        }

        public async Task ApplyChangesAsync(CancellationToken cancellation, IProgress<ApplyChangesProgressInfo>? progress)
        {
            if (this.Provider is not null)
            {
                await this.Provider.ApplyChangesAsync(cancellation, progress);
                InvalidateVisual();
            }
        }

        #endregion

        #region Render methods

        protected override void OnRender(DrawingContext ctx)
        {
            if (this.Provider is null)
                return;

            Stopwatch sw = Stopwatch.StartNew();

            int bytesPerLine = this.BytesPerLine;
            int displayLines = this.DisplayLines;

            // Create a buffer to get the bytes to display from the provider
            int screenBufferSize = bytesPerLine * (displayLines + 1);  // + 1 incase there is a partial line
            if (_screenBuffer is null || _screenBuffer.Length < screenBufferSize)
                _screenBuffer = new byte[screenBufferSize];

            // Determine the offset in the provider to start reading bytes and read them
            long byteIndexAtTopLeftScreen = (long)bytesPerLine * this.VerticalScrollOffset;
            ProviderModifications modifications = new ProviderModifications();
            int read = this.Provider.CopyTo(byteIndexAtTopLeftScreen, _screenBuffer, 0, screenBufferSize, modifications);

            // Start rendering
            RenderBackground(ctx);
            RenderHighlights(ctx, bytesPerLine);
            RenderRowColumnCaretLineGuides(ctx);
            RenderSelection(ctx, bytesPerLine);

            RenderInfo info = new RenderInfo()
            {
                Buffer = new ArraySegment<byte>(_screenBuffer, 0, read),
                Modifications = modifications,

                BytesPerLine = bytesPerLine,
                FirstDisplayedLine = this.VerticalScrollOffset,
                DisplayedLineCount = displayLines,

                LineHeight = _textMetrics.LineHeight,
            };

            RenderBookmarks(ctx, info);
            RenderBytes(ctx, info);
            
            RenderBorder(ctx);

            //Debug.WriteLine($"Render time: {sw.ElapsedMilliseconds}");
        }

        private void RenderBackground(DrawingContext ctx) =>
            ctx.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight));

        private void RenderHighlights(DrawingContext ctx, int bytesPerLine)
        {
            if ((this.HighlightBrushes?.Count ?? 0) == 0)
                return;

            for (int i = 0; i < this.HighlightRanges.Count; i++)
            {
                HighlightRange highlight = this.HighlightRanges[i];
                if (highlight == null)
                    continue;

                RenderRange(ctx, highlight, highlight.Brush, bytesPerLine);
            }
        }

        private Brush GetNextHighlightBrush()
        {
            Brush result = this.HighlightBrushes[_nextHighlightBrushIndex % this.HighlightBrushes.Count];
            _nextHighlightBrushIndex++;

            return result;
        }

        private void RenderRowColumnCaretLineGuides(DrawingContext ctx)
        {
            int column = (int)(this.CaretByteIndex % this.BytesPerLine);
            int line = (int)(this.CaretByteIndex / this.BytesPerLine) - this.VerticalScrollOffset;

            // Calculate the rect of the hex byte
            Rect rectHex = new Rect(
                x: this.HexBytesX - _textMetrics.HalfSpaceWidth + (column * _textMetrics.HexDigitPairWidthIncludingSpace),
                y: line * _textMetrics.LineHeight,
                width: _textMetrics.HexDigitPairWidth + _textMetrics.SpaceWidth,
                height: _textMetrics.LineHeight);

            // Calculate the rect of the ascii byte - the y/height is the same as the hex byte rect
            Rect rectAscii = new Rect(
                x: this.AsciiBytesX + (column * _textMetrics.AsciiCharWidth),
                y: line * _textMetrics.LineHeight,
                width: _textMetrics.AsciiCharWidth,
                height: _textMetrics.LineHeight);

            PathFigure figure = new PathFigure();

            // Top half
            figure.StartPoint = new Point(1, rectHex.Top);
            figure.Segments.Add(new LineSegment(new Point(rectHex.Left, rectHex.Top), true));
            figure.Segments.Add(new LineSegment(new Point(rectHex.Left, 1), true));

            figure.Segments.Add(new LineSegment(new Point(rectHex.Right, 1), false));
            figure.Segments.Add(new LineSegment(new Point(rectHex.Right, rectHex.Top), true));
            figure.Segments.Add(new LineSegment(new Point(rectAscii.Left, rectHex.Top), true));
            figure.Segments.Add(new LineSegment(new Point(rectAscii.Left, 1), true));

            figure.Segments.Add(new LineSegment(new Point(rectAscii.Right, 1), false));
            figure.Segments.Add(new LineSegment(new Point(rectAscii.Right, rectHex.Top), true));
            figure.Segments.Add(new LineSegment(new Point(this.ActualWidth - 1, rectHex.Top), true));

            // Bottom half
            figure.Segments.Add(new LineSegment(new Point(1, rectHex.Bottom), false));
            figure.Segments.Add(new LineSegment(new Point(rectHex.Left, rectHex.Bottom), true));
            figure.Segments.Add(new LineSegment(new Point(rectHex.Left, this.ActualHeight - 1), true));

            figure.Segments.Add(new LineSegment(new Point(rectHex.Right, this.ActualHeight - 1), false));
            figure.Segments.Add(new LineSegment(new Point(rectHex.Right, rectHex.Bottom), true));
            figure.Segments.Add(new LineSegment(new Point(rectAscii.Left, rectHex.Bottom), true));
            figure.Segments.Add(new LineSegment(new Point(rectAscii.Left, this.ActualHeight - 1), true));

            figure.Segments.Add(new LineSegment(new Point(rectAscii.Right, this.ActualHeight - 1), false));
            figure.Segments.Add(new LineSegment(new Point(rectAscii.Right, rectHex.Bottom), true));
            figure.Segments.Add(new LineSegment(new Point(this.ActualWidth - 1, rectHex.Bottom), true));

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            if (geometry.CanFreeze)
                geometry.Freeze();

            ctx.DrawGeometry(null, HexEditorBrushes.CaretRowColumnPen, geometry);
        }

        private void RenderBytes(DrawingContext ctx, RenderInfo info)
        {
            // Draw the address for the first line
            ulong lineOffset = Math.Min(UInt64.MaxValue, (ulong)info.GetLineProviderOffset(0));
            string address = lineOffset.ToString($"x{this.AddressDigits}");
            RenderText(ctx, this.AddressX, info.GetLineYPosition(0), $"{address} ");

            // Draw the lines
            for (int line = 0; line < info.DisplayedLineCount + 1; line++)    // + 1 incase there is a partially visible line
            {
                ArraySegment<byte> lineBuffer = info.GetLineBuffer(line);
                if (lineBuffer.Count == 0)
                    break;

                double lineYPos = info.GetLineYPosition(line);
                ProviderRange[] modificationRanges = info.GetLineModifications(line).ToArray();

                _sbDigits.Clear();
                _sbDigitsModified.Clear();
                _sbAscii.Clear();
                _sbAsciiModified.Clear();

                for (int i = 0; i < Math.Min(this.BytesPerLine, lineBuffer.Count); i++)
                {
                    if (modificationRanges.Any(o => o.IsInRange(i)))
                    {
                        _sbDigitsModified.Append($"{lineBuffer[i]:x2} ");
                        _sbDigits.Append("   ");
                        _sbAsciiModified.Append($"{GetDisplayAsciiChar(lineBuffer[i])}");
                        _sbAscii.Append(" ");
                    }
                    else
                    {
                        _sbDigits.Append($"{lineBuffer[i]:x2} ");
                        _sbDigitsModified.Append("   ");
                        _sbAscii.Append($"{GetDisplayAsciiChar(lineBuffer[i])}");
                        _sbAsciiModified.Append(" ");
                    }
                }

                RenderText(ctx, this.HexBytesX, lineYPos, _sbDigits.ToString());
                RenderText(ctx, this.AsciiBytesX, lineYPos, _sbAscii.ToString());

                RenderText(ctx, this.HexBytesX, lineYPos, _sbDigitsModified.ToString(), Brushes.Red);
                RenderText(ctx, this.AsciiBytesX, lineYPos, _sbAsciiModified.ToString(), Brushes.Red);

                // If the next line will be drawn
                if (line < info.DisplayedLineCount && lineBuffer.Count == info.BytesPerLine)
                {
                    // Draw the address for the next line
                    lineOffset = Math.Min(UInt64.MaxValue, (ulong)info.GetLineProviderOffset(line + 1));

                    double lastLineYPos = info.GetLineYPosition(line + 1);
                    address = lineOffset.ToString($"x{this.AddressDigits}");
                    RenderText(ctx, this.AddressX, lastLineYPos, $"{address} ");
                }
            }
        }

        private void RenderBookmarks(DrawingContext ctx, RenderInfo info)
        {
            // Determine the range of bytes that are in view
            long startingIndex = info.GetLineProviderOffset(0);
            long endingIndex = info.GetLineProviderOffset(info.DisplayedLineCount + 1);    // + 1 incase there is a partially visible line

            foreach (Bookmark bookmark in this.Bookmarks)
            {
                if (bookmark.ByteIndex < startingIndex || bookmark.ByteIndex > endingIndex)
                    continue;

                long byteIndex = bookmark.ByteIndex - ((long)info.FirstDisplayedLine * info.BytesPerLine);

                int line = (int)(byteIndex / info.BytesPerLine);
                int lineByteIndex = (int)(byteIndex % info.BytesPerLine);

                Rect rectHexByte = new Rect(
                    x: this.HexBytesX + (lineByteIndex * _textMetrics.HexDigitPairWidthIncludingSpace), 
                    y: line * info.LineHeight,
                    width: _textMetrics.HexDigitPairWidth,
                    height: info.LineHeight);

                rectHexByte.Offset(-_textMetrics.HalfSpaceWidth, 0);

                ////// 

                PathFigure figure = new PathFigure();
                figure.StartPoint = rectHexByte.TopRight;
                figure.Segments.Add(new LineSegment(rectHexByte.TopLeft, false));
                figure.Segments.Add(new LineSegment(rectHexByte.BottomLeft, false));
                PathGeometry geometry = new PathGeometry();
                geometry.Figures.Add(figure);

                ctx.DrawGeometry(HexEditorBrushes.BookmarkBrush, null, geometry);

                ////// 
                
                Rect rectAsciiByte = new Rect(
                    x: this.AsciiBytesX + (lineByteIndex * _textMetrics.AsciiCharWidth),
                    y: line * info.LineHeight,
                    width: _textMetrics.AsciiCharWidth,
                    height: info.LineHeight);

                figure = new PathFigure();
                figure.StartPoint = rectAsciiByte.TopRight;
                figure.Segments.Add(new LineSegment(rectAsciiByte.TopLeft, false));
                figure.Segments.Add(new LineSegment(rectAsciiByte.BottomLeft, false));
                geometry = new PathGeometry();
                geometry.Figures.Add(figure);

                ctx.DrawGeometry(HexEditorBrushes.BookmarkBrush, null, geometry);
            }
        }

        private void RenderSelection(DrawingContext ctx, int bytesPerLine)
        {
            if (_inProgressSelection != null)
                RenderRange(ctx, _inProgressSelection, HexEditorBrushes.SelectionBrush, bytesPerLine);

            if (this.SelectedRange != null)
                RenderRange(ctx, this.SelectedRange, HexEditorBrushes.SelectionBrush, bytesPerLine);
        }

        private void RenderBorder(DrawingContext ctx)
        {
            Pen borderPen = new Pen(this.IsKeyboardFocused ? HexEditorBrushes.ActiveBorderBrush : SystemColors.InactiveBorderBrush, 1);
            ctx.DrawRectangle(null, borderPen, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
        }

        private void RenderRange(DrawingContext ctx, ProviderRange range, Brush brush, int bytesPerLine)
        {
            range = range.Normalize();

            long selectionStartLine = range.StartIndex / bytesPerLine;
            long selectionEndLine = (range.StartIndex + range.Count) / bytesPerLine;

            for (long i = selectionStartLine; i <= selectionEndLine; i++)
            {
                Rect rectHex = new Rect(
                    this.HexBytesX - _textMetrics.HalfSpaceWidth,
                    (i - this.VerticalScrollOffset) * _textMetrics.LineHeight,
                    this.HexBytesWidth + _textMetrics.SpaceWidth,
                    _textMetrics.LineHeight);

                Rect rectAscii = new Rect(
                    this.AsciiBytesX,
                    (i - this.VerticalScrollOffset) * _textMetrics.LineHeight,
                    this.AsciiWidth,
                    _textMetrics.LineHeight);

                if (i == selectionStartLine)
                {
                    double adjustStartX = (range.StartIndex % bytesPerLine) * _textMetrics.HexDigitPairWidthIncludingSpace;
                    rectHex.Offset(adjustStartX, 0);
                    rectHex.Width -= adjustStartX;

                    adjustStartX = (range.StartIndex % bytesPerLine) * _textMetrics.AsciiCharWidth;
                    rectAscii.Offset(adjustStartX, 0);
                    rectAscii.Width -= adjustStartX;
                }

                if (i == selectionEndLine)
                {
                    double adjustWidth = (bytesPerLine - (range.StartIndex + range.Count) % bytesPerLine) * _textMetrics.HexDigitPairWidthIncludingSpace;
                    rectHex.Width -= adjustWidth;

                    adjustWidth = (bytesPerLine - (range.StartIndex + range.Count) % bytesPerLine) * _textMetrics.AsciiCharWidth;
                    rectAscii.Width -= adjustWidth;
                }

                ctx.DrawRectangle(brush, null, rectHex);
                ctx.DrawRectangle(brush, null, rectAscii);
            }
        }

        private void RenderText(DrawingContext ctx, double x, double y, string text, Brush? brush = null) =>
            ctx.DrawText(CreateFormattedText(text, brush ?? SystemColors.WindowTextBrush), new Point(x, y));

        #endregion

        #region IScrollInfo

        public bool CanHorizontallyScroll { get; set; }

        public bool CanVerticallyScroll { get; set; }

        public ScrollViewer ScrollOwner
        {
            get => _scrollOwner!;
            set => _scrollOwner = value;
        }

        public double ExtentHeight => this.TotalLines;

        public double ExtentWidth => 0;

        public double HorizontalOffset => 0;

        public double VerticalOffset => this.VerticalScrollOffset;

        public double ViewportHeight => this.DisplayLines;

        public double ViewportWidth => 0;

        public void LineDown() => AdjustVerticalScroll(1);

        public void LineUp() => AdjustVerticalScroll(-1);

        public void LineLeft() { }

        public void LineRight() { }

        public Rect MakeVisible(Visual visual, Rect rectangle) => Rect.Empty;

        public void MouseWheelDown()
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                AdjustZoom(ZoomStepAmount * -1);
            }
            else
            {
                int amount = GetMouseWheelScrollAmount();
                AdjustVerticalScroll(amount);
            }

            _caretAdorner?.ResetBlink();
        }

        public void MouseWheelUp()
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                AdjustZoom(ZoomStepAmount);
            }
            else
            {
                int amount = GetMouseWheelScrollAmount();
                AdjustVerticalScroll(amount * -1);
            }

            _caretAdorner?.ResetBlink();
        }

        private int GetMouseWheelScrollAmount() => 
            (int)((this.Provider?.Length ?? 0) / Int32.MaxValue) + 1;

        public void MouseWheelLeft() { }

        public void MouseWheelRight() { }

        public void PageDown()
        {
            AdjustVerticalScroll((int)this.ViewportHeight - 1);
            MoveCaretToByteIndex((int)(this.CaretByteIndex + ((this.ViewportHeight - 1) * this.BytesPerLine)));

            _caretAdorner?.ResetBlink();
        }

        public void PageUp()
        {
            AdjustVerticalScroll((int)-this.ViewportHeight);
            MoveCaretToByteIndex((int)(this.CaretByteIndex - (this.ViewportHeight * this.BytesPerLine)));

            _caretAdorner?.ResetBlink();
        }

        public void PageLeft() { }

        public void PageRight() { }

        public void SetHorizontalOffset(double offset) { }

        public void SetVerticalOffset(double offset)
        {
            if (Double.IsInfinity(offset))  // This will be the case if ScrollToBottom is called on the ScrollViewer
                offset = this.ExtentHeight - this.ViewportHeight;

            AdjustVerticalScroll((int)(offset - this.VerticalScrollOffset));

            _caretAdorner?.ResetBlink();
        }

        #endregion

        #region Event handlers

        private void HighlightRangeCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) =>
            InvalidateVisual();

        private void BookmarksCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) =>
            InvalidateVisual();

        private void Provider_ByteInserted(object? sender, ProviderByteEventArgs args)
        {
            for (int i = 0; i < this.HighlightRanges.Count; i++)
            {
                HighlightRange range = this.HighlightRanges[i];
                if (range == null)
                    continue;

                if (args.AtIndex < range.StartIndex)
                {
                    this.HighlightRanges.RemoveAt(i);
                    this.HighlightRanges.Insert(i, new HighlightRange(range.StartIndex + 1, range.EndIndex + 1, range.Brush));
                }
                else if (args.AtIndex <= range.EndIndex)
                {
                    this.HighlightRanges.RemoveAt(i);
                    this.HighlightRanges.Insert(i, new HighlightRange(range.StartIndex, range.EndIndex + 1, range.Brush));
                }
            }

            for (int i = 0; i < this.Bookmarks.Count; i++)
            {
                Bookmark bookmark = this.Bookmarks[i];

                if (args.AtIndex < bookmark.ByteIndex)
                {
                    this.Bookmarks.RemoveAt(i);
                    this.Bookmarks.Insert(i, bookmark with { ByteIndex = bookmark.ByteIndex + 1 });
                }
            }

            InvalidateVisual();
        }

        private void Provider_ByteDeleted(object? sender, ProviderByteEventArgs args)
        {
            for (int i = 0; i < this.HighlightRanges.Count; i++)
            {
                HighlightRange range = this.HighlightRanges[i];
                if (range == null)
                    continue;

                if (args.AtIndex < range.StartIndex)
                {
                    this.HighlightRanges.RemoveAt(i);
                    this.HighlightRanges.Insert(i, new HighlightRange(range.StartIndex - 1, range.EndIndex - 1, range.Brush));
                }
                else if (args.AtIndex <= range.EndIndex)
                {
                    this.HighlightRanges.RemoveAt(i);

                    if (range.StartIndex != range.EndIndex)
                        this.HighlightRanges.Insert(i, new HighlightRange(range.StartIndex, range.EndIndex - 1, range.Brush));
                }
            }

            for (int i = 0; i < this.Bookmarks.Count; i++)
            {
                Bookmark bookmark = this.Bookmarks[i];

                if (args.AtIndex == bookmark.ByteIndex)
                {
                    this.Bookmarks.RemoveAt(i);
                    i--;
                }
                else if (args.AtIndex < bookmark.ByteIndex)
                {
                    this.Bookmarks.RemoveAt(i);
                    this.Bookmarks.Insert(i, bookmark with { ByteIndex = bookmark.ByteIndex - 1 });
                }
            }

            InvalidateVisual();
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}
