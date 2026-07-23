namespace PayItOff.MauiClient.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Collections.ObjectModel;

public partial class FriendsViewModel : BaseViewModel
{
    private readonly FriendService _friendService;
    private readonly SignalRService _signalRService;

    private readonly List<FriendDisplayModel> _allFriends = new();

    public ObservableCollection<FriendDisplayModel> Friends { get; } = new();
    public ObservableCollection<FriendDisplayModel> TopDebtors { get; } = new();
    public ObservableCollection<FriendDisplayModel> TopDebts { get; } = new();

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsInvitePopupVisible { get; set; }

    [ObservableProperty]
    public partial bool IsFriendDetailsPopupVisible { get; set; }

    [ObservableProperty]
    public partial FriendDisplayModel? SelectedFriendForDetails { get; set; }

    [ObservableProperty]
    public partial string InviteFriendId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string InviteNickname { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string InviteEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string InvitePhoneNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsDeletePopupVisible { get; set; }

    [ObservableProperty]
    public partial FriendDisplayModel? FriendToDelete { get; set; }

    [ObservableProperty]
    public partial decimal TotalOwed { get; set; }

    [ObservableProperty]
    public partial decimal TotalOwedToMe { get; set; }

    private CancellationTokenSource? _searchCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSearchedUserVisible))]
    public partial SearchUserResponse? SearchedUser { get; set; }

    public bool IsSearchedUserVisible => SearchedUser != null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmptyListMessage))]
    public partial FriendFilterType ActiveFilter { get; set; } = FriendFilterType.Friends;

    public string EmptyListMessage => ActiveFilter switch
    {
        FriendFilterType.Friends => "Brak Znajomych",
        FriendFilterType.Sent => "Brak Wysłanych Zaproszeń",
        FriendFilterType.Pending => "Brak Zaproszeń",
        _ => "Brak Znajomych"
    };

    [ObservableProperty]
    public partial int FriendsCount { get; set; }

    [ObservableProperty]
    public partial int SentCount { get; set; }

    [ObservableProperty]
    public partial int PendingCount { get; set; }

    [ObservableProperty]
    public partial string DeletePopupTitle { get; set; } = "Usuwanie znajomego";

    [ObservableProperty]
    public partial string DeletePopupSubtitle { get; set; } = "Czy na pewno chcesz usunąć tego znajomego?";

    [ObservableProperty]
    public partial bool IsAlertPopupVisible { get; set; }

    [ObservableProperty]
    public partial string AlertTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AlertMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsAlertError { get; set; } = true;

    [ObservableProperty]
    public partial bool IsSharedGroupsPopupVisible { get; set; }

    [ObservableProperty]
    public partial string SelectedFriendFullName { get; set; } = string.Empty;

    public ObservableCollection<SharedGroupResponse> SelectedFriendSharedGroups { get; } = new();

    [RelayCommand]
    private void CloseAlertPopup()
    {
        IsAlertPopupVisible = false;
    }

    private void ShowAlert(string title, string message, bool isError = true)
    {
        AlertTitle = title;
        AlertMessage = message;
        IsAlertError = isError;
        IsAlertPopupVisible = true;
    }

    public FriendsViewModel(FriendService friendService, SignalRService signalRService)
    {
        _friendService = friendService;
        _signalRService = signalRService;
    }

    public void SubscribeToEvents()
    {
        _signalRService.OnFriendUpdateReceived += HandleFriendUpdate;
    }

    public void UnsubscribeFromEvents()
    {
        _signalRService.OnFriendUpdateReceived -= HandleFriendUpdate;
    }

    public void OnDisappearing()
    {
        UnsubscribeFromEvents();
    }

    partial void OnSearchQueryChanged(string value)
    {
        FilterFriends();
    }

    partial void OnInviteNicknameChanged(string value) => TriggerSearch();
    partial void OnInviteEmailChanged(string value) => TriggerSearch();
    partial void OnInvitePhoneNumberChanged(string value) => TriggerSearch();
    partial void OnActiveFilterChanged(FriendFilterType value) => FilterFriends();

    private void TriggerSearch()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        var nick = InviteNickname;
        var email = InviteEmail;
        var phone = InvitePhoneNumber;

        if (string.IsNullOrWhiteSpace(nick) && string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
        {
            SearchedUser = null;
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (token.IsCancellationRequested) return;

                var user = await _friendService.SearchUserAsync(
                    string.IsNullOrWhiteSpace(nick) ? null : nick.Trim(),
                    string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                    string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
                );
                if (token.IsCancellationRequested) return;

                MainThread.BeginInvokeOnMainThread(() => SearchedUser = user);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search user error: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(() => SearchedUser = null);
            }
        }, token);
    }

    [RelayCommand]
    private void ShowSharedGroupsPopup(FriendDisplayModel friend)
    {
        if (friend == null || !friend.HasSharedGroups) return;
        SelectedFriendFullName = friend.FullName;
        SelectedFriendSharedGroups.Clear();
        foreach (var group in friend.SharedGroupsList)
        {
            SelectedFriendSharedGroups.Add(group);
        }
        IsSharedGroupsPopupVisible = true;
    }

    [RelayCommand]
    private void CloseSharedGroupsPopup()
    {
        IsSharedGroupsPopupVisible = false;
    }

    [RelayCommand]
    private async Task GoToGroupAsync(SharedGroupResponse group)
    {
        if (group == null) return;
        IsSharedGroupsPopupVisible = false;
        _signalRService.OnFriendUpdateReceived -= HandleFriendUpdate;
        await Shell.Current.GoToAsync($"//GroupDetailsPage?groupId={group.GroupId}");
    }

    [RelayCommand]
    private void ShowFriendDetailsPopup(FriendDisplayModel friend)
    {
        if (friend == null) return;
        SelectedFriendForDetails = friend;
        IsFriendDetailsPopupVisible = true;
    }

    [RelayCommand]
    private void CloseFriendDetailsPopup()
    {
        IsFriendDetailsPopupVisible = false;
        SelectedFriendForDetails = null;
    }

    [RelayCommand]
    public async Task LoadFriendsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var friendsResponse = await _friendService.GetUserFriendListAsync();
            var pendingResponse = await _friendService.GetPendingInvitationsAsync();

            _allFriends.Clear();
            if (friendsResponse != null)
            {
                foreach (var f in friendsResponse)
                {
                    _allFriends.Add(new FriendDisplayModel(f));
                }
            }
            if (pendingResponse != null)
            {
                foreach (var p in pendingResponse)
                {
                    var status = p.IsIncoming ? FriendshipStatus.ReceivedPending : FriendshipStatus.SentPending;
                    _allFriends.Add(new FriendDisplayModel(p, status));
                }
            }

            FriendsCount = _allFriends.Count(f => f.Status == FriendshipStatus.Accepted);
            SentCount = _allFriends.Count(f => f.Status == FriendshipStatus.SentPending);
            PendingCount = _allFriends.Count(f => f.Status == FriendshipStatus.ReceivedPending);

            FilterFriends();
            UpdateTopLists();
            UpdateTotals();
        }
        catch (Exception ex)
        {
            ShowAlert("Błąd", $"Nie udało się załadować listy znajomych: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterFriends()
    {
        Friends.Clear();

        FriendshipStatus targetStatus = ActiveFilter switch
        {
            FriendFilterType.Friends => FriendshipStatus.Accepted,
            FriendFilterType.Sent => FriendshipStatus.SentPending,
            FriendFilterType.Pending => FriendshipStatus.ReceivedPending,
            _ => FriendshipStatus.Accepted
        };

        var filteredByStatus = _allFriends.Where(f => f.Status == targetStatus);

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var lowerQuery = SearchQuery.ToLowerInvariant();
            filteredByStatus = filteredByStatus.Where(f =>
                f.FullName.ToLowerInvariant().Contains(lowerQuery) ||
                f.Nickname!.ToLowerInvariant().Contains(lowerQuery));
        }

        foreach (var friend in filteredByStatus)
        {
            Friends.Add(friend);
        }
    }

    [RelayCommand]
    private void ChangeFilter(string filterTypeStr)
    {
        if (Enum.TryParse<FriendFilterType>(filterTypeStr, out var filterType))
        {
            ActiveFilter = filterType;
        }
    }

    [RelayCommand]
    private void ShowInvitePopup()
    {
        InviteFriendId = string.Empty;
        InviteNickname = string.Empty;
        InviteEmail = string.Empty;
        InvitePhoneNumber = string.Empty;
        SearchedUser = null;
        IsInvitePopupVisible = true;
    }

    [RelayCommand]
    private void CloseInvitePopup()
    {
        _searchCts?.Cancel();
        IsInvitePopupVisible = false;
        SearchedUser = null;
    }

    [RelayCommand]
    private void RemoveFriend(FriendDisplayModel friend)
    {
        if (friend == null) return;
        FriendToDelete = friend;

        if (friend.Status == FriendshipStatus.SentPending)
        {
            DeletePopupTitle = "Wycofanie zaproszenia";
            DeletePopupSubtitle = "Czy na pewno chcesz wycofać to zaproszenie?";
        }
        else if (friend.Status == FriendshipStatus.ReceivedPending)
        {
            DeletePopupTitle = "Odrzucenie zaproszenia";
            DeletePopupSubtitle = "Czy na pewno chcesz odrzucić to zaproszenie?";
        }
        else
        {
            DeletePopupTitle = "Usuwanie znajomego";
            DeletePopupSubtitle = "Czy na pewno chcesz usunąć tego znajomego?";
        }

        IsDeletePopupVisible = true;
    }

    [RelayCommand]
    private void CancelDeleteFriend()
    {
        IsDeletePopupVisible = false;
        FriendToDelete = null;
    }

    [RelayCommand]
    private async Task ConfirmDeleteFriendAsync()
    {
        if (FriendToDelete == null) return;

        var friend = FriendToDelete;
        IsDeletePopupVisible = false;
        FriendToDelete = null;

        try
        {
            IsBusy = true;
            bool success = false;
            if (friend.Status == FriendshipStatus.ReceivedPending)
            {
                success = await _friendService.DeclineInviteAsync(new UpdateInviteRequest { InviteId = friend.InviteId });
            }
            else
            {
                success = await _friendService.RemoveFriendAsync(new UpdateInviteRequest { InviteId = friend.InviteId });
            }

            if (success)
            {
                IsBusy = false;
                await LoadFriendsAsync();
            }
            else
            {
                var msg = friend.Status == FriendshipStatus.SentPending ? "Nie udało się wycofać zaproszenia." :
                          friend.Status == FriendshipStatus.ReceivedPending ? "Nie udało się odrzucić zaproszenia." :
                          "Nie udało się usunąć znajomego.";
                ShowAlert("Błąd", msg);
            }
        }
        catch (Exception ex)
        {
            ShowAlert("Błąd", $"Wystąpił problem: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AcceptInviteAsync(FriendDisplayModel friend)
    {
        if (friend == null) return;

        try
        {
            IsBusy = true;
            var success = await _friendService.AcceptInviteAsync(new UpdateInviteRequest { InviteId = friend.InviteId });
            if (success)
            {
                IsBusy = false;
                await LoadFriendsAsync();
            }
            else
            {
                ShowAlert("Błąd", "Nie udało się zaakceptować zaproszenia.");
            }
        }
        catch (Exception ex)
        {
            ShowAlert("Błąd", $"Wystąpił problem: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToGroupsAsync()
    {
        _signalRService.OnFriendUpdateReceived -= HandleFriendUpdate;
        await Shell.Current.GoToAsync("//GroupsPage");
    }

    [RelayCommand]
    private async Task ConfirmInviteAsync()
    {
        if (SearchedUser == null &&
            string.IsNullOrWhiteSpace(InviteNickname) &&
            string.IsNullOrWhiteSpace(InviteEmail) &&
            string.IsNullOrWhiteSpace(InvitePhoneNumber) &&
            string.IsNullOrWhiteSpace(InviteFriendId))
        {
            ShowAlert("Błąd walidacji", "Wprowadź dane do przynajmniej jednego z pól lub znajdź użytkownika.");
            return;
        }

        try
        {
            IsBusy = true;

            int? targetId = null;
            if (SearchedUser != null)
            {
                targetId = SearchedUser.Id;
            }
            else if (!string.IsNullOrWhiteSpace(InviteFriendId) && int.TryParse(InviteFriendId, out int id))
            {
                targetId = id;
            }

            var request = new FriendInviteRequest
            {
                TargetUserId = targetId,
                Nickname = targetId.HasValue ? null : (string.IsNullOrWhiteSpace(InviteNickname) ? null : InviteNickname.Trim()),
                Email = targetId.HasValue ? null : (string.IsNullOrWhiteSpace(InviteEmail) ? null : InviteEmail.Trim()),
                PhoneNumber = targetId.HasValue ? null : (string.IsNullOrWhiteSpace(InvitePhoneNumber) ? null : InvitePhoneNumber.Trim())
            };

            var success = await _friendService.InviteAsync(request);

            if (success)
            {
                IsInvitePopupVisible = false;
                InviteFriendId = string.Empty;
                InviteNickname = string.Empty;
                InviteEmail = string.Empty;
                InvitePhoneNumber = string.Empty;
                SearchedUser = null;
                IsBusy = false;
                await LoadFriendsAsync();
                ShowAlert("Sukces", "Zaproszenie zostało pomyślnie wysłane.", isError: false);
            }
            else
            {
                ShowAlert("Błąd", "Nie udało się wysłać zaproszenia. Sprawdź poprawność wpisanych danych.");
            }
        }
        catch (Exception ex)
        {
            ShowAlert("Błąd krytyczny", $"Wystąpił problem: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateTopLists()
    {
        TopDebtors.Clear();
        var debtors = _allFriends
            .Where(f => f.Status == FriendshipStatus.Accepted && f.Income > 0)
            .OrderByDescending(f => f.Income)
            .Take(3);
        foreach (var d in debtors) TopDebtors.Add(d);

        TopDebts.Clear();
        var debts = _allFriends
            .Where(f => f.Status == FriendshipStatus.Accepted && f.Expense > 0)
            .OrderByDescending(f => f.Expense)
            .Take(3);
        foreach (var d in debts) TopDebts.Add(d);
    }

    private void UpdateTotals()
    {
        TotalOwed = _allFriends.Where(f => f.Status == FriendshipStatus.Accepted).Sum(f => f.Expense);
        TotalOwedToMe = _allFriends.Where(f => f.Status == FriendshipStatus.Accepted).Sum(f => f.Income);
    }

    private void HandleFriendUpdate()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await LoadFriendsAsync();
        });
    }
}

public enum FriendshipStatus
{
    Accepted,
    SentPending,
    ReceivedPending
}

public enum FriendFilterType
{
    Friends,
    Sent,
    Pending
}

public class FriendDisplayModel
{
    private readonly FriendListResponse? _friend;
    private readonly FriendPendingInvitationResponse? _pending;

    public FriendDisplayModel(FriendListResponse friend)
    {
        _friend = friend;
        Status = FriendshipStatus.Accepted;
    }

    public FriendDisplayModel(FriendPendingInvitationResponse pending, FriendshipStatus status)
    {
        _pending = pending;
        Status = status;
    }

    public int InviteId => _friend?.InviteId ?? _pending?.InvitationId ?? 0;
    public int FriendId => _friend?.FriendId ?? _pending?.FriendId ?? 0;
    public string? PhoneNumber => string.IsNullOrWhiteSpace(_friend?.PhoneNumber) ? null : _friend.PhoneNumber;
    public decimal Balance => _friend?.Balance ?? 0;
    public decimal Income => _friend?.Income ?? 0;
    public decimal Expense => _friend?.Expense ?? 0;

    public string AvatarUrl => string.IsNullOrWhiteSpace(_friend?.AvatarUrl ?? _pending?.AvatarUrl)
        ? "default_user_avatar.png"
        : _friend?.AvatarUrl ?? _pending?.AvatarUrl!;

    public string FullName => _friend != null
        ? $"{_friend.Name} {_friend.Surname}".Trim()
        : $"{_pending?.Name} {_pending?.Surname}".Trim();

    public string? Nickname
    {
        get
        {
            var nick = _friend?.Nickname ?? _pending?.Nickname;
            return string.IsNullOrWhiteSpace(nick) ? null : nick;
        }
    }

    public FriendshipStatus Status { get; set; }

    public bool IsAccepted => Status == FriendshipStatus.Accepted;
    public bool IsSentPending => Status == FriendshipStatus.SentPending;
    public bool IsReceivedPending => Status == FriendshipStatus.ReceivedPending;

    public List<SharedGroupResponse> SharedGroupsList => _friend?.SharedGroups ?? new List<SharedGroupResponse>();
    public bool HasSharedGroups => SharedGroupsList.Count > 0 && IsAccepted;
    public bool HasNickname => !string.IsNullOrWhiteSpace(_friend?.Nickname ?? _pending?.Nickname);
    public bool HasPhoneNumber => !string.IsNullOrWhiteSpace(_friend?.PhoneNumber);

    public List<SharedGroupDisplayItem> SharedGroupsDisplay
    {
        get
        {
            var items = new List<SharedGroupDisplayItem>();
            var groups = _friend?.SharedGroups ?? new List<SharedGroupResponse>();
            if (groups.Count <= 2)
            {
                foreach (var group in groups)
                    items.Add(new SharedGroupDisplayItem { AvatarUrl = group.AvatarUrl, IsMoreIndicator = false });
            }
            else
            {
                for (int i = 0; i < 2; i++)
                    items.Add(new SharedGroupDisplayItem { AvatarUrl = groups[i].AvatarUrl, IsMoreIndicator = false });
                int remaining = groups.Count - 2;
                items.Add(new SharedGroupDisplayItem { IsMoreIndicator = true, MoreText = $"+{remaining}" });
            }
            for (int i = 0; i < items.Count; i++)
                items[i].ZIndex = items.Count - i;
            return items;
        }
    }

    public string BalanceText => _friend == null ? "—" :
                                 _friend.Balance > 0 ? $"Wisi ci {_friend.Balance:N2} zł" :
                                 _friend.Balance < 0 ? $"Wisisz mu {Math.Abs(_friend.Balance):N2} zł" :
                                 "Uregulowano";

    public string BalanceFormatted => _friend == null ? "—" : $"{_friend.Balance:N2} zł";
    public string BalanceAbsoluteFormatted => Status == FriendshipStatus.Accepted ? $"{Math.Abs(_friend?.Balance ?? 0):N2} zł" : "—";
    public string IncomeFormatted => _friend == null ? "—" : $"{_friend.Income:N2} zł";
    public string ExpenseFormatted => _friend == null ? "—" : $"{_friend.Expense:N2} zł";

    public Color BalanceColor => _friend == null ? Color.FromArgb("#9CA3AF") :
                                 _friend.Balance > 0 ? Color.FromArgb("#10B981") :
                                 _friend.Balance < 0 ? Color.FromArgb("#EF4444") :
                                 Color.FromArgb("#9CA3AF");
}

public class SharedGroupDisplayItem
{
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsMoreIndicator { get; set; }
    public string MoreText { get; set; } = string.Empty;
    public int ZIndex { get; set; }
}
