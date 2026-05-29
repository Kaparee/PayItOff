using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Models;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Responses;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PayItOff.MauiClient.ViewModels;

public class DebtDisplayItem
{
    public int UserId { get; set; }
    public required string FullName { get; set; }
    public required string AvatarUrl { get; set; }
    public required string CategoriesDisplay { get; set; }
    public required DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public partial class MainViewModel : BaseViewModel
{
    private readonly SettlementService _settlementService;
    private readonly GroupService _groupService;
    private readonly NotificationService _notificationService;

    [ObservableProperty]
    public partial ObservableCollection<DebtDisplayItem> Incomes { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<DebtDisplayItem> Expenses { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<ActiveGroupsDisplayResponse> ActiveGroups { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<NotificationDisplayItem> LastNotifications { get; set; } = new();

    [ObservableProperty]
    public partial decimal TotalIncomes { get; set; }

    [ObservableProperty]
    public partial decimal TotalExpenses { get; set; }

    [ObservableProperty]
    public required partial string DebugStatus { get; set; }

    [ObservableProperty]
    public required partial string DebugColor { get; set; }

    [ObservableProperty]
    public required partial string CurrentUserEmail { get; set; }

    public MainViewModel(SettlementService settlementService, GroupService groupService, NotificationService notificationService)
    {
        _settlementService = settlementService;
        _groupService = groupService;
        _notificationService = notificationService;
    }

    public async Task LoadDashboardDataAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var token = await SecureStorage.Default.GetAsync("jwt_token");
            DebugStatus = string.IsNullOrEmpty(token) ? "Brak Tokena" : "Token OK";
            DebugColor = string.IsNullOrEmpty(token) ? "#FF4500" : "#00FF7F";
            CurrentUserEmail = string.IsNullOrEmpty(token) ? "Niezalogowany" : "Zalogowano pomyślnie";

            var incTask = _settlementService.GetUserAllIncomesSummaryAsync();
            var expTask = _settlementService.GetUserAllExpensesSummaryAsync();
            var groupsTask = _groupService.Get4ActiveGroups();
            var notifTask = _notificationService.Get5LastNotification();

            await Task.WhenAll(incTask, expTask, groupsTask, notifTask);

            var incResult = await incTask ?? new GlobalSettlementResponse();
            var expResult = await expTask ?? new GlobalSettlementResponse();
            var groupsResult = await groupsTask;
            var notificationsResult = await notifTask;

            Incomes = new ObservableCollection<DebtDisplayItem>(incResult.Items.Select(i => new DebtDisplayItem
            {
                UserId = i.UserId,
                FullName = $"{i.Name} {i.Surname}",
                AvatarUrl = i.AvatarUrl,
                Amount = i.Amount,
                Date = i.Date,
                CategoriesDisplay = i.Categories != null ? string.Join(", ", i.Categories) : "Brak"
            }));

            TotalIncomes = incResult.TotalAmount;

            Expenses = new ObservableCollection<DebtDisplayItem>(expResult.Items.Select(e => new DebtDisplayItem
            {
                UserId = e.UserId,
                FullName = $"{e.Name} {e.Surname}",
                AvatarUrl = e.AvatarUrl,
                Amount = e.Amount,
                Date = e.Date,
                CategoriesDisplay = e.Categories != null ? string.Join(", ", e.Categories) : "Brak"
            }));

            TotalExpenses = expResult.TotalAmount;

            ActiveGroups = new ObservableCollection<ActiveGroupsDisplayResponse>(groupsResult);

            LastNotifications = new ObservableCollection<NotificationDisplayItem>(
                (notificationsResult ?? Enumerable.Empty<NotificationResponse>()).Select(NotificationDisplayItem.FromResponse)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard Load Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToWallet(string filter)
    {
        IsBusy = true;
        await Task.Delay(50);

        try
        {
            await Shell.Current.GoToAsync($"//WalletPage?filter={filter}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToPerson(DebtDisplayItem item)
    {
        if (item == null) return;

        string filter = Incomes.Contains(item) ? "Income" : "Expense";

        IsBusy = true;
        await Task.Delay(50);
        try
        {
            await Shell.Current.GoToAsync($"//WalletPage?targetId={item.UserId}&filter={filter}");
        }
        finally { IsBusy = false; }
    }

    public ICommand NavigateToGroupDetailsCommand => new Command<ActiveGroupsDisplayResponse>(async (group) =>
    {
        if (group == null) return;
        await Shell.Current.GoToAsync($"//GroupDetailsPage?groupId={group.Id}");
    });
}