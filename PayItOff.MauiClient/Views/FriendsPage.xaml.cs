using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class FriendsPage : ContentPage
{
    public FriendsPage(FriendsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}