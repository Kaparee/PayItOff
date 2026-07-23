using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class FriendsPage : ContentPage
{
    private readonly FriendsViewModel _viewModel;

    public FriendsPage(FriendsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        _viewModel.SubscribeToEvents();
        _viewModel.LoadFriendsCommand.Execute(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        (BindingContext as FriendsViewModel)?.OnDisappearing();
    }
}