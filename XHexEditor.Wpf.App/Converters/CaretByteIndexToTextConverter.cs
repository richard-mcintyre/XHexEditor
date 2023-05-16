using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace XHexEditor.Wpf.App.Converters
{
    public class CaretByteIndexToTextConverter : IValueConverter
    {
        public static IValueConverter Instance { get; } = new CaretByteIndexToTextConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            $"0x{value:X8}";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
