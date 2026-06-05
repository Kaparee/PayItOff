using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Responses;
using System.Collections.ObjectModel;
using System.Globalization;
using PayItOff.Shared.Requests;
using PayItOff.Domain.Enums;

namespace PayItOff.MauiClient.ViewModels;

public partial class TransactionGroup : ObservableCollection<ExpenseSummaryDto>
{
    public string DateHeader { get; }
    public IReadOnlyList<ExpenseSummaryDto> Children { get; }

    public TransactionGroup(string dateHeader, List<ExpenseSummaryDto> expenses) : base(expenses)
    {
        DateHeader = dateHeader;
        Children = expenses;
    }
}

public partial class GroupDetailsViewModel : PopupViewModelBase, IQueryAttributable
{
    private readonly GroupService _groupService;
    private readonly GroupMemberService _groupMemberService;
    private readonly FriendService _friendService;
    private readonly ExpenseService _expenseService;

    private List<GroupMemberBalanceDto> _loadedMembers = new();
    private List<ExpenseSummaryDto> _loadedExpenses = new();
    private int _currentUserId;

    [ObservableProperty]
    private int _groupId;

    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAdminOrFounder))]
    [NotifyPropertyChangedFor(nameof(IsOwner))]
    [NotifyPropertyChangedFor(nameof(RoleSubtitle))]
    public partial string UserRole { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotArchived))]
    public partial bool IsArchived { get; set; }

    public bool IsNotArchived => !IsArchived;

    [ObservableProperty]
    public partial string MemberSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TransactionSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditGroupPopupVisible { get; set; }

    [ObservableProperty]
    public partial string NewGroupName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsManageMembersPopupVisible { get; set; }

    [ObservableProperty]
    public partial string InviteSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsExpenseDetailsPopupVisible { get; set; }

    [ObservableProperty]
    public partial bool IsEditExpensePopupVisible { get; set; }

    [ObservableProperty]
    public partial string EditExpenseName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditExpenseCategory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ExpenseDetailsResponse? SelectedExpenseDetails { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingAmountToSplit))]
    public partial decimal EditExpenseTotalAmount { get; set; }

    public ObservableCollection<EditableExpenseSplit> EditExpenseSplits { get; } = new();

    public decimal RemainingAmountToSplit => EditExpenseTotalAmount - EditExpenseSplits.Sum(s => s.OwedAmount);

    public ObservableCollection<GroupMemberResponse> ActiveMembersList { get; } = new();

    public ObservableCollection<FriendListResponse> FriendsList { get; } = new();
    public ObservableCollection<GroupMemberBalanceDto> FilteredMembers { get; } = new();
    public ObservableCollection<TransactionGroup> TransactionSections { get; } = new();


    public bool IsAdminOrFounder => UserRole is "Owner" or "Admin";
    public bool IsOwner => UserRole == "Owner";

    public string RoleSubtitle => UserRole switch
    {
        "Owner" => "Jesteś właścicielem!",
        "Admin" => "Jesteś administratorem grupy.",
        _ => "Jesteś członkiem grupy."
    };

    public GroupDetailsViewModel(GroupService groupService, GroupMemberService groupMemberService, FriendService friendService, ExpenseService expenseService)
    {
        _groupService = groupService;
        _groupMemberService = groupMemberService;
        _friendService = friendService;
        _expenseService = expenseService;
        IsCustomAlertSupported = true;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("groupId", out var raw)) return;

        var id = raw switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) => n,
            _ => 0
        };

        if (id > 0)
            GroupId = id;
    }

    partial void OnGroupIdChanged(int value)
    {
        if (value > 0)
            _ = LoadDataAsync();
    }

    partial void OnMemberSearchTextChanged(string value) => ApplyMemberFilter();

    partial void OnTransactionSearchTextChanged(string value) => ApplyTransactionFilter();

    [RelayCommand]
    private async Task GoBackAsync()
    {
        if (IsArchived)
        {
            await Shell.Current.GoToAsync("//ArchivePage");
        }
        else
        {
            await Shell.Current.GoToAsync("//GroupsPage");
        }
    }

    [RelayCommand]
    private async Task AddExpenseAsync() =>
        await Shell.Current.GoToAsync($"{nameof(Views.AddExpensePage)}?groupId={GroupId}");

    [RelayCommand]
    private Task ManageSettlementAsync() => ShowAlertAsync("Info", "Zarządzanie rozliczeniem — wkrótce.", "OK");

    [RelayCommand]
    private Task ToggleTransactionViewAsync() => ShowAlertAsync("Info", "Zmiana widoku — wkrótce.", "OK");

    [RelayCommand]
    private void ShowEditGroupPopup()
    {
        NewGroupName = GroupName;
        IsEditGroupPopupVisible = true;
    }

    [RelayCommand]
    private void CancelEditGroupPopup() => IsEditGroupPopupVisible = false;

    [RelayCommand]
    private async Task ShowExpenseDetailsAsync(ExpenseSummaryDto expense)
    {
        if (expense == null) return;

        IsBusy = true;
        try
        {
            var details = await _expenseService.GetExpenseItemDetailsAsync(expense.ExpenseId, expense.ItemId);
            SelectedExpenseDetails = details;
            IsExpenseDetailsPopupVisible = true;
        }
        catch (Exception)
        {
            await ShowAlertAsync("Błąd", "Nie udało się pobrać szczegółów wydatku.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CloseExpenseDetailsPopup()
    {
        IsExpenseDetailsPopupVisible = false;
    }

    [RelayCommand]
    private void ShowEditExpensePopup()
    {
        if (SelectedExpenseDetails == null) return;
        EditExpenseName = SelectedExpenseDetails.Title;
        EditExpenseCategory = SelectedExpenseDetails.Category;
        EditExpenseTotalAmount = SelectedExpenseDetails.TotalAmount;
        
        foreach(var old in EditExpenseSplits) old.PropertyChanged -= OnSplitPropertyChanged;
        EditExpenseSplits.Clear();
        foreach (var p in SelectedExpenseDetails.Participants)
        {
            var split = new EditableExpenseSplit
            {
                UserId = p.UserId,
                FullName = p.FullName,
                AvatarUrl = p.AvatarUrl,
                OwedAmount = p.OwedAmount
            };
            split.PropertyChanged += OnSplitPropertyChanged;
            EditExpenseSplits.Add(split);
        }
        
        OnPropertyChanged(nameof(RemainingAmountToSplit));
        IsEditExpensePopupVisible = true;
    }

    private void OnSplitPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditableExpenseSplit.OwedAmount))
        {
            OnPropertyChanged(nameof(RemainingAmountToSplit));
        }
    }

    [RelayCommand]
    private void CancelEditExpensePopup()
    {
        IsEditExpensePopupVisible = false;
    }

    [RelayCommand]
    private async Task ConfirmEditExpenseAsync()
    {
        if (SelectedExpenseDetails == null || string.IsNullOrWhiteSpace(EditExpenseName) || string.IsNullOrWhiteSpace(EditExpenseCategory))
            return;

        if (Math.Abs(RemainingAmountToSplit) > 0.01m)
        {
            await ShowAlertAsync("Błąd", "Suma podziałów musi być równa kwocie całkowitej.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var splits = EditExpenseSplits.Select(s => new PayItOff.Shared.Requests.ExpenseSplitDto
            {
                UserId = s.UserId,
                Amount = s.OwedAmount
            }).ToList();

            var request = new PayItOff.Shared.Requests.UpdateExpenseItemRequest
            {
                Name = EditExpenseName.Trim(),
                Category = EditExpenseCategory.Trim(),
                TotalPrice = EditExpenseTotalAmount,
                Splits = splits
            };
            await _expenseService.UpdateExpenseItemAsync(SelectedExpenseDetails.ExpenseId, SelectedExpenseDetails.ItemId, request);
            
            IsEditExpensePopupVisible = false;
            IsExpenseDetailsPopupVisible = false;
            await LoadDataAsync();
        }
        catch (Exception)
        {
            await ShowAlertAsync("Błąd", "Nie udało się edytować wydatku.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmEditGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGroupName)) return;

        IsBusy = true;
        IsEditGroupPopupVisible = false;

        var request = new EditGroupInfoRequest { GroupId = GroupId, NewName = NewGroupName.Trim() };
        var success = await _groupService.EditGroupInfoAsync(request);

        if (success)
        {
            GroupName = request.NewName;
            await LoadDataAsync();
        }
        else
        {
            await ShowAlertAsync("Błąd", "Nie udało się zmienić nazwy grupy.", "OK");
        }
        IsBusy = false;
    }

    [RelayCommand]
    private async Task DeleteGroupAsync()
    {
        bool confirm = await ShowAlertAsync("Usuń Grupę", "Czy na pewno chcesz usunąć tę grupę? Tej operacji nie można cofnąć.", "Tak, usuń", "Anuluj");
        if (!confirm) return;

        IsBusy = true;
        var success = await _groupService.DeleteGroupAsync(GroupId);
        if (success)
        {
            await Shell.Current.GoToAsync("//GroupsPage");
        }
        else
        {
            await ShowAlertAsync("Błąd", "Nie udało się usunąć grupy.", "OK");
        }
        IsBusy = false;
    }

    [RelayCommand]
    private async Task LeaveGroupAsync()
    {
        bool confirm = await ShowAlertAsync("Opuść Grupę", "Czy na pewno chcesz opuścić tę grupę?", "Tak, opuść", "Anuluj");
        if (!confirm) return;

        IsBusy = true;
        var success = await _groupMemberService.LeaveGroupAsync(GroupId);
        if (success)
        {
            await Shell.Current.GoToAsync("//GroupsPage");
        }
        else
        {
            await ShowAlertAsync("Błąd", "Nie udało się opuścić grupy.", "OK");
        }
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ShowManageMembersPopupAsync()
    {
        IsBusy = true;

        var friendsTask = _friendService.GetUserFriendListAsync();
        var membersTask = _groupMemberService.GetAllActiveGroupMembersAsync(GroupId);

        await Task.WhenAll(friendsTask, membersTask);

        FriendsList.Clear();
        if (friendsTask.Result != null)
        {
            foreach (var friend in friendsTask.Result)
            {
                if (!FilteredMembers.Any(m => m.UserId == friend.FriendId))
                {
                    FriendsList.Add(friend);
                }
            }
        }

        ActiveMembersList.Clear();
        if (membersTask.Result != null)
        {
            foreach (var member in membersTask.Result)
            {
                ActiveMembersList.Add(member);
            }
        }

        IsBusy = false;
        IsManageMembersPopupVisible = true;
    }

    [RelayCommand]
    private void CancelManageMembersPopup() => IsManageMembersPopupVisible = false;

    [RelayCommand]
    private async Task InviteUserBySearchAsync()
    {
        if (string.IsNullOrWhiteSpace(InviteSearchText)) return;

        IsBusy = true;
        var isEmail = InviteSearchText.Contains('@');
        var searchResponse = await _friendService.SearchUserAsync(
            isEmail ? null : InviteSearchText.Trim(),
            isEmail ? InviteSearchText.Trim() : null,
            null);

        if (searchResponse == null)
        {
            IsBusy = false;
            await ShowAlertAsync("Błąd", "Nie znaleziono użytkownika o podanych danych.", "OK");
            return;
        }

        await ExecuteInviteAsync(searchResponse.Id);
    }

    [RelayCommand]
    private async Task InviteFriendAsync(FriendListResponse friend)
    {
        if (friend == null) return;
        IsBusy = true;
        await ExecuteInviteAsync(friend.FriendId);
    }

    private async Task ExecuteInviteAsync(int targetUserId)
    {
        var request = new GroupInviteUserRequest
        {
            GroupId = GroupId,
            UserId = targetUserId,
            Role = GroupMemberRole.Member
        };

        var (success, errorMsg) = await _groupMemberService.InviteUserAsync(request);
        if (success)
        {
            InviteSearchText = string.Empty;
            await ShowAlertAsync("Sukces", "Wysłano zaproszenie.", "OK");

            var invitedFriend = FriendsList.FirstOrDefault(f => f.FriendId == targetUserId);
            if (invitedFriend != null)
            {
                FriendsList.Remove(invitedFriend);
            }
        }
        else
        {
            await ShowAlertAsync("Błąd", errorMsg, "OK");
        }
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ChangeMemberRoleAsync(GroupMemberResponse member)
    {
        if (UserRole != "Owner") return;
        if (member.UserId == _currentUserId) return;

        var newRole = member.Role == GroupMemberRole.Admin ? GroupMemberRole.Member : GroupMemberRole.Admin;

        bool confirm = await ShowAlertAsync("Zmień rolę", $"Czy chcesz zmienić rolę {member.FullName} na {newRole}?", "Tak", "Anuluj");
        if (!confirm) return;

        IsBusy = true;
        var success = await _groupMemberService.UpdateRoleAsync(new GroupMemberUpdateRequest { GroupId = GroupId, TargetUserId = member.UserId, NewRole = newRole });
        if (success)
        {
            member.Role = newRole;
            var membersTask = await _groupMemberService.GetAllActiveGroupMembersAsync(GroupId);
            if (membersTask != null)
            {
                ActiveMembersList.Clear();
                foreach (var m in membersTask) ActiveMembersList.Add(m);
            }
        }
        IsBusy = false;
    }

    [RelayCommand]
    private async Task KickMemberAsync(GroupMemberResponse member)
    {
        if (!IsAdminOrFounder) return;
        if (member.UserId == _currentUserId) return;

        bool confirm = await ShowAlertAsync("Wyrzuć członka", $"Czy na pewno chcesz wyrzucić {member.FullName} z grupy?", "Tak", "Anuluj");
        if (!confirm) return;

        IsBusy = true;
        var success = await _groupMemberService.KickUserFromGroupAsync(GroupId, member.UserId);
        if (success)
        {
            ActiveMembersList.Remove(member);
            await LoadDataAsync();
        }
        IsBusy = false;
    }

    public async Task LoadDataAsync()
    {
        if (GroupId <= 0) return;

        if (_currentUserId == 0)
        {
            var idStr = await SecureStorage.Default.GetAsync("user_id");
            _currentUserId = int.TryParse(idStr, out var parsed) ? parsed : 0;
        }

        IsBusy = true;
        try
        {
            var response = await _groupService.GetGroupDetails(GroupId);
            if (response == null)
            {
                await ShowAlertAsync("Błąd", "Nie udało się wczytać grupy.", "OK");
                return;
            }

            GroupName = response.GroupName ?? string.Empty;
            UserRole = response.UserRole ?? string.Empty;
            IsArchived = response.IsArchived;
            OnPropertyChanged(nameof(IsAdminOrFounder));
            OnPropertyChanged(nameof(RoleSubtitle));
            OnPropertyChanged(nameof(IsNotArchived));

            _loadedMembers = response.Members ?? new List<GroupMemberBalanceDto>();
            _loadedExpenses = response.Expenses ?? new List<ExpenseSummaryDto>();

            ApplyMemberFilter();
            ApplyTransactionFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyMemberFilter()
    {
        var q = (MemberSearchText ?? string.Empty).Trim().ToLowerInvariant();
        IEnumerable<GroupMemberBalanceDto> src = _loadedMembers;
        if (q.Length > 0)
            src = src.Where(m => m.FullName.ToLowerInvariant().Contains(q));

        FilteredMembers.Clear();
        foreach (var m in src)
            FilteredMembers.Add(m);
    }

    private void ApplyTransactionFilter()
    {
        var q = (TransactionSearchText ?? string.Empty).Trim().ToLowerInvariant();
        IEnumerable<ExpenseSummaryDto> src = _loadedExpenses;
        if (q.Length > 0)
        {
            src = src.Where(e =>
                ((e.Title ?? string.Empty).ToLowerInvariant().Contains(q)
                || (e.PayerName ?? string.Empty).ToLowerInvariant().Contains(q)));
        }


        src = src.Where(e => e.TotalAmount > 0);

        var grouped = src
            .GroupBy(e => e.Date.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new TransactionGroup(FormatDate(g.Key), g.ToList()))
            .ToList();

        TransactionSections.Clear();
        foreach (var g in grouped)
            TransactionSections.Add(g);
    }

    private static string FormatDate(DateTime date)
    {
        if (date.Date == DateTime.Today) return "Dzisiaj";
        if (date.Date == DateTime.Today.AddDays(-1)) return "Wczoraj";
        return date.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("pl-PL"));
    }

    [RelayCommand]
    private async Task CopyToClipboardAsync(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            await Clipboard.Default.SetTextAsync(text);
        }
    }
}
