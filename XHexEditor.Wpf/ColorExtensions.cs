using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace XHexEditor.Wpf
{
    static class ColorExtensions
    {
        public static Color Darken(this Color color, double factor)
        {
            byte red = (byte)(color.R * factor);
            byte green = (byte)(color.G * factor);
            byte blue = (byte)(color.B * factor);

            return Color.FromRgb(red, green, blue);
        }
    }
}
