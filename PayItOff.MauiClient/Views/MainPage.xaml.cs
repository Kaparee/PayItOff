using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (_viewModel != null)
        {
            _viewModel.SubscribeToEvents();
            await _viewModel.LoadDashboardDataAsync();
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);

        if (_viewModel != null)
        {
            _viewModel.UnsubscribeFromEvents();
        }
    }
}