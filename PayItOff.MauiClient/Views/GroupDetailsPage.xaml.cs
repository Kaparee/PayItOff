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
            _ = vm.LoadDataAsync();
        }
    }

    private int _currentIndex = 0;

    private void OnPrevMemberClicked(object? sender, TappedEventArgs e)
    {
        if (BindingContext is GroupDetailsViewModel vm && vm.FilteredMembers.Count > 0)
        {
            _currentIndex = (_currentIndex - 1 + vm.FilteredMembers.Count) % vm.FilteredMembers.Count;
            MembersCollectionView.ScrollTo(_currentIndex, position: ScrollToPosition.Center);
        }
    }

    private void OnNextMemberClicked(object? sender, TappedEventArgs e)
    {
        if (BindingContext is GroupDetailsViewModel vm && vm.FilteredMembers.Count > 0)
        {
            _currentIndex = (_currentIndex + 1) % vm.FilteredMembers.Count;
            MembersCollectionView.ScrollTo(_currentIndex, position: ScrollToPosition.Center);
        }
    }
}