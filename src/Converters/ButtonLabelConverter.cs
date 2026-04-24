using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace EarthBackground.Converters
{
    public class ButtonLabelConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string text)
                return string.Empty;

            var firstSpace = text.IndexOf(' ');
            return firstSpace >= 0 ? text[(firstSpace + 1)..] : text;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
