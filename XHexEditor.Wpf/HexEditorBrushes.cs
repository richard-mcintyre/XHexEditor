using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace XHexEditor.Wpf
{
    static class HexEditorBrushes
    {
        static HexEditorBrushes()
        {
            Freeze(ChangedBytesBrush);
            Freeze(CaretRowColumnPen);
            Freeze(SelectionBrush);
            Freeze(ZoomNotificationBackground);
            Freeze(ActiveBorderBrush);
            Freeze(InactiveBorderBrush);
            Freeze(BookmarkBrush);
        }

        public static Brush ChangedBytesBrush => Brushes.Red;

        public static Pen CaretRowColumnPen => new Pen(new SolidColorBrush(Color.FromRgb(0xe0, 0xe0, 0xe0)), 1);

        public static Brush SelectionBrush => new SolidColorBrush(Color.FromArgb((int)(255 * 0.3), 30, 144, 255));

        public static Brush ZoomNotificationBackground => new SolidColorBrush(Color.FromRgb(50, 50, 50));

        public static Brush ActiveBorderBrush => new SolidColorBrush(Color.FromRgb(130, 135, 144));

        public static Brush InactiveBorderBrush => new SolidColorBrush(Color.FromRgb(160, 165, 174));

        public static Brush BookmarkBrush => new SolidColorBrush(Color.FromRgb(150, 150, 225));

        private static void Freeze(Freezable freezable)
        {
            if (freezable.CanFreeze)
                freezable.Freeze();
        }

        private static Color FromHex(uint color) =>
            Color.FromRgb((byte)((color >> 16) & 0xFF), (byte)((color >> 8) & 0xFF), (byte)(color & 0xFF));
    }
}
