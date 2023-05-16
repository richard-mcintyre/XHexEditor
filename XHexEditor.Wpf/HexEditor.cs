using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using XHexEditor.Providers;

namespace XHexEditor.Wpf
{
    public class HexEditor : Control, INotifyPropertyChanged
    {
        #region Dependency properties

        public static readonly DependencyProperty ProviderProperty =
            DependencyProperty.Register(nameof(Provider), typeof(IProvider), typeof(HexEditor),
                new PropertyMetadata(new StreamProvider(new MemoryStream())));

        public static readonly DependencyProperty HighlightBrushesProperty =
            DependencyProperty.Register(nameof(HighlightBrushes), typeof(ObservableCollection<Brush>), typeof(HexEditor),
                new PropertyMetadata(new ObservableCollection<Brush>()));

        public static readonly DependencyProperty HighlightRangesProperty =
            DependencyProperty.Register(nameof(HighlightRanges), typeof(HighlightRangeCollection), typeof(HexEditor),
                new PropertyMetadata(new HighlightRangeCollection()));

        public static readonly DependencyProperty SelectedRangeProperty =
            DependencyProperty.Register(nameof(SelectedRange), typeof(ProviderRange), typeof(HexEditor));

        public static readonly DependencyProperty CaretModeProperty =
            DependencyProperty.Register(nameof(CaretMode), typeof(CaretMode), typeof(HexEditor));

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(HexEditor), new PropertyMetadata(1d));

        public static readonly DependencyProperty BookmarksProperty =
            DependencyProperty.Register(nameof(Bookmarks), typeof(BookmarkCollection), typeof(HexEditor),
                new PropertyMetadata(new BookmarkCollection()));

        #endregion

        #region Construction

        public HexEditor()
        {
            this.DefaultStyleKey = typeof(HexEditor);
        }

        #endregion

        #region Fields

        private HexEditorInternal? _editor;

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

        public ProviderRange? SelectedRange
        {
            get => (ProviderRange?)GetValue(SelectedRangeProperty);
            set => SetValue(SelectedRangeProperty, value);
        }

        public CaretMode CaretMode
        {
            get => (CaretMode)GetValue(CaretModeProperty);
            set => SetValue(CaretModeProperty, value);
        }

        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        /// <summary>
        /// The index into the provider of the byte that the caret is on
        /// </summary>
        public long CaretByteIndex => _editor?.CaretByteIndex ?? 0;

        public BookmarkCollection Bookmarks
        {
            get => (BookmarkCollection)GetValue(BookmarksProperty);
            set => SetValue(BookmarksProperty, value);
        }

        /// <summary>
        /// Determines if the editor has unsaved changes
        /// </summary>
        public bool IsModified => _editor?.IsModified ?? false;

        #endregion

        #region Methods

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs args)
        {
            _editor?.Focus();
            args.Handled = true;
        }

        public override void OnApplyTemplate()
        {
            _editor = GetTemplateChild("PART_Editor") as HexEditorInternal;
            if (_editor != null)
            {
                PropertyChangedEventManager.AddHandler(_editor, 
                    (s, a) => OnPropertyChanged(nameof(CaretByteIndex)), nameof(HexEditorInternal.CaretByteIndex));

                PropertyChangedEventManager.AddHandler(_editor,
                    (s, a) => OnPropertyChanged(nameof(IsModified)), nameof(HexEditorInternal.IsModified));
            }
        }

        public void SetZoom(double scale)
        {
            if (_editor != null)
                _editor.Zoom = scale;
        }

        /// <summary>
        /// Saves any changes made by the user to the provider
        /// </summary>
        public async Task SaveChangesAsync(CancellationToken cancellation, IProgress<ApplyChangesProgressInfo>? progress)
        {
            if (_editor is null)
                return;

            await _editor.SaveChangesAsync(cancellation, progress);
        }

        public void ToggleBookmark(long byteIndex) =>
            _editor?.ToggleBookmark(byteIndex);

        public bool MoveToPreviousBookmark() =>
            _editor?.MoveToPreviousBookmark() ?? false;

        public bool MoveToNextBookmark() =>
            _editor?.MoveToNextBookmark() ?? false;

        public async Task ApplyChangesAsync(CancellationToken cancellation, IProgress<ApplyChangesProgressInfo>? progress)
        {
            if (_editor is not null)
                await _editor.ApplyChangesAsync(cancellation, progress);
        }

        /// <summary>
        /// Ensures that hte specified byte index is visible, scrolling if necessary
        /// </summary>
        public void EnsureByteIndexIsVisible(long index) =>
            _editor?.EnsureByteIndexIsVisible(index);

        /// <summary>
        /// Moves the caret to the specified byte
        /// </summary>
        public void MoveCaretToByteIndex(long index) =>
            _editor?.MoveCaretToByteIndex(index);

        #endregion

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion
    }
}
