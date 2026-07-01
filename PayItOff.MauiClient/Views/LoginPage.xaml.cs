using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (BindingContext is LoginViewModel vm)
            _ = vm.CheckForAppUpdateAsync();
    }
}