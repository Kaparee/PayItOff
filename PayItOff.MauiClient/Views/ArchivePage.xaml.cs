using PayItOff.MauiClient.ViewModels;

namespace PayItOff.MauiClient.Views;

public partial class ArchivePage : ContentPage
{
    public ArchivePage(ArchiveViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (BindingContext is ArchiveViewModel vm)
        {
            await vm.LoadArchivedGroupsAsync();
        }
    }
}