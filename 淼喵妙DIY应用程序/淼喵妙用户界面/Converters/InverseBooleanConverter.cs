using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace 淼喵妙用户界面.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)(value ?? false);
            string paramStr = parameter as string;
            
            if (!string.IsNullOrEmpty(paramStr) && paramStr.Contains(","))
            {
                string[] colors = paramStr.Split(',');
                if (colors.Length == 2)
                {
                    // InverseBooleanConverter：value=true时反转为false，取第一个颜色；value=false时反转为true，取第二个颜色
                    string colorStr = boolValue ? colors[0].Trim() : colors[1].Trim();
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorStr));
                }
            }
            
            return !boolValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)(value ?? false);
        }
    }
}
