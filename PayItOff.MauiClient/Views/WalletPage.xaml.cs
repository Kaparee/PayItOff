using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class WalletPage : ContentPage
{
    private readonly WalletViewModel _viewModel;

    public WalletPage(WalletViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        await _viewModel.LoadDataAsync();
    }
}