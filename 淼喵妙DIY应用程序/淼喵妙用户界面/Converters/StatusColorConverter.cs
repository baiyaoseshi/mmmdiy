using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace 淼喵妙用户界面.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            return status switch
            {
                TaskConstants.Status.运行中 => new SolidColorBrush(Color.FromRgb(0, 204, 0)),
                TaskConstants.Status.等待中 => new SolidColorBrush(Color.FromRgb(255, 200, 0)),
                TaskConstants.Status.完成 => new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                TaskConstants.Status.失败 => new SolidColorBrush(Color.FromRgb(204, 0, 0)),
                _ => new SolidColorBrush(Color.FromRgb(136, 136, 136))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? TaskConstants.启用 : TaskConstants.禁用;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnabledColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value 
                ? new SolidColorBrush(Color.FromRgb(0, 204, 0))
                : new SolidColorBrush(Color.FromRgb(204, 0, 0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CountdownConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan? nullableCountdown = value as TimeSpan?;
            if (nullableCountdown.HasValue)
            {
                TimeSpan countdown = nullableCountdown.Value;
                if (countdown.TotalSeconds <= 0)
                    return "即将执行";
                return $"{countdown.Hours:D2}:{countdown.Minutes:D2}:{countdown.Seconds:D2}";
            }
            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ModeButtonColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isTaskManagementMode = (bool)value;
            bool reverse = parameter?.ToString() == "reverse";
            
            if (reverse)
                return isTaskManagementMode ? new SolidColorBrush(Color.FromRgb(128, 128, 128)) 
                                           : new SolidColorBrush(Color.FromRgb(0, 122, 204));
            else
                return isTaskManagementMode ? new SolidColorBrush(Color.FromRgb(0, 122, 204)) 
                                           : new SolidColorBrush(Color.FromRgb(128, 128, 128));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}