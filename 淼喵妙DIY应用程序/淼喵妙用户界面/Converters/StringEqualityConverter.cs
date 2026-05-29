using System;
using System.Globalization;
using System.Windows.Data;

namespace 淼喵妙用户界面.Converters
{
    public class StringEqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is string s1 && values[1] is string s2)
                return s1 == s2;
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
