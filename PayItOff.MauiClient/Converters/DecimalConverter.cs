using System.Globalization;

namespace PayItOff.MauiClient.Converters;

public class DecimalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            return d == 0 ? string.Empty : d.ToString(culture);
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            string separator = culture.NumberFormat.NumberDecimalSeparator;
            s = s.Replace(".", separator).Replace(",", separator);

            if (decimal.TryParse(s, NumberStyles.Any, culture, out decimal result))
            {
                return result;
            }
        }
        return 0m;
    }
}
