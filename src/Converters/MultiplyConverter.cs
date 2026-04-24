using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace EarthBackground.Converters
{
    public class MultiplyConverter : IValueConverter
    {
        public double Factor { get; set; } = 1;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var number = value switch
            {
                double doubleValue => doubleValue,
                int intValue => intValue,
                Rect rect => rect.Width,
                _ => 0
            };

            return number * Factor;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
