namespace PayItOff.MauiClient.Controls;

public partial class SidebarMenu : ContentView
{

    public static readonly BindableProperty ActiveTabProperty =
        BindableProperty.Create(nameof(ActiveTab), typeof(string), typeof(SidebarMenu), "Home");

    public string ActiveTab
    {
        get => (string)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    public SidebarMenu()
    {
        InitializeComponent();
    }
}