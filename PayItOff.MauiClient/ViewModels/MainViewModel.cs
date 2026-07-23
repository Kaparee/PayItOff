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
    public required string Number { get; set; }
    public string DisplayNumber => string.IsNullOrWhiteSpace(Number) ? "Brak numeru" : Number;
    public required string CategoriesDisplay { get; set; }
    public required DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public partial class MainViewModel : BaseViewModel
{
    private readonly SettlementService _settlementService;
    private readonly GroupService _groupService;
    private readonly NotificationService _notificationService;
    private readonly SignalRService _signalRService;

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

    public MainViewModel(
        SettlementService settlementService,
        GroupService groupService,
        NotificationService notificationService,
        SignalRService signalRService
        )
    {
        _settlementService = settlementService;
        _groupService = groupService;
        _notificationService = notificationService;
        _signalRService = signalRService;
    }

    public void SubscribeToEvents()
    {
        _signalRService.OnSettlementUpdateReceived += HandleSettlementUpdate;
        _signalRService.OnSystemNotificationEventReceived += HandleNotificationEvent;
    }

    public void UnsubscribeFromEvents()
    {
        _signalRService.OnSettlementUpdateReceived -= HandleSettlementUpdate;
        _signalRService.OnSystemNotificationEventReceived -= HandleNotificationEvent;
    }

    public void OnDisappearing()
    {
        UnsubscribeFromEvents();
    }

    public async Task LoadDashboardDataAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            if (_signalRService.IsDisconnected) { await _signalRService.StartAsync(); }

            var incTask = _settlementService.GetUserAllIncomesSummaryAsync();
            var expTask = _settlementService.GetUserAllExpensesSummaryAsync();
            var groupsTask = _groupService.Get4ActiveGroups();
            var notifTask = _notificationService.Get5LastNotification();

            await Task.WhenAll(incTask, expTask, groupsTask, notifTask);

            var incResult = await incTask ?? new GlobalSettlementResponse();
            var expResult = await expTask ?? new GlobalSettlementResponse();
            var groupsResult = await groupsTask;
            var notificationsResult = await notifTask;

            Incomes.Clear();
            foreach (var i in incResult.Items)
            {
                Incomes.Add(new DebtDisplayItem
                {
                    UserId = i.UserId,
                    FullName = $"{i.Name} {i.Surname}",
                    AvatarUrl = i.AvatarUrl,
                    Number = i.Number,
                    Amount = i.Amount,
                    Date = i.Date,
                    CategoriesDisplay = i.Categories != null ? string.Join(", ", i.Categories) : "Brak"
                });
            }

            TotalIncomes = incResult.TotalAmount;

            Expenses.Clear();
            foreach (var e in expResult.Items)
            {
                Expenses.Add(new DebtDisplayItem
                {
                    UserId = e.UserId,
                    FullName = $"{e.Name} {e.Surname}",
                    AvatarUrl = e.AvatarUrl,
                    Number = e.Number,
                    Amount = e.Amount,
                    Date = e.Date,
                    CategoriesDisplay = e.Categories != null ? string.Join(", ", e.Categories) : "Brak"
                });
            }

            TotalExpenses = expResult.TotalAmount;

            ActiveGroups.Clear();
            if (groupsResult != null)
            {
                foreach (var g in groupsResult)
                {
                    g.AvatarUrl = g.AvatarUrl;
                    ActiveGroups.Add(g);
                }
            }

            LastNotifications.Clear();
            if (notificationsResult != null)
            {
                foreach (var n in notificationsResult) LastNotifications.Add(NotificationDisplayItem.FromResponse(n));
            }
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

    [RelayCommand]
    private async Task NavigateToNotifications()
    {
        await Shell.Current.GoToAsync("//NotificationsPage");
    }

    private void HandleSettlementUpdate()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await LoadDashboardDataAsync();
        });
    }

    private void HandleNotificationEvent()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await LoadDashboardDataAsync();
        });
    }

}
