namespace PayItOff.MauiClient.Helpers;

public class AllTrueConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return values.All(v => v is bool b && b);
    }

    public object[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

