using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace XHexEditor.Wpf.App.Converters
{
    public class ZoomLevelToTextConverter : IValueConverter
    {
        public static IValueConverter Instance { get; } = new ZoomLevelToTextConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            $"{(((double)value) * 100):.}%";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
