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

public partial class GroupDetailsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly GroupService _groupService;
    private readonly GroupMemberService _groupMemberService;
    private readonly FriendService _friendService;
    private readonly ExpenseService _expenseService;

    private List<GroupMemberBalanceDto> _loadedMembers = new();
    private List<ExpenseSummaryDto> _loadedExpenses = new();

    [ObservableProperty]
    private int _groupId;

    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAdminOrFounder))]
    [NotifyPropertyChangedFor(nameof(IsOwner))]
    [NotifyPropertyChangedFor(nameof(RoleSubtitle))]
    private string _userRole = string.Empty;

    [ObservableProperty]
    private string _memberSearchText = string.Empty;

    [ObservableProperty]
    private string _transactionSearchText = string.Empty;

    [ObservableProperty]
    private bool _isEditGroupPopupVisible;

    [ObservableProperty]
    private string _newGroupName = string.Empty;

    [ObservableProperty]
    private bool _isManageMembersPopupVisible;

    [ObservableProperty]
    private string _inviteSearchText = string.Empty;

    [ObservableProperty]
    private bool _isExpenseDetailsPopupVisible;

    [ObservableProperty]
    private ExpenseDetailsResponse? _selectedExpenseDetails;

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
    private async Task GoBackAsync() => await Shell.Current.GoToAsync("//GroupsPage");

    [RelayCommand]
    private async Task AddExpenseAsync() =>
        await Shell.Current.GoToAsync($"{nameof(Views.AddExpensePage)}?groupId={GroupId}");

    [RelayCommand]
    private Task ManageSettlementAsync() => Shell.Current.DisplayAlertAsync("Info", "Zarządzanie rozliczeniem — wkrótce.", "OK");

    [RelayCommand]
    private Task ToggleTransactionViewAsync() => Shell.Current.DisplayAlertAsync("Info", "Zmiana widoku — wkrótce.", "OK");

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
            var details = await _expenseService.GetExpenseDetailsAsync(expense.ExpenseId);
            SelectedExpenseDetails = details;
            IsExpenseDetailsPopupVisible = true;
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Nie udało się pobrać szczegółów wydatku.", "OK");
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
        SelectedExpenseDetails = null;
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
            await Shell.Current.DisplayAlertAsync("Błąd", "Nie udało się zmienić nazwy grupy.", "OK");
        }
        IsBusy = false;
    }

    [RelayCommand]
    private async Task DeleteGroupAsync()
    {
        bool confirm = await Shell.Current.DisplayAlertAsync("Usuń Grupę", "Czy na pewno chcesz usunąć tę grupę? Tej operacji nie można cofnąć.", "Tak, usuń", "Anuluj");
        if (!confirm) return;

        IsBusy = true;
        var success = await _groupService.DeleteGroupAsync(GroupId);
        if (success)
        {
            await Shell.Current.GoToAsync("//GroupsPage");
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Nie udało się usunąć grupy.", "OK");
        }
        IsBusy = false;
    }

    [RelayCommand]
    private async Task LeaveGroupAsync()
    {
        bool confirm = await Shell.Current.DisplayAlertAsync("Opuść Grupę", "Czy na pewno chcesz opuścić tę grupę?", "Tak, opuść", "Anuluj");
        if (!confirm) return;

        IsBusy = true;
        var success = await _groupMemberService.LeaveGroupAsync(GroupId);
        if (success)
        {
            await Shell.Current.GoToAsync("//GroupsPage");
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Nie udało się opuścić grupy.", "OK");
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
            await Shell.Current.DisplayAlertAsync("Błąd", "Nie znaleziono użytkownika o podanych danych.", "OK");
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
            await Shell.Current.DisplayAlertAsync("Sukces", "Wysłano zaproszenie.", "OK");
            
            var invitedFriend = FriendsList.FirstOrDefault(f => f.FriendId == targetUserId);
            if (invitedFriend != null)
            {
                FriendsList.Remove(invitedFriend);
            }
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Błąd", errorMsg, "OK");
        }
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ChangeMemberRoleAsync(GroupMemberResponse member)
    {
        if (UserRole != "Owner") return;
        if (member.UserId == int.Parse(SecureStorage.Default.GetAsync("user_id").Result ?? "0")) return;
        
        var newRole = member.Role == GroupMemberRole.Admin ? GroupMemberRole.Member : GroupMemberRole.Admin;
        
        bool confirm = await Shell.Current.DisplayAlertAsync("Zmień rolę", $"Czy chcesz zmienić rolę {member.FullName} na {newRole}?", "Tak", "Anuluj");
        if (!confirm) return;
        
        IsBusy = true;
        var success = await _groupMemberService.UpdateRoleAsync(new GroupMemberUpdateRequest { GroupId = GroupId, TargetUserId = member.UserId, NewRole = newRole });
        if (success) 
        {
            member.Role = newRole;
            // Force property change to update UI if it was ObservableObject, but it's not. Re-fetching active members is safer.
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
        if (member.UserId == int.Parse(SecureStorage.Default.GetAsync("user_id").Result ?? "0")) return;
        
        bool confirm = await Shell.Current.DisplayAlertAsync("Wyrzuć członka", $"Czy na pewno chcesz wyrzucić {member.FullName} z grupy?", "Tak", "Anuluj");
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

    private async Task LoadDataAsync()
    {
        if (GroupId <= 0) return;

        IsBusy = true;
        try
        {
            var response = await _groupService.GetGroupDetails(GroupId);
            if (response == null)
            {
                await Shell.Current.DisplayAlertAsync("Błąd", "Nie udało się wczytać grupy.", "OK");
                return;
            }

            GroupName = response.GroupName ?? string.Empty;
            UserRole = response.UserRole ?? string.Empty;
            OnPropertyChanged(nameof(IsAdminOrFounder));
            OnPropertyChanged(nameof(RoleSubtitle));

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
}
