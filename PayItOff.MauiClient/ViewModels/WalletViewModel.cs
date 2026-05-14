using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;

namespace PayItOff.MauiClient.ViewModels;

public partial class WalletPersonUiModel : ObservableObject
{
    public int ExpenseId { get; set; }
    public int OtherUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string CategoriesDisplay { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public bool IsIncome { get; set; }

    public bool IsSettlement { get; set; }
    public string Status { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public string SettlementBorderColor { get; set; } = string.Empty;
    public bool CanSendDebtReminder { get; set; }

    public bool IsPending => Status == "Pending";
    public bool ShowAcceptRejectButtons => IsSettlement && IsIncome && IsPending;
    public bool ShowNormalAmount => !ShowAcceptRejectButtons;

    public string BorderStrokeColor =>
        IsSettlement && !string.IsNullOrEmpty(SettlementBorderColor)
            ? SettlementBorderColor
            : "Transparent";

    public int BorderStrokeThickness =>
        IsSettlement && !string.IsNullOrEmpty(SettlementBorderColor) ? 3 : 0;

    public string StatusColor => IsIncome ? "#00FF7F" : "#FF4500";
    public bool ShowRemindButton => !IsSettlement && IsIncome && CanSendDebtReminder;

    public bool ShowSettlementStatus => IsSettlement;
    public string SettlementStatusHint => IsSettlement
        ? (IsPending
            ? (IsIncome ? "Oczekuje Twojej decyzji" : "Czeka na akceptację wierzyciela")
            : Status switch
            {
                "Confirmed" => "Potwierdzona",
                "Rejected" => "Odrzucona",
                _ => Status
            })
        : string.Empty;
}

[QueryProperty(nameof(FilterType), "filter")]
[QueryProperty(nameof(TargetIdFilter), "targetId")]
public partial class WalletViewModel : BaseViewModel
{
    private readonly SettlementService _settlementService;
    private int _loadId = 0;

    [ObservableProperty]
    public partial string? FilterType { get; set; }

    [ObservableProperty]
    public partial string? TargetIdFilter { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<WalletPersonUiModel> Transactions { get; set; } = new();

    [ObservableProperty]
    public partial int TotalTransactionsCount { get; set; }

    [ObservableProperty]
    public partial int IncomesCount { get; set; }

    [ObservableProperty]
    public partial int ExpensesCount { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    private List<WalletPersonUiModel> _allTransactions = new();

    [ObservableProperty]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalPages { get; set; } = 1;

    private CancellationTokenSource? _searchCts;

    [ObservableProperty]
    public partial bool IsPaymentOverlayVisible { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<PayableDebtOptionResponse> PayableDebtOptions { get; set; } = new();

    [ObservableProperty]
    public partial PayableDebtOptionResponse? SelectedPayableDebtOption { get; set; }

    [ObservableProperty]
    public partial string PaymentAmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PaymentFormHint { get; set; } = string.Empty;

    public WalletViewModel(SettlementService settlementService)
    {
        _settlementService = settlementService;
        FilterType = "All";
    }

    [RelayCommand]
    private void ChangeFilter(string filter)
    {
        if (FilterType == filter) return;
        FilterType = filter;
    }

    partial void OnFilterTypeChanged(string? value)
    {
        CurrentPage = 1;
        MainThread.BeginInvokeOnMainThread(async () => await LoadDataAsync());
    }

    partial void OnTargetIdFilterChanged(string? value)
    {
        CurrentPage = 1;
        MainThread.BeginInvokeOnMainThread(async () => await LoadDataAsync());
    }

    public async Task LoadDataAsync()
    {
        int currentLoadId = Interlocked.Increment(ref _loadId);

        IsBusy = true;

        try
        {
            string apiType = FilterType switch
            {
                "Income" => "Income",
                "Incomes" => "Income",
                "Expense" => "Expense",
                "Expenses" => "Expense",
                _ => "All"
            };

            int? targetId = int.TryParse(TargetIdFilter, out var id) ? id : null;

            var response = await _settlementService.GetHistoryAsync(CurrentPage, apiType, targetId);

            if (currentLoadId != _loadId) return;

            if (response != null && response.Items != null)
            {
                TotalPages = response.TotalPages > 0 ? response.TotalPages : 1;

                var newList = response.Items.Select(item => new WalletPersonUiModel
                {
                    ExpenseId = item.ExpenseId,
                    OtherUserId = item.OtherUserId,
                    FullName = $"{item.OtherName} {item.OtherSurname}",
                    AvatarUrl = item.OtherAvatarUrl!,
                    CategoriesDisplay = item.Categories != null && item.Categories.Any() ? string.Join(", ", item.Categories) : "",
                    Description = item.GroupName,
                    Amount = item.Amount,
                    Date = item.Date,
                    IsIncome = !item.AmIDebtor,
                    IsSettlement = item.IsSettlement,
                    Status = item.Status,
                    GroupId = item.GroupId,
                    SettlementBorderColor = item.SettlementBorderColor,
                    CanSendDebtReminder = item.CanSendDebtReminder
                }).ToList();

                _allTransactions = newList;
                ApplySearchFilter();

                TotalTransactionsCount = response.TotalTransactionsCount;
                IncomesCount = response.TotalIncomesCount;
                ExpensesCount = response.TotalExpensesCount;
            }
            else
            {
                Transactions.Clear();
                _allTransactions.Clear();
                TotalTransactionsCount = 0;
                IncomesCount = 0;
                ExpensesCount = 0;
            }
        }
        catch (Exception ex)
        {
            if (currentLoadId == _loadId)
                await Shell.Current.DisplayAlertAsync("Błąd", ex.Message, "OK");
        }
        finally
        {
            if (currentLoadId == _loadId)
                IsBusy = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                if (token.IsCancellationRequested) return;

                MainThread.BeginInvokeOnMainThread(() => ApplySearchFilter());
            }
            catch (TaskCanceledException) { }
        });
    }

    private void ApplySearchFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Transactions = new ObservableCollection<WalletPersonUiModel>(_allTransactions);
            return;
        }

        var lowerValue = SearchText.ToLower();
        var filtered = _allTransactions.Where(t =>
            t.FullName.ToLower().Contains(lowerValue) ||
            t.Description.ToLower().Contains(lowerValue) ||
            t.Amount.ToString().Contains(lowerValue)).ToList();

        Transactions = new ObservableCollection<WalletPersonUiModel>(filtered);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        TargetIdFilter = null;
        SearchText = string.Empty;
        FilterType = "All";
    }

    partial void OnSelectedPayableDebtOptionChanged(PayableDebtOptionResponse? value)
    {
        if (value != null)
            PaymentAmountText = value.Amount.ToString("F2", CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private async Task OpenPaymentPopup()
    {
        PaymentFormHint = string.Empty;
        IsBusy = true;
        try
        {
            var options = await _settlementService.GetPayableDebtOptionsAsync();
            PayableDebtOptions = new ObservableCollection<PayableDebtOptionResponse>(options ?? new List<PayableDebtOptionResponse>());
            if (PayableDebtOptions.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Spłata", "Brak aktywnych długów do spłaty (albo wszystkie mają już oczekującą spłatę).", "OK");
                return;
            }

            SelectedPayableDebtOption = PayableDebtOptions[0];
            IsPaymentOverlayVisible = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClosePaymentPopup()
    {
        IsPaymentOverlayVisible = false;
        PaymentFormHint = string.Empty;
    }

    [RelayCommand]
    private async Task SubmitSettlement()
    {
        if (SelectedPayableDebtOption is null)
        {
            PaymentFormHint = "Wybierz linię długu.";
            return;
        }

        if (!decimal.TryParse(PaymentAmountText?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            PaymentFormHint = "Podaj poprawną kwotę większą od zera.";
            return;
        }

        if (amount > SelectedPayableDebtOption.Amount)
        {
            PaymentFormHint = $"Kwota nie może przekroczyć {SelectedPayableDebtOption.Amount:N2} zł.";
            return;
        }

        IsBusy = true;
        PaymentFormHint = string.Empty;
        try
        {
            var request = new CreateSettlementRequest
            {
                GroupId = SelectedPayableDebtOption.GroupId,
                ReceiverId = SelectedPayableDebtOption.CreditorId,
                Amount = amount,
                Description = "Spłata z portfela"
            };

            var (ok, err) = await _settlementService.CreateSettlementAsync(request);
            if (ok)
            {
                IsPaymentOverlayVisible = false;
                await Shell.Current.DisplayAlertAsync("Spłata", "Wysłano propozycję spłaty do akceptacji przez wierzyciela.", "OK");
                await LoadDataAsync();
            }
            else
                PaymentFormHint = err ?? "Nie udało się utworzyć spłaty.";
        }
        finally
        {
            IsBusy = false;
        }
    }


    [RelayCommand]
    private async Task AcceptSettlement(WalletPersonUiModel item)
    {
        if (item == null) return;
        IsBusy = true;

        var success = await _settlementService.AcceptSettlementAsync(item.ExpenseId);

        if (success)
        {
            await Shell.Current.DisplayAlertAsync("Sukces", "Spłata została zatwierdzona.", "OK");
            await LoadDataAsync();
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Nie udało się zatwierdzić spłaty.", "OK");
        }

        IsBusy = false;
    }

    [RelayCommand]
    private async Task RejectSettlement(WalletPersonUiModel item)
    {
        if (item == null) return;
        IsBusy = true;

        var success = await _settlementService.RejectSettlementAsync(item.ExpenseId);

        if (success)
        {
            await Shell.Current.DisplayAlertAsync("Odrzucono", "Spłata została odrzucona.", "OK");
            await LoadDataAsync();
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Błąd", "Wystąpił problem przy odrzucaniu.", "OK");
        }

        IsBusy = false;
    }

    [RelayCommand]
    private async Task RemindDebt(WalletPersonUiModel item)
    {
        if (item == null || item.GroupId == 0) return;
        IsBusy = true;

        var req = new RemindDebtRequest { GroupId = item.GroupId, DebtorUserId = item.OtherUserId };
        var (ok, err) = await _settlementService.SendDebtReminderAsync(req);

        if (ok)
        {
            await Shell.Current.DisplayAlertAsync("Przypomnienie", "Wysłano przypomnienie do dłużnika.", "OK");
            await LoadDataAsync();
        }
        else
            await Shell.Current.DisplayAlertAsync("Błąd", err ?? "Nie udało się wysłać przypomnienia.", "OK");

        IsBusy = false;
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        IsPaymentOverlayVisible = false;
        FilterType = "All";
        TargetIdFilter = null;
        Transactions = new ObservableCollection<WalletPersonUiModel>();

        await Shell.Current.GoToAsync("//MainPage");
    }
}