using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class NotificationsPage : ContentPage
{
    public NotificationsPage(NotificationsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (BindingContext is NotificationsViewModel vm)
        {
            _ = vm.LoadNotificationsAsync();
        }
    }
}