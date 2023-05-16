using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using SR = XHexEditor.Wpf.App.Properties.Resources;

namespace XHexEditor.Wpf.App.Converters
{
    public class ApplicationTitleConverter : IMultiValueConverter
    {
        public static IMultiValueConverter Instance { get; } = new ApplicationTitleConverter();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 3)
                return String.Empty;

            string appTitle = System.Convert.ToString(values[0])!;

            string fileName = SR.Untitled;
            if (values[1] is not null && String.IsNullOrWhiteSpace(values[1].ToString()) == false)
            {
                string filePath = System.Convert.ToString(values[1])!;
                fileName = System.IO.Path.GetFileName(filePath);
            }

            bool isModified = System.Convert.ToBoolean(values[2]);

            return $"{appTitle} - {fileName} {(isModified ? "*" : String.Empty)}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => new object[] { Binding.DoNothing };
    }
}
