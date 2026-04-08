using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}