namespace PayItOff.MauiClient.Controls;

public partial class SidebarMenu : ContentView
{
    // Rejestracja właściwości ActiveTab, aby można było ją bindować w XAML
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