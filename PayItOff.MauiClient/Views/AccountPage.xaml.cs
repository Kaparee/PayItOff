using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class AccountPage : ContentPage
{
    public AccountPage(AccountsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (BindingContext is AccountsViewModel vm)
        {
            await vm.LoadProfileAsync();
        }
    }
}