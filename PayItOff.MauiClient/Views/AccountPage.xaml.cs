using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class AccountPage : ContentPage
{
    public AccountPage(AccountsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}