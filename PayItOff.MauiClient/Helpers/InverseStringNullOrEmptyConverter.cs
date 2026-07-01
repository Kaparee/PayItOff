namespace PayItOff.MauiClient.Helpers;

public class InverseStringNullOrEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        var str = value as string;
        if (string.IsNullOrEmpty(str)) return true;
        if (str.Contains("default_")) return true;
        return false;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
}

