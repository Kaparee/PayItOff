using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class NotificationsPage : ContentPage
{
    public NotificationsPage(NotificationsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}