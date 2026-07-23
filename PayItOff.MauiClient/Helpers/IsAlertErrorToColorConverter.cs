namespace PayItOff.MauiClient.Helpers;

public class IsAlertErrorToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isError && isError)
        {
            return Color.FromArgb("#EF4444"); // StatusRed
        }

        return Color.FromArgb("#22C55E"); // StatusGreen
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

