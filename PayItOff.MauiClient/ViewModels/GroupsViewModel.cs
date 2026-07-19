using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Responses;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PayItOff.MauiClient.ViewModels;

public partial class GroupsViewModel : PopupViewModelBase
{
    private readonly GroupService _groupService;
    private readonly GroupMemberService _groupMemberService;
    private readonly SignalRService _signalRService;
    private List<GroupInfoResponse> _allGroups = new();

    private List<GroupPendingInvitationResponse> _allInvitations = new();

    [ObservableProperty]
    public partial ObservableCollection<GroupInfoResponse> Groups { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<GroupPendingInvitationResponse> Invites { get; set; }

    [ObservableProperty]
    public partial bool IsCreatePopupVisible { get; set; }

    [ObservableProperty]
    public partial bool IsInviteListPopupVisible { get; set; }

    [ObservableProperty]
    public partial bool IsInviteListButtonVisible { get; set; }

    [ObservableProperty]
    public partial string NewGroupName { get; set; }

    [ObservableProperty]
    public partial ImageSource? SelectedAvatarSource { get; set; }

    [ObservableProperty]
    public partial string SearchQuery { get; set; }

    private Stream? _tempAvatarStream;
    private string? _tempFileName;

    public GroupsViewModel(
        GroupService groupService,
        GroupMemberService groupMemberService,
        SignalRService signalRService
        )
    {
        _groupService = groupService;
        _groupMemberService = groupMemberService;
        _signalRService = signalRService;
        Groups = new ObservableCollection<GroupInfoResponse>();
        Invites = new ObservableCollection<GroupPendingInvitationResponse>();
        IsCustomAlertSupported = true;
        _ = LoadGroupsAsync();
    }

    public void SubscribeToEvents()
    {
        _signalRService.OnInvitationReceived += HandleInvitationReceived;
    }

    public void UnsubscribeFromEvents()
    {
        _signalRService.OnInvitationReceived -= HandleInvitationReceived;
    }

    private void HandleInvitationReceived()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await IsInvitePending();
        });
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    public async Task LoadGroupsAsync()
    {
        var response = await _groupService.GetUserGroups();

        _allGroups = response;

        Groups.Clear();
        foreach (var group in response)
        {
            Groups.Add(new GroupInfoResponse
            {
                Id = group.Id,
                Name = group.Name,
                Income = group.Income,
                Expense = group.Expense,
                Balance = group.Balance,
                IsFavorite = group.IsFavorite,
                AvatarUrl = group.AvatarUrl,
                UpdatedAt = group.UpdatedAt
            });
        }

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Groups.Clear();

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            foreach (var group in _allGroups)
            {
                Groups.Add(group);
            }
        }
        else
        {
            var lowerQuery = SearchQuery.ToLower();
            var filtered = _allGroups.Where(g => g.Name.ToLower().Contains(lowerQuery));

            foreach (var group in filtered)
            {
                Groups.Add(group);
            }
        }

        _ = IsInvitePending();
    }

    private async Task IsInvitePending()
    {
        var invites = await _groupMemberService.GetPendingInvitationsAsync();

        if(invites != null && invites.Count() > 0)
        {
            IsInviteListButtonVisible = true;
        } else
        {
            IsInviteListButtonVisible = false;
        }
    }

    [RelayCommand]
    private async Task ShowInvitationPopup()
    {
        var invites = await _groupMemberService.GetPendingInvitationsAsync();

        _allInvitations = invites!;

        Invites.Clear();
        if (invites != null && invites.Count() > 0)
        {
            foreach (var invite in invites)
            {
                Invites.Add(new GroupPendingInvitationResponse
                {
                    InvitationId = invite.InvitationId,
                    GroupId = invite.GroupId,
                    GroupName = invite.GroupName,
                    GroupAvatarUrl = invite.GroupAvatarUrl,
                    Role = invite.Role,
                    InvitedAt = invite.InvitedAt
                });
            }
        }
        IsInviteListPopupVisible = true;
    }

    [RelayCommand]
    public async Task AcceptInviteAsync(GroupPendingInvitationResponse invite)
    {
        await _groupMemberService.AcceptInviteAsync(invite.InvitationId);

        Invites.Remove(invite);

        if (Invites.Count == 0)
        {
            CloseInvitationPopup();
            IsInviteListButtonVisible = false;
        }

        await LoadGroupsAsync();
    }

    [RelayCommand]
    public async Task DeclineInviteAsync(GroupPendingInvitationResponse invite)
    {
        await _groupMemberService.DeclineInviteAsync(invite.InvitationId);

        Invites.Remove(invite);

        if (Invites.Count == 0)
        {
            CloseInvitationPopup();
            IsInviteListButtonVisible = false;
        }

        await LoadGroupsAsync();
    }

    [RelayCommand]
    private void CloseInvitationPopup()
    {
        IsInviteListPopupVisible = false;
    }

    [RelayCommand]
    private void ShowCreatePopup()
    {
        NewGroupName = string.Empty;
        SelectedAvatarSource = null;
        _tempAvatarStream = null;
        _tempFileName = null;
        IsCreatePopupVisible = true;
    }

    [RelayCommand]
    private void ClosePopup()
    {
        IsCreatePopupVisible = false;
    }

    [RelayCommand]
    private async Task PickPhoto()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Wybierz awatar grupy",
            FileTypes = FilePickerFileType.Images
        });

        if (result != null)
        {
            _tempFileName = result.FileName;
            var stream = await result.OpenReadAsync();

            var memStream = new MemoryStream();
            await stream.CopyToAsync(memStream);
            memStream.Position = 0;
            _tempAvatarStream = memStream;

            SelectedAvatarSource = ImageSource.FromStream(() =>
            {
                var s = new MemoryStream(memStream.ToArray());
                return s;
            });
        }
    }

    [RelayCommand]
    private async Task ConfirmCreateGroup()
    {
        if (string.IsNullOrWhiteSpace(NewGroupName)) return;
        if (_tempAvatarStream != null) _tempAvatarStream.Position = 0;

        var isSuccess = await _groupService.CreateGroup(NewGroupName, _tempAvatarStream, _tempFileName);

        if (isSuccess)
        {
            IsCreatePopupVisible = false;
            await LoadGroupsAsync();
        }
        else
        {
            await ShowAlertAsync("Błąd", "Nie udało się utworzyć grupy. Sprawdź połączenie lub nazwę.", "OK");
        }
    }

    [RelayCommand]
    private async Task ToggleFavorite(GroupInfoResponse group)
    {
        if (group == null) return;

        var isSuccess = await _groupService.SetGroupFavorite(group.Id);

        if (isSuccess)
        {
            group.IsFavorite = !group.IsFavorite;
            await LoadGroupsAsync();
        }
        else
        {
            await ShowAlertAsync("Błąd", "Nie udało się zmienić statusu ulubionych.", "OK");
        }
    }

    public ICommand NavigateToGroupDetailsCommand => new Command<GroupInfoResponse>(async (group) =>
    {
        if (group == null) return;
        await Shell.Current.GoToAsync($"//GroupDetailsPage?groupId={group.Id}");
    });
}
