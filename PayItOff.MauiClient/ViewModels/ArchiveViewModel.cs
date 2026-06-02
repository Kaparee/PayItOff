using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Responses;

namespace PayItOff.MauiClient.ViewModels;

public partial class ArchiveViewModel : PopupViewModelBase
{
    private readonly GroupService _groupService;
    public ObservableCollection<GroupInfoResponse> ArchivedGroups { get; } = new();

    public ArchiveViewModel(GroupService groupService)
    {
        _groupService = groupService;
        IsCustomAlertSupported = true;
    }

    public async Task LoadArchivedGroupsAsync()
    {
        IsBusy = true;
        try
        {
            ArchivedGroups.Clear();
            var groups = await _groupService.GetArchivedGroups();
            foreach (var g in groups)
            {
                ArchivedGroups.Add(g);
            }
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Błąd", $"Nie udało się pobrać zarchiwizowanych grup: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GroupTapped(GroupInfoResponse group)
    {
        if (group == null) return;
        
        await Shell.Current.GoToAsync($"//GroupDetailsPage?groupId={group.Id}");
    }
}