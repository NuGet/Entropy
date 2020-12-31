using System;
using System.Globalization;
using System.Runtime.Versioning;
using System.Windows.Data;

namespace IVsTestingExtension.Xaml.ApiInvoker
{
    [ValueConversion(typeof(string), typeof(FrameworkName))]
    public class FrameworkNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return ((FrameworkName)value).ToString();
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var frameworkName = new FrameworkName((string)value);
                return frameworkName;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
