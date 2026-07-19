using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class GroupDetailsPage : ContentPage, IQueryAttributable
{
    public GroupDetailsPage(GroupDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is GroupDetailsViewModel vm)
            vm.ApplyQueryAttributes(query);
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (BindingContext is GroupDetailsViewModel vm && vm.GroupId > 0)
        {
            vm.SubscribeToEvents();
            _ = vm.LoadDataAsync();
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        if (BindingContext is GroupDetailsViewModel vm)
        {
            vm.UnsubscribeFromEvents();
        }
    }

}