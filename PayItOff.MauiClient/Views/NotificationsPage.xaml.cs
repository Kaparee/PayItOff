using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class NotificationsPage : ContentPage
{
    public NotificationsPage(NotificationsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (BindingContext is NotificationsViewModel vm)
        {
            vm.SubscribeToEvents();
            await vm.LoadNotificationsAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        (BindingContext as NotificationsViewModel)?.OnDisappearing();
    }
}