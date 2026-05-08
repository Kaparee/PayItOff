using CommunityToolkit.Mvvm.ComponentModel;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Responses;
using System.Collections.ObjectModel;

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

    [ObservableProperty]
    public partial ObservableCollection<DebtDisplayItem> Incomes { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<DebtDisplayItem> Expenses { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<ActiveGroupsDisplayResponse> ActiveGroups { get; set; } = new();

    [ObservableProperty]
    public partial decimal TotalIncomes { get; set; }

    [ObservableProperty]
    public partial decimal TotalExpenses { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }
    [ObservableProperty]
    public required partial string DebugStatus { get; set; }

    [ObservableProperty]
    public required partial string DebugColor { get; set; }

    [ObservableProperty]
    public required partial string CurrentUserEmail { get; set; }

    public MainViewModel(SettlementService settlementService, GroupService groupService)
    {
        _settlementService = settlementService;
        _groupService = groupService;
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

            var incTask = _settlementService.GetIncomesAsync();
            var expTask = _settlementService.GetExpensesAsync();
            var groupsTask = _groupService.Get4ActiveGroups();

            await Task.WhenAll(incTask, expTask, groupsTask);

            var incResult = await incTask;
            var expResult = await expTask;
            var groupsResult = await groupsTask;

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
}