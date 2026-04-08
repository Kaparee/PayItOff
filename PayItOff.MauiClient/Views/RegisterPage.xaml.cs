using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}