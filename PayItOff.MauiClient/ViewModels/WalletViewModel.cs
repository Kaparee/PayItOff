using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayItOff.MauiClient.Services;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Collections.ObjectModel;
using System.Globalization;

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
    public string TransferReference { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public bool CanSendDebtReminder { get; set; }

    public bool IsPending => Status == "Pending";
    public bool ShowAcceptRejectButtons => IsSettlement && IsIncome && IsPending;
    public bool ShowNormalAmount => !ShowAcceptRejectButtons;

    public string StatusColor => IsIncome ? "#10B981" : "#EF4444";
    public bool ShowRemindButton => !IsSettlement && IsIncome && CanSendDebtReminder;

    public Microsoft.Maui.Controls.Brush ItemBorderBrush
    {
        get
        {
            if (!IsSettlement)
            {
                return Microsoft.Maui.Controls.Brush.Transparent;
            }

            if (Status == "Rejected")
            {
                return new Microsoft.Maui.Controls.SolidColorBrush(Microsoft.Maui.Graphics.Colors.Black);
            }

            return IsIncome
                ? new Microsoft.Maui.Controls.SolidColorBrush(Microsoft.Maui.Graphics.Color.FromArgb("#10B981"))
                : new Microsoft.Maui.Controls.SolidColorBrush(Microsoft.Maui.Graphics.Color.FromArgb("#EF4444"));
        }
    }

    public double ItemBorderThickness => IsSettlement ? 1.0 : 0.0;
    public bool ShowStatusBlock => !IsSettlement;

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
public partial class WalletViewModel : PopupViewModelBase
{
    private readonly SettlementService _settlementService;
    private readonly ExpenseService _expenseService;
    private readonly SignalRService _signalRService;

    private int _loadId = 0;
    private CancellationTokenSource? _searchCts;
    private List<WalletPersonUiModel> _allTransactions = [];
    private List<PayableDebtOptionResponse> _allDebtOptions = [];
    private List<DebtDisplayItem> _allNetPayCreditors = [];


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

    [ObservableProperty]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    public partial bool IsPaymentOverlayVisible { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<PayableDebtOptionResponse> PayableDebtOptions { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<PayableDebtOptionResponse> FilteredPayableDebtOptions { get; set; } = new();

    [ObservableProperty]
    public partial PayableDebtOptionResponse? SelectedPayableDebtOption { get; set; }

    [ObservableProperty]
    public partial string PaymentAmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PaymentFormHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SearchDebtText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsNetPayMode { get; set; } = true;

    [ObservableProperty]
    public partial bool CanSwitchToGrossPay { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DebtDisplayItem> FilteredNetPayCreditors { get; set; } = new();

    [ObservableProperty]
    public partial DebtDisplayItem? SelectedNetPayCreditor { get; set; }


    [ObservableProperty] public partial bool IsTransactionDetailsPopupVisible { get; set; }
    [ObservableProperty] public partial WalletPersonUiModel? SelectedTransactionDetails { get; set; }
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasExpensePhotos))] public partial ExpenseDetailsResponse? SelectedExpenseDetails { get; set; }


    public WalletViewModel(SettlementService settlementService, ExpenseService expenseService, SignalRService signalRService)
    {
        _settlementService = settlementService;
        _expenseService = expenseService;
        _signalRService = signalRService;
        FilterType = "All";
        IsCustomAlertSupported = true;
    }

    public void SubscribeToEvents()
    {
        _signalRService.OnSettlementUpdateReceived += HandleSettlementUpdate;
    }

    public void UnsubscribeFromEvents()
    {
        _signalRService.OnSettlementUpdateReceived -= HandleSettlementUpdate;
    }

    public void OnDisappearing()
    {
        UnsubscribeFromEvents();
    }

    [RelayCommand]
    private void ChangeFilter(string filter)
    {
        if (FilterType == filter)
        {
            return;
        }

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

            if (currentLoadId != _loadId)
            {
                return;
            }

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
                    TransferReference = item.TransferReference,
                    GroupId = item.GroupId,
                    CanSendDebtReminder = item.CanSendDebtReminder
                }).ToList();

                if (targetId.HasValue)
                {
                    newList = newList.Where(x => x.OtherUserId == targetId.Value).ToList();
                }

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
            {
                await ShowAlertAsync("Błąd", ex.Message, "OK");
            }
        }
        finally
        {
            if (currentLoadId == _loadId)
            {
                IsBusy = false;
            }
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
            Transactions.Clear();
            foreach (var t in _allTransactions)
            {
                Transactions.Add(t);
            }

            return;
        }

        var lowerValue = SearchText.ToLower();
        var filtered = _allTransactions.Where(t =>
            t.FullName.ToLower().Contains(lowerValue) ||
            t.Description.ToLower().Contains(lowerValue) ||
            t.Amount.ToString().Contains(lowerValue)).ToList();

        Transactions.Clear();
        foreach (var f in filtered)
        {
            Transactions.Add(f);
        }
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
        if (value != null && !IsNetPayMode)
            PaymentAmountText = value.Amount.ToString("F2", CultureInfo.InvariantCulture);
    }

    partial void OnSelectedNetPayCreditorChanged(DebtDisplayItem? value)
    {
        if (value != null && IsNetPayMode)
            PaymentAmountText = value.Amount.ToString("F2", CultureInfo.InvariantCulture);
    }

    partial void OnIsNetPayModeChanged(bool value)
    {
        PaymentFormHint = string.Empty;
        if (value)
        {
            SelectedPayableDebtOption = null;
            ApplyNetPaySearchFilter();
            PaymentAmountText = string.Empty;
            Application.Current?.Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(50);
                SelectedNetPayCreditor = null;
                PaymentAmountText = string.Empty;
            });
        }
        else
        {
            SelectedNetPayCreditor = null;
            RefreshPayableDebtSearchFilter();
            PaymentAmountText = string.Empty;
            Application.Current?.Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(50);
                SelectedPayableDebtOption = null;
                PaymentAmountText = string.Empty;
            });
        }
    }

    [RelayCommand]
    private void ClosePaymentPopup()
    {
        IsPaymentOverlayVisible = false;
        PaymentFormHint = string.Empty;
        IsNetPayMode = true;
    }

    [RelayCommand]
    private async Task SubmitSettlement()
    {
        if (!decimal.TryParse(PaymentAmountText?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            PaymentFormHint = "Podaj poprawną kwotę większą od zera.";
            return;
        }

        const decimal tol = 0.01m;

        if (IsNetPayMode)
        {
            if (SelectedNetPayCreditor is null)
            {
                PaymentFormHint = "Wybierz wierzyciela (saldo netto).";
                return;
            }

            if (amount > SelectedNetPayCreditor.Amount + tol)
            {
                PaymentFormHint = $"Kwota nie może przekroczyć salda netto: {SelectedNetPayCreditor.Amount:N2} zł.";
                return;
            }

            IsBusy = true;
            PaymentFormHint = string.Empty;
            try
            {
                var netReq = new PayNetDebtRequest { CreditorId = SelectedNetPayCreditor.UserId, Amount = amount };
                var (ok, result, err) = await _settlementService.CreateNetDebtSettlementsAsync(netReq);
                if (ok)
                {
                    var n = result?.SettlementIds?.Count ?? 0;
                    var msg = n > 1
                        ? $"Wysłano {n} propozycji spłaty w poszczególnych grupach (po rozliczeniu netto). Wierzyciel musi je zaakceptować."
                        : "Wysłano propozycję spłaty do akceptacji przez wierzyciela.";
                    IsPaymentOverlayVisible = false;
                    await ShowAlertAsync("Spłata", msg, "OK");
                    await LoadDataAsync();
                }
                else
                {
                    PaymentFormHint = err ?? "Nie udało się utworzyć spłaty netto.";
                }
            }
            finally
            {
                IsBusy = false;
            }

            return;
        }

        if (SelectedPayableDebtOption is null)
        {
            PaymentFormHint = "Wybierz linię długu (konkretna grupa).";
            return;
        }

        if (amount > SelectedPayableDebtOption.Amount + tol)
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
                Amount = amount
            };

            var (ok, err) = await _settlementService.CreateSettlementAsync(request);
            if (ok)
            {
                IsPaymentOverlayVisible = false;
                await ShowAlertAsync("Spłata", "Wysłano propozycję spłaty do akceptacji przez wierzyciela.", "OK");
                await LoadDataAsync();
            }
            else
            {
                PaymentFormHint = err ?? "Nie udało się utworzyć spłaty.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AcceptSettlement(WalletPersonUiModel item)
    {
        if (item == null || !item.IsSettlement)
        {
            return;
        }

        var confirm = await ShowAlertAsync(
            "Akceptacja spłaty",
            $"{item.FullName} wnioskuje o spłatę {item.Amount:N2} zł.\n\nZaakceptować?",
            "Tak",
            "Nie");

        if (!confirm)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var success = await _settlementService.AcceptSettlementAsync(item.ExpenseId);

            if (success)
            {
                await ShowAlertAsync("Sukces", $"Spłata {item.Amount:N2} zł została zatwierdzona.", "OK");
                await LoadDataAsync();
            }
            else
            {
                await ShowAlertAsync("Błąd", "Nie udało się zatwierdzić spłaty.", "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RejectSettlement(WalletPersonUiModel item)
    {
        if (item == null || !item.IsSettlement)
        {
            return;
        }

        var confirm = await ShowAlertAsync(
            "Odrzucenie spłaty",
            $"Odrzucić propozycję spłaty {item.Amount:N2} zł od {item.FullName}?",
            "Odrzuć",
            "Anuluj");

        if (!confirm)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var success = await _settlementService.RejectSettlementAsync(item.ExpenseId);

            if (success)
            {
                await ShowAlertAsync("Odrzucono", $"Propozycja spłaty {item.Amount:N2} zł została odrzucona.", "OK");
                await LoadDataAsync();
            }
            else
            {
                await ShowAlertAsync("Błąd", "Wystąpił problem przy odrzucaniu.", "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ShowTransactionDetails(WalletPersonUiModel item)
    {
        if (item == null)
        {
            return;
        }

        SelectedTransactionDetails = item;
        SelectedExpenseDetails = null;

        if (!item.IsSettlement)
        {
            IsBusy = true;
            try
            {
                SelectedExpenseDetails = await _expenseService.GetExpenseDetailsAsync(item.ExpenseId);
                if (SelectedExpenseDetails != null)
                {
                    SelectedExpenseDetails.PayerAvatarUrl = SelectedExpenseDetails.PayerAvatarUrl;
                    if (SelectedExpenseDetails.Participants != null)
                    {
                        foreach (var participant in SelectedExpenseDetails.Participants)
                        {
                            participant.AvatarUrl = participant.AvatarUrl;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowAlertAsync("Błąd", "Nie udało się pobrać szczegółów wydatku." + ex, "OK");
                IsBusy = false;
                return;
            }
            IsBusy = false;
        }

        IsTransactionDetailsPopupVisible = true;
    }

    [RelayCommand]
    private void CloseTransactionDetailsPopup()
    {
        IsTransactionDetailsPopupVisible = false;
    }

    [RelayCommand]
    private async Task RemindDebt(WalletPersonUiModel item)
    {
        if (item == null || item.GroupId == 0)
        {
            return;
        }

        IsBusy = true;

        var req = new RemindDebtRequest { GroupId = item.GroupId, DebtorUserId = item.OtherUserId };
        var (ok, err) = await _settlementService.SendDebtReminderAsync(req);

        if (ok)
        {
            await ShowAlertAsync("Przypomnienie", "Wysłano przypomnienie do dłużnika.", "OK");
            await LoadDataAsync();
        }
        else
        {
            await ShowAlertAsync("Błąd", err ?? "Nie udało się wysłać przypomnienia.", "OK");
        }

        IsBusy = false;
    }

    [RelayCommand]
    private async Task CompensateDebts()
    {
        IsBusy = true;
        try
        {
            var options = await _settlementService.GetPayableDebtOptionsAsync();
            if (options == null || options.Count == 0)
            {
                await ShowAlertAsync("Info", "Brak długów do kompensacji.", "OK");
                return;
            }

            var uniqueCreditors = options.GroupBy(x => x.CreditorId).Select(g => g.First()).ToList();
            var actionSheetButtons = uniqueCreditors.Select(c => $"{c.CreditorName} {c.CreditorSurname}").ToArray();

            IsBusy = false;

            var action = await ShowActionSheetAsync("Z kim chcesz uprościć długi?", actionSheetButtons);

            if (action == "Anuluj" || string.IsNullOrEmpty(action))
            {
                return;
            }

            var selectedUser = uniqueCreditors.FirstOrDefault(c => $"{c.CreditorName} {c.CreditorSurname}" == action);
            if (selectedUser != null)
            {
                IsBusy = true;
                var request = new CompensateDebtsRequest { TargetUserId = selectedUser.CreditorId };

                var (ok, err) = await _settlementService.CompensateDebtsAsync(request);
                if (ok)
                {
                    await ShowAlertAsync("Sukces", "Długi zostały pomyślnie skompensowane!", "OK");
                    await LoadDataAsync();
                }
                else
                {
                    await ShowAlertAsync("Błąd", err ?? "Nie udało się skompensować długów.", "OK");
                }
            }
        }
        catch (Exception)
        {
            await ShowAlertAsync("Błąd", "Nie udało się skompensować długów. Możliwe, że druga strona nie ma wobec Ciebie żadnych zobowiązań do odliczenia.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSearchDebtTextChanged(string value)
    {
        if (IsNetPayMode)
            ApplyNetPaySearchFilter();
        else
            RefreshPayableDebtSearchFilter();
    }

    private void RefreshPayableDebtSearchFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchDebtText))
        {
            FilteredPayableDebtOptions.Clear();
            foreach (var o in _allDebtOptions)
            {
                FilteredPayableDebtOptions.Add(o);
            }

            return;
        }

        var lower = SearchDebtText.ToLower();
        FilteredPayableDebtOptions.Clear();
        foreach (var x in _allDebtOptions.Where(x =>
                x.CreditorName.ToLower().Contains(lower) ||
                x.CreditorSurname.ToLower().Contains(lower) ||
                x.GroupName.ToLower().Contains(lower)))
        {
            FilteredPayableDebtOptions.Add(x);
        }
    }

    private void ApplyNetPaySearchFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchDebtText))
        {
            FilteredNetPayCreditors.Clear();
            foreach (var c in _allNetPayCreditors)
            {
                FilteredNetPayCreditors.Add(c);
            }

            return;
        }

        var lower = SearchDebtText.ToLower();
        FilteredNetPayCreditors.Clear();
        foreach (var x in _allNetPayCreditors.Where(x => x.FullName.ToLower().Contains(lower)))
        {
            FilteredNetPayCreditors.Add(x);
        }
    }

    private async Task LoadNetPayCreditorsAsync()
    {
        var summary = await _settlementService.GetUserAllExpensesSummaryAsync();
        _allNetPayCreditors = (summary?.Items ?? []).Select(e => new DebtDisplayItem
        {
            UserId = e.UserId,
            FullName = $"{e.Name} {e.Surname}",
            AvatarUrl = e.AvatarUrl,
            Number = e.Number!,
            CategoriesDisplay = e.Categories != null && e.Categories.Count > 0 ? string.Join(", ", e.Categories) : "—",
            Date = e.Date,
            Amount = e.Amount
        }).ToList();
        ApplyNetPaySearchFilter();
    }

    [RelayCommand]
    private async Task OpenPaymentPopup()
    {
        PaymentFormHint = string.Empty;
        SearchDebtText = string.Empty;
        SelectedPayableDebtOption = null;
        SelectedNetPayCreditor = null;
        PaymentAmountText = string.Empty;

        IsBusy = true;
        try
        {
            var options = await _settlementService.GetPayableDebtOptionsAsync();
            _allDebtOptions = options ?? [];
            FilteredPayableDebtOptions.Clear();
            foreach (var o in _allDebtOptions)
            {
                FilteredPayableDebtOptions.Add(o);
            }

            await LoadNetPayCreditorsAsync();

            var hasGross = _allDebtOptions.Count > 0;
            var hasNet = _allNetPayCreditors.Count > 0;

            if (!hasGross && !hasNet)
            {
                await ShowAlertAsync("Spłata", "Brak aktywnych długów do spłaty.", "OK");
                return;
            }

            CanSwitchToGrossPay = hasGross && hasNet;
            IsNetPayMode = hasNet;

            if (int.TryParse(TargetIdFilter, out var tid) && tid > 0)
            {
                var match = _allNetPayCreditors.FirstOrDefault(x => x.UserId == tid);
                if (match != null)
                {
                    IsNetPayMode = true;
                    ApplyNetPaySearchFilter();
                    Application.Current?.Dispatcher.Dispatch(async () =>
                    {
                        await Task.Delay(100);
                        SelectedNetPayCreditor = match;
                        PaymentAmountText = match.Amount.ToString("F2", CultureInfo.InvariantCulture);
                    });
                }
            }
            else
            {
                SelectedPayableDebtOption = null;
                PaymentAmountText = string.Empty;

                Application.Current?.Dispatcher.Dispatch(async () =>
                {
                    await Task.Delay(50);
                    SelectedNetPayCreditor = null;
                    SelectedPayableDebtOption = null;
                    PaymentAmountText = string.Empty;
                });
            }

            IsPaymentOverlayVisible = true;
        }
        finally
        {
            IsBusy = false;
        }
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
        IsNetPayMode = true;
        FilterType = "All";
        TargetIdFilter = null;
        Transactions.Clear();

        await Shell.Current.GoToAsync("//MainPage");
    }

    [ObservableProperty]
    public partial bool IsPhotoViewerVisible { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasExpensePhotos))]
    public partial ObservableCollection<string> ExpensePhotosToView { get; set; } = new();

    [ObservableProperty]
    public partial string? SelectedExpensePhotoUrl { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhotoViewerTitle))]
    public partial int SelectedPhotoIndex { get; set; }

    public bool HasExpensePhotos => SelectedExpenseDetails?.ReceiptPhotos?.Count > 0;
    public string PhotoViewerTitle => ExpensePhotosToView.Count > 0
        ? $"Zdjęcie {SelectedPhotoIndex + 1} / {ExpensePhotosToView.Count}"
        : "Brak zdjęć";

    [RelayCommand]
    private void ShowExpensePhotos()
    {
        if (SelectedExpenseDetails?.ReceiptPhotos == null || SelectedExpenseDetails.ReceiptPhotos.Count == 0)
        {
            return;
        }

        ExpensePhotosToView.Clear();
        foreach (var url in SelectedExpenseDetails.ReceiptPhotos)
        {
            if (string.IsNullOrEmpty(url))
            {
                continue;
            }

            ExpensePhotosToView.Add(url);
        }

        SelectedPhotoIndex = 0;
        SelectedExpensePhotoUrl = ExpensePhotosToView[0];
        OnPropertyChanged(nameof(HasExpensePhotos));
        OnPropertyChanged(nameof(PhotoViewerTitle));
        IsPhotoViewerVisible = true;
    }

    [RelayCommand]
    private void ClosePhotoViewer()
    {
        IsPhotoViewerVisible = false;
    }

    [RelayCommand]
    private void PhotoSwipeLeft()
    {
        if (ExpensePhotosToView.Count == 0)
        {
            return;
        }

        SelectedPhotoIndex = (SelectedPhotoIndex + 1) % ExpensePhotosToView.Count;
        SelectedExpensePhotoUrl = ExpensePhotosToView[SelectedPhotoIndex];
        OnPropertyChanged(nameof(PhotoViewerTitle));
    }

    [RelayCommand]
    private void PhotoSwipeRight()
    {
        if (ExpensePhotosToView.Count == 0)
        {
            return;
        }

        SelectedPhotoIndex = (SelectedPhotoIndex - 1 + ExpensePhotosToView.Count) % ExpensePhotosToView.Count;
        SelectedExpensePhotoUrl = ExpensePhotosToView[SelectedPhotoIndex];
        OnPropertyChanged(nameof(PhotoViewerTitle));
    }

    [RelayCommand]
    private void SelectPhotoThumbnail(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        var idx = ExpensePhotosToView.IndexOf(url);
        if (idx >= 0)
        {
            SelectedPhotoIndex = idx;
            SelectedExpensePhotoUrl = url;
            OnPropertyChanged(nameof(PhotoViewerTitle));
        }
    }

    private void HandleSettlementUpdate()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await LoadDataAsync();
        });
    }
}