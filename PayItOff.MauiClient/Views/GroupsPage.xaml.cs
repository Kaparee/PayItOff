using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class GroupsPage : ContentPage
{
    public GroupsPage(GroupsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}