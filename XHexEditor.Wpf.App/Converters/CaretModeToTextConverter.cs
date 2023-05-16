using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace XHexEditor.Wpf.App.Converters
{
    public class CaretModeToTextConverter : IValueConverter
    {
        public static IValueConverter Instance { get; } = new CaretModeToTextConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((CaretMode)value)
            {
                case CaretMode.Insert:
                    return "INS";

                case CaretMode.Overwrite:
                    return "OVR";
            }

            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
