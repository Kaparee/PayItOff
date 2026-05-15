using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Responses;
using System.Collections.ObjectModel;
using System.Globalization;

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
    private List<GroupMemberBalanceDto> _loadedMembers = new();
    private List<ExpenseSummaryDto> _loadedExpenses = new();

    [ObservableProperty]
    private int _groupId;

    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private string _userRole = string.Empty;

    [ObservableProperty]
    private string _memberSearchText = string.Empty;

    [ObservableProperty]
    private string _transactionSearchText = string.Empty;

    public ObservableCollection<GroupMemberBalanceDto> FilteredMembers { get; } = new();
    public ObservableCollection<TransactionGroup> TransactionSections { get; } = new();

    public bool IsAdminOrFounder => UserRole is "Owner" or "Admin";

    public string RoleSubtitle => UserRole switch
    {
        "Owner" => "Jesteś właścicielem!",
        "Admin" => "Jesteś administratorem grupy.",
        _ => "Jesteś członkiem grupy."
    };

    public GroupDetailsViewModel(GroupService groupService)
    {
        _groupService = groupService;
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
    private Task AddExpenseAsync() =>
        Shell.Current.DisplayAlertAsync("Info", "Dodawanie wydatków z tego widoku — użyj listy wydatków w grupie (wkrótce).", "OK");

    [RelayCommand]
    private Task ManageSettlementAsync() => Shell.Current.DisplayAlertAsync("Info", "Zarządzanie rozliczeniem — wkrótce.", "OK");

    [RelayCommand]
    private Task ManageMembersAsync() => Shell.Current.DisplayAlertAsync("Info", "Zarządzanie członkami — wkrótce.", "OK");

    [RelayCommand]
    private Task ToggleTransactionViewAsync() => Shell.Current.DisplayAlertAsync("Info", "Zmiana widoku — wkrótce.", "OK");

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

        // Tylko wydatki (kwoty dodatnie)
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
