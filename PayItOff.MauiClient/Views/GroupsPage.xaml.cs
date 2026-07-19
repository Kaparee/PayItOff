using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class GroupsPage : ContentPage
{
    public GroupsPage(GroupsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (BindingContext is GroupsViewModel vm)
        {
            vm.SubscribeToEvents();
            await vm.LoadGroupsAsync();
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        
        if (BindingContext is GroupsViewModel vm)
        {
            vm.UnsubscribeFromEvents();
        }
    }
}