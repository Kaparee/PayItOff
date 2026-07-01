namespace PayItOff.MauiClient.Controls;

public partial class AppButton : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(AppButton), string.Empty);
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(System.Windows.Input.ICommand), typeof(AppButton), null);
    public System.Windows.Input.ICommand Command
    {
        get => (System.Windows.Input.ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(AppButton), null);
    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly BindableProperty ButtonBackgroundColorProperty =
        BindableProperty.Create(nameof(ButtonBackgroundColor), typeof(Color), typeof(AppButton), Color.FromArgb("#7F00FF"));
    public Color ButtonBackgroundColor
    {
        get => (Color)GetValue(ButtonBackgroundColorProperty);
        set => SetValue(ButtonBackgroundColorProperty, value);
    }

    public static readonly BindableProperty ButtonTextColorProperty =
        BindableProperty.Create(nameof(ButtonTextColor), typeof(Color), typeof(AppButton), Colors.White);
    public Color ButtonTextColor
    {
        get => (Color)GetValue(ButtonTextColorProperty);
        set => SetValue(ButtonTextColorProperty, value);
    }

    public static readonly BindableProperty ButtonHeightProperty =
        BindableProperty.Create(nameof(ButtonHeight), typeof(double), typeof(AppButton), 48.0);
    public double ButtonHeight
    {
        get => (double)GetValue(ButtonHeightProperty);
        set => SetValue(ButtonHeightProperty, value);
    }

    public static readonly BindableProperty ButtonFontSizeProperty =
        BindableProperty.Create(nameof(ButtonFontSize), typeof(double), typeof(AppButton), 14.0);
    public double ButtonFontSize
    {
        get => (double)GetValue(ButtonFontSizeProperty);
        set => SetValue(ButtonFontSizeProperty, value);
    }

    public static readonly BindableProperty ButtonFontAttributesProperty =
        BindableProperty.Create(nameof(ButtonFontAttributes), typeof(FontAttributes), typeof(AppButton), FontAttributes.Bold);
    public FontAttributes ButtonFontAttributes
    {
        get => (FontAttributes)GetValue(ButtonFontAttributesProperty);
        set => SetValue(ButtonFontAttributesProperty, value);
    }

    public static readonly BindableProperty ButtonPaddingProperty =
        BindableProperty.Create(nameof(ButtonPadding), typeof(Thickness), typeof(AppButton), new Thickness(14, 10));
    public Thickness ButtonPadding
    {
        get => (Thickness)GetValue(ButtonPaddingProperty);
        set => SetValue(ButtonPaddingProperty, value);
    }

    public static readonly BindableProperty ButtonBorderColorProperty =
        BindableProperty.Create(nameof(ButtonBorderColor), typeof(Color), typeof(AppButton), Colors.Transparent);
    public Color ButtonBorderColor
    {
        get => (Color)GetValue(ButtonBorderColorProperty);
        set => SetValue(ButtonBorderColorProperty, value);
    }

    public static readonly BindableProperty ButtonBorderWidthProperty =
        BindableProperty.Create(nameof(ButtonBorderWidth), typeof(double), typeof(AppButton), 0.0);
    public double ButtonBorderWidth
    {
        get => (double)GetValue(ButtonBorderWidthProperty);
        set => SetValue(ButtonBorderWidthProperty, value);
    }

    public AppButton()
    {
        InitializeComponent();
    }
}
