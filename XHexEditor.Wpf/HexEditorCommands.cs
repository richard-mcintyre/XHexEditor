using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using SR = XHexEditor.Wpf.Properties.Resources;

namespace XHexEditor.Wpf
{
    public static class HexEditorCommands
    {
        public static RoutedUICommand Zoom50Percent { get; } =
            new RoutedUICommand("50%", nameof(Zoom50Percent), typeof(HexEditorCommands));

        public static RoutedUICommand Zoom100Percent { get; } =
            new RoutedUICommand("100%", nameof(Zoom100Percent), typeof(HexEditorCommands));

        public static RoutedUICommand Zoom150Percent { get; } =
            new RoutedUICommand("150%", nameof(Zoom150Percent), typeof(HexEditorCommands));

        public static RoutedUICommand Zoom200Percent { get; } =
            new RoutedUICommand("200%", nameof(Zoom200Percent), typeof(HexEditorCommands));

        public static RoutedUICommand ToggleBookmark { get; } = 
            new RoutedUICommand(SR.MenuItem_ToggleBookmark, nameof(ToggleBookmark), typeof(HexEditorCommands),
                new InputGestureCollection(new KeyGesture[] { new KeyGesture(Key.F9) }));

        public static RoutedUICommand PreviousBookmark { get; } = 
            new RoutedUICommand(SR.MenuItem_PrevBookmark, nameof(PreviousBookmark), typeof(HexEditorCommands),
                new InputGestureCollection(new KeyGesture[] { new KeyGesture(Key.F10) }));

        public static RoutedUICommand NextBookmark { get; } = 
            new RoutedUICommand(SR.MenuItem_NextBookmark, nameof(NextBookmark), typeof(HexEditorCommands),
                new InputGestureCollection(new KeyGesture[] { new KeyGesture(Key.F11) }));

        public static RoutedUICommand RemoveAllHighlights { get; } = 
            new RoutedUICommand(SR.MenuItem_RemoveAllHighlights, nameof(RemoveAllHighlights), typeof(HexEditorCommands));

        public static RoutedUICommand RemoveHighlight { get; } =
            new RoutedUICommand(SR.MenuItem_RemoveHighlight, nameof(RemoveHighlight), typeof(HexEditorCommands));

        public static RoutedUICommand HighlightSelection { get; } =
            new RoutedUICommand(SR.MenuItem_HighlightSelection, nameof(HighlightSelection), typeof(HexEditorCommands));

    }
}
