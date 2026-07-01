using Microsoft.Extensions.Configuration;
using PayItOff.Application.Helpers;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Domain.Exceptions;
using PayItOff.Domain.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Application.Services;

public class SettlementService : ISettlementService
{
    private readonly IConfiguration _configuration;
    private readonly IGroupDebtRepository _groupDebtRepository;
    private readonly IUserRepository _userRepository;
    private readonly IExpenseSplitRepository _expenseSplitRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupRepository _groupRepository;
    private readonly ISettlementRepository _settlementRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailService _emailService;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly INotificationService _notificationService;

    public SettlementService(
        IConfiguration configuration,
        IGroupDebtRepository groupDebtRepository,
        IUserRepository userRepository,
        IExpenseSplitRepository expenseSplitRepository,
        IUnitOfWork unitOfWork,
        IGroupRepository groupRepository,
        ISettlementRepository settlementRepository,
        INotificationRepository notificationRepository,
        IEmailService emailService,
        IGroupMemberRepository groupMemberRepository,
        INotificationService notificationService)
    {
        _configuration = configuration;
        _groupDebtRepository = groupDebtRepository;
        _userRepository = userRepository;
        _expenseSplitRepository = expenseSplitRepository;
        _unitOfWork = unitOfWork;
        _groupRepository = groupRepository;
        _settlementRepository = settlementRepository;
        _notificationRepository = notificationRepository;
        _emailService = emailService;
        _groupMemberRepository = groupMemberRepository;
        _notificationService = notificationService;
    }

    public async Task<GlobalSettlementResponse> GetUserAllIncomesSummaryAsync(int userId)
    {
        var incomes = await _groupDebtRepository.GetUserTotalIncomesAsync(userId);
        var baseUrl = _configuration["AppUrls:BackendUrl"];

        var items = incomes.Select(data => new GlobalDebtSummaryResponse
        {
            UserId = data.UserId,
            Name = data.Name,
            Surname = data.Surname,
            AvatarUrl = UrlHelper.BuildUserAvatarUrl(baseUrl!, data.AvatarUrl),
            Categories = data.Categories,
            Date = data.Date,
            Amount = data.Amount
        }).ToList();

        return new GlobalSettlementResponse { Items = items, TotalAmount = items.Sum(i => i.Amount) };
    }

    public async Task<GlobalSettlementResponse> GetUserAllExpensesSummaryAsync(int userId)
    {
        var expenses = await _groupDebtRepository.GetUserTotalExpensesAsync(userId);
        var baseUrl = _configuration["AppUrls:BackendUrl"];

        var items = expenses.Select(data => new GlobalDebtSummaryResponse
        {
            UserId = data.UserId,
            Name = data.Name,
            Surname = data.Surname,
            AvatarUrl = UrlHelper.BuildUserAvatarUrl(baseUrl!, data.AvatarUrl),
            Categories = data.Categories,
            Date = data.Date,
            Amount = data.Amount
        }).ToList();

        return new GlobalSettlementResponse { Items = items, TotalAmount = items.Sum(i => i.Amount) };
    }

    public async Task<PagedTransactionResponse> GetHistoryAsync(int userId, UserExpenseHistoryRequest request)
    {
        const int pageSize = 10;
        var baseUrl = _configuration["AppUrls:BackendUrl"];

        var (splits, settlements, totalCount) = await _expenseSplitRepository.GetMixedHistoryAsync(
            userId, request.TargetId, request.Type, request.Page, pageSize);

        var counts = await _expenseSplitRepository.GetMixedHistoryCountsAsync(userId, request.TargetId);

        var validSplits = splits.Where(s => s.UserId != s.ExpenseItem.Expense.PayerId).ToList();

        var groupedSplits = validSplits
            .GroupBy(s => new
            {
                ExpenseId = s.ExpenseItem.ExpenseId,
                TargetUserId = s.ExpenseItem.Expense.PayerId == userId ? s.UserId : s.ExpenseItem.Expense.PayerId
            })
            .Select(group =>
            {
                var expense = group.First().ExpenseItem.Expense;
                bool amIPayer = expense.PayerId == userId;

                User otherUser = amIPayer
                    ? group.First().User
                    : expense.Payer;

                if (otherUser.Id == userId)
                    throw new InvalidOperationException("Nie udało się ustalić drugiej strony transakcji.");

                return new UserDebtComponentResponse
                {
                    ExpenseId = expense.Id,
                    GroupId = expense.GroupId,
                    Date = expense.PurchasedAt,
                    GroupName = expense.Group?.Name!,
                    AmIDebtor = !amIPayer,
                    Amount = group.Sum(s => s.OwedAmount),
                    Categories = group.Select(s => s.ExpenseItem.Category).Distinct().ToList(),
                    OtherUserId = otherUser.Id,
                    OtherName = otherUser.Name,
                    OtherSurname = otherUser.Surname,
                    OtherAvatarUrl = UrlHelper.BuildUserAvatarUrl(baseUrl!, otherUser.AvatarUrl),
                    IsSettlement = false,
                    Status = "Confirmed",
                    CanSendDebtReminder = false
                };
            }).ToList();

        foreach (var row in groupedSplits)
        {
            if (row.IsSettlement || row.AmIDebtor || row.GroupId == 0)
                continue;

            var debt = await _groupDebtRepository.GetDebtAsync(row.GroupId, row.OtherUserId, userId);
            if (debt is null || debt.Amount <= 0)
                continue;

            if (await _settlementRepository.HasPendingSettlementAsync(row.OtherUserId, userId, row.GroupId))
                continue;

            var since = DateTime.UtcNow.AddHours(-24);
            if (await _notificationRepository.HasDebtReminderSinceAsync(userId, row.OtherUserId, debt.Id, since))
                continue;

            row.CanSendDebtReminder = true;
        }

        var mappedSettlements = settlements.Select(s =>
        {
            bool amISender = s.SenderId == userId;

            var otherUser = amISender ? s.Receiver : s.Sender;

            if (otherUser.Id == userId)
                throw new InvalidOperationException("Nie udało się ustalić drugiej strony spłaty.");

            return new UserDebtComponentResponse
            {
                ExpenseId = s.Id,
                GroupId = s.GroupId,
                Date = s.CreatedAt,
                GroupName = "Spłata długu",
                AmIDebtor = amISender,
                Amount = s.Amount,
                Categories = new List<string> { "Transfer" },
                OtherUserId = otherUser.Id,
                OtherName = otherUser.Name,
                OtherSurname = otherUser.Surname,
                OtherAvatarUrl = UrlHelper.BuildUserAvatarUrl(baseUrl!, otherUser.AvatarUrl),
                IsSettlement = true,
                Status = s.Status.ToString(),
                TransferReference = s.TransferReference,
                CanSendDebtReminder = false
            };
        }).ToList();

        var combinedTimeline = groupedSplits.Concat(mappedSettlements)
            .OrderByDescending(x => x.Date)
            .ToList();

        return new PagedTransactionResponse
        {
            Items = combinedTimeline,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            CurrentPage = request.Page,
            TotalTransactionsCount = counts.Total,
            TotalIncomesCount = counts.Incomes,
            TotalExpensesCount = counts.Expenses
        };
    }

    public async Task<decimal> GetUserCurrentTotalDebtAsync(int userId, int? targetId = null)
    {
        var expenses = await _groupDebtRepository.GetUserTotalExpensesAsync(userId);
        var query = expenses.AsQueryable();
        if (targetId.HasValue) query = query.Where(x => x.UserId == targetId.Value);
        return query.Sum(x => x.Amount);
    }

    public async Task<List<PayableDebtOptionResponse>> GetPayableDebtOptionsAsync(int userId)
    {
        var rows = await _groupDebtRepository.GetOpenDebtLinesForDebtorAsync(userId);
        var list = new List<PayableDebtOptionResponse>();

        foreach (var r in rows)
        {
            if (await _settlementRepository.HasPendingSettlementAsync(userId, r.CreditorId, r.GroupId))
                continue;

            list.Add(new PayableDebtOptionResponse
            {
                GroupId = r.GroupId,
                GroupName = r.GroupName,
                CreditorId = r.CreditorId,
                CreditorName = r.CreditorName,
                CreditorSurname = r.CreditorSurname,
                Amount = r.Amount
            });
        }

        return list;
    }

    public async Task<int> CreateSettlementAsync(int userId, CreateSettlementRequest request)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (request.Amount <= 0) throw new SettlementOperationException("Kwota musi być większa od zera.");

            var groupInfo = await _groupRepository.GetGroupInfoByIdAsync(request.GroupId) ?? throw new SettlementOperationException("Nie znaleziono grupy.");

            var groupMember = await _groupMemberRepository.GetMemberAsync(request.GroupId, userId);
            if (groupMember == null)
                throw new SettlementOperationException("Nie jesteś członkiem wskazanej grupy.");

            var creditorMember = await _groupMemberRepository.GetMemberAsync(request.GroupId, request.ReceiverId);
            if (creditorMember == null)
                throw new SettlementOperationException("Wierzyciel nie należy do wskazanej grupy.");

            var debtRecord = await _groupDebtRepository.GetDebtAsync(request.GroupId, userId, request.ReceiverId);
            decimal currentDebtAmount = debtRecord?.Amount ?? 0m;

            if (currentDebtAmount < request.Amount)
            {
                throw new SettlementOperationException($"Nie możesz spłacić więcej niż wynosi Twój dług wobec tego użytkownika w tej grupie. Aktualny dług: {currentDebtAmount}");
            }

            var sender = await _userRepository.GetUserByIdAsync(userId)
                ?? throw new SettlementOperationException("Nie znaleziono nadawcy w bazie.");

            var receiver = await _userRepository.GetUserByIdAsync(request.ReceiverId)
                ?? throw new SettlementOperationException("Nie znaleziono odbiorcy w bazie.");

            string description = $"Spłata w grupie {groupInfo.Name}";

            var settlement = Settlement.Create(sender, receiver, groupInfo, request.Amount, description);

            await _settlementRepository.AddAsync(settlement);

            await _unitOfWork.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(receiver.Id, sender.Id, NotificationType.NeedAction, $"{sender.FullName} zadeklarował spłatę {request.Amount} zł w grupie {groupInfo.Name}", settlement.Id, EntityType.Settlements);
            await _unitOfWork.CommitAsync();

            return settlement.Id;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> AcceptSettlementAsync(int userId, int settlementId)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var result = await AcceptSettlementInternalAsync(userId, settlementId);
            await _unitOfWork.CommitAsync();
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task<bool> AcceptSettlementInternalAsync(int userId, int settlementId)
    {
        var settlement = await _settlementRepository.GetSettlementByIdAsync(userId, settlementId)
            ?? throw new SettlementOperationException("Nie znaleziono spłaty.");

        if (settlement.ReceiverId != userId)
            throw new UnauthorizedAccessException("Tylko odbiorca może zaakceptować spłatę.");

        if (settlement.Status != SettlementStatus.Pending)
            throw new SettlementOperationException("Ta spłata nie oczekuje już na akceptację.");

        var debt = await _groupDebtRepository.GetDebtAsync(settlement.GroupId, settlement.SenderId, settlement.ReceiverId);
        if (debt is null || debt.Amount < settlement.Amount)
            throw new SettlementOperationException(
                "Saldo długu w grupie jest mniejsze niż proponowana spłata (np. rozliczyły się nowe wydatki). Odrzuć tę propozycję lub poproś dłużnika o nową kwotę.");

        settlement.Confirm();
        var sender = await _userRepository.GetUserByIdAsync(settlement.SenderId) ?? throw new UserNotFoundException();
        var receiver = await _userRepository.GetUserByIdAsync(userId) ?? throw new UserNotFoundException();
        var group = await _groupRepository.GetGroupInfoByIdAsync(settlement.GroupId) ?? throw new SettlementOperationException("Nie znaleziono grupy.");

        await _groupDebtRepository.ApplyDebtChangeAsync(group, sender, receiver, -settlement.Amount);

        if (sender.NotificationsSettings.NotifyOnTransferConfirmed == true)
        {
            await _notificationService.CreateNotificationAsync(sender.Id, userId, NotificationType.Normal, $"{receiver.FullName} zatwierdził twoją spłatę długu, która wynosiła: {settlement.Amount} zł", settlement.Id, EntityType.Settlements);
        }

        await _notificationService.ResolveActionNotificationAsync(userId, settlement.Id, EntityType.Settlements, true);

        group.UpdateTimestamp();
        await _groupRepository.UpdateAsync(group);

        return true;
    }

    public async Task<bool> RejectSettlementAsync(int userId, int settlementId)
    {
        var settlement = await _settlementRepository.GetSettlementByIdAsync(userId, settlementId)
            ?? throw new SettlementOperationException("Nie znaleziono spłaty.");
        if (settlement.ReceiverId != userId) throw new UnauthorizedAccessException("Tylko odbiorca może odrzucić spłatę.");
        if (settlement.Status != SettlementStatus.Pending)
            throw new SettlementOperationException("Ta spłata nie oczekuje już na decyzję.");
        settlement.Reject();

        var sender = await _userRepository.GetUserByIdAsync(settlement.SenderId) ?? throw new UserNotFoundException();
        var receiver = await _userRepository.GetUserByIdAsync(userId) ?? throw new UserNotFoundException();

        await _notificationService.CreateNotificationAsync(sender.Id, userId, NotificationType.Normal, $"{receiver.FullName} odrzucił twoją spłatę długu, wyjaśnij sytuację", settlement.Id, EntityType.Settlements);

        await _notificationService.ResolveActionNotificationAsync(userId, settlement.Id, EntityType.Settlements, false);

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AcceptNetSettlementsAsync(int receiverId, int senderId)
    {
        var pendingIds = await _settlementRepository.GetPendingSettlementIdsAsync(senderId, receiverId);

        if (!pendingIds.Any()) return false;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var settlementId in pendingIds)
            {
                var success = await AcceptSettlementInternalAsync(receiverId, settlementId);
                if (!success) throw new SettlementOperationException("Błąd podczas zbiorczej akceptacji spłaty.");
            }

            var notification = await _notificationRepository.GetActionNotificationAsync(receiverId, senderId, EntityType.NetSettlements);
            if (notification != null)
            {
                notification.ChangeTypeToNormal();
                notification.AppendToBody(" (ZAAKCEPTOWANE)");
                await _notificationRepository.UpdateAsync(notification);
            }

            await _unitOfWork.CommitAsync();
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> RejectNetSettlementsAsync(int receiverId, int senderId)
    {
        var pendingIds = await _settlementRepository.GetPendingSettlementIdsAsync(senderId, receiverId);

        if (!pendingIds.Any()) return false;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var settlementId in pendingIds)
            {
                var success = await RejectSettlementAsync(receiverId, settlementId);
                if (!success) throw new SettlementOperationException("Błąd podczas zbiorczego odrzucania spłaty.");
            }

            var notification = await _notificationRepository.GetActionNotificationAsync(receiverId, senderId, EntityType.NetSettlements);
            if (notification != null)
            {
                notification.ChangeTypeToNormal();
                notification.AppendToBody(" (ODRZUCONE)");
                await _notificationRepository.UpdateAsync(notification);
            }

            await _unitOfWork.CommitAsync();
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task SendDebtReminderAsync(int creditorUserId, RemindDebtRequest request)
    {
        if (creditorUserId == request.DebtorUserId)
            throw new SettlementOperationException("Nie możesz wysłać przypomnienia samemu sobie.");

        var debtor = await _userRepository.GetUserByIdAsync(request.DebtorUserId) ?? throw new UserNotFoundException();
        var creditor = await _userRepository.GetUserByIdAsync(creditorUserId) ?? throw new UserNotFoundException();
        var group = await _groupRepository.GetGroupInfoByIdAsync(request.GroupId) ?? throw new SettlementOperationException("Nie znaleziono grupy.");

        var debt = await _groupDebtRepository.GetDebtAsync(request.GroupId, request.DebtorUserId, creditorUserId)
            ?? throw new SettlementOperationException("Brak aktywnego długu do przypomnienia.");

        if (debt.Amount <= 0)
            throw new SettlementOperationException("Brak aktywnego długu do przypomnienia.");

        if (await _settlementRepository.HasPendingSettlementAsync(request.DebtorUserId, creditorUserId, request.GroupId))
            throw new SettlementOperationException("Dłużnik ma już oczekującą spłatę do Ciebie — poczekaj na decyzję.");

        var since = DateTime.UtcNow.AddHours(-24);
        if (await _notificationRepository.HasDebtReminderSinceAsync(creditorUserId, request.DebtorUserId, debt.Id, since))
            throw new SettlementOperationException("Przypomnienie można wysłać najwyżej raz na 24 godziny.");

        await _notificationService.CreateNotificationAsync(debtor.Id, creditorUserId, NotificationType.Normal, $"{creditor.FullName} przypomina o zapłacie {debt.Amount:N2} PLN w grupie {group.Name}.", debt.Id, EntityType.Settlements);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<PayNetDebtResponse> CreateNetDebtSettlementsAsync(int userId, PayNetDebtRequest request)
    {
        var sender = await _userRepository.GetUserByIdAsync(userId)
                ?? throw new SettlementOperationException("Nie znaleziono nadawcy w bazie.");

        var receiver = await _userRepository.GetUserByIdAsync(request.CreditorId)
            ?? throw new SettlementOperationException("Nie znaleziono odbiorcy w bazie.");

        const decimal tol = 0.01m;

        if (request.Amount <= 0)
            throw new SettlementOperationException("Kwota musi być większa od zera.");

        if (request.CreditorId == userId)
            throw new SettlementOperationException("Nie możesz spłacić długu samemu sobie.");

        var bilateral = (await _groupDebtRepository.GetBilateralActiveDebtsBetweenUsersAsync(userId, request.CreditorId)).ToList();
        var forwardSum = bilateral.Where(d => d.DebtorId == userId && d.CreditorId == request.CreditorId).Sum(d => d.Amount);
        var reverseSum = bilateral.Where(d => d.DebtorId == request.CreditorId && d.CreditorId == userId).Sum(d => d.Amount);
        var net = forwardSum - reverseSum;

        if (net <= tol)
            throw new SettlementOperationException("Brak dodatniego salda netto wobec wskazanego użytkownika.");

        if (request.Amount > net + tol)
            throw new SettlementOperationException($"Kwota ({request.Amount:N2} PLN) przekracza saldo netto ({net:N2} PLN).");

        var forwardGroupIds = bilateral
            .Where(d => d.DebtorId == userId && d.CreditorId == request.CreditorId)
            .Select(d => d.GroupId)
            .Distinct()
            .ToList();

        var pendingForwardKeys = await _settlementRepository.GetPendingSettlementKeysForUserPairInGroupsAsync(
            userId, request.CreditorId, forwardGroupIds);

        if (pendingForwardKeys.Any(k => k.SenderId == userId && k.ReceiverId == request.CreditorId))
            throw new SettlementOperationException("Masz już oczekującą spłatę do tej osoby w jednej z grup — rozlicz ją lub poczekaj na decyzję.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await CompensateBilateralDebtsAsync(userId, request.CreditorId);
            await _unitOfWork.SaveChangesAsync();

            var after = (await _groupDebtRepository.GetBilateralActiveDebtsBetweenUsersAsync(userId, request.CreditorId)).ToList();
            var forwards = after
                .Where(d => d.DebtorId == userId && d.CreditorId == request.CreditorId && d.Amount > 0)
                .OrderByDescending(d => d.Amount)
                .ToList();

            var availableForward = forwards.Sum(d => d.Amount);
            if (request.Amount > availableForward + tol)
                throw new SettlementOperationException("Saldo długów zmieniło się w trakcie operacji. Spróbuj ponownie.");

            var created = new List<Settlement>();
            decimal remaining = request.Amount;

            foreach (var row in forwards)
            {
                if (remaining <= tol)
                    break;

                var take = Math.Min(row.Amount, remaining);
                if (take <= tol)
                    continue;

                var settlement = await BuildPendingSettlementForNetPayAsync(userId, request.CreditorId, row.GroupId, take);
                await _unitOfWork.SaveChangesAsync();
                created.Add(settlement);
                remaining -= take;
            }

            if (created.Any())
            {
                await _notificationService.CreateNotificationAsync(request.CreditorId, userId, NotificationType.NeedAction, $"{sender.FullName} zadeklarował zbiorczą spłatę netto: {request.Amount:N2} zł. Zaakceptuj, aby rozliczyć wszystkie powiązane grupy.", userId, EntityType.NetSettlements);
            }

            if (remaining > tol)
                throw new SettlementOperationException("Nie udało się rozłożyć kwoty na długi w poszczególnych grupach.");

            await _unitOfWork.CommitAsync();
            return new PayNetDebtResponse { SettlementIds = created.Select(s => s.Id).ToList() };
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task CompensateMutualDebtsAsync(int userId, CompensateDebtsRequest request)
    {
        if (request.TargetUserId == userId)
            throw new SettlementOperationException("Nie możesz rozliczyć wzajemności ze samym sobą.");

        _ = await _userRepository.GetUserByIdAsync(request.TargetUserId)
            ?? throw new SettlementOperationException("Nie znaleziono wybranego użytkownika.");

        var bilateral = (await _groupDebtRepository.GetBilateralActiveDebtsBetweenUsersAsync(userId, request.TargetUserId)).ToList();
        var forward = bilateral.Where(d => d.DebtorId == userId && d.CreditorId == request.TargetUserId).ToList();
        var reverse = bilateral.Where(d => d.DebtorId == request.TargetUserId && d.CreditorId == userId).ToList();

        if (forward.Count == 0 || reverse.Count == 0)
            throw new SettlementOperationException(
                "Brak wzajemnych należności do rozliczenia (musisz być dłużnikiem i jednocześnie wierzycielem tej osoby w różnych grupach).");

        var mutualGroupIds = bilateral.Select(d => d.GroupId).Distinct().ToList();
        var pendingMutualKeys = await _settlementRepository.GetPendingSettlementKeysForUserPairInGroupsAsync(
            userId, request.TargetUserId, mutualGroupIds);

        foreach (var row in bilateral.Where(d => d.Amount > 0))
        {
            if (pendingMutualKeys.Contains((row.DebtorId, row.CreditorId, row.GroupId)))
                throw new SettlementOperationException(
                    "Jest oczekująca spłata powiązana z tymi długami — najpierw ją rozwiąż lub anuluj.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await CompensateBilateralDebtsAsync(userId, request.TargetUserId);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task CompensateBilateralDebtsAsync(int debtorId, int creditorId)
    {
        var debts = (await _groupDebtRepository.GetBilateralActiveDebtsBetweenUsersAsync(debtorId, creditorId)).ToList();

        for (var safety = 0; safety < 500; safety++)
        {
            var forward = debts.Where(d => d.DebtorId == debtorId && d.CreditorId == creditorId && d.Amount > 0).ToList();
            var reverse = debts.Where(d => d.DebtorId == creditorId && d.CreditorId == debtorId && d.Amount > 0).ToList();

            if (forward.Count == 0 || reverse.Count == 0)
                return;

            var f = forward.MaxBy(x => x.Amount)!;
            var r = reverse.MaxBy(x => x.Amount)!;
            var x = Math.Min(f.Amount, r.Amount);

            try
            {
                _groupDebtRepository.ApplyDirectDebtReduction(r, x);
                _groupDebtRepository.ApplyDirectDebtReduction(f, x);
            }
            catch (InvalidOperationException ex)
            {
                throw new SettlementOperationException($"Nie udało się rozliczyć wzajemnych długów: {ex.Message}");
            }
        }

        throw new SettlementOperationException("Przekroczono limit bezpieczeństwa rozliczania wzajemnych długów.");
    }

    private async Task<Settlement> BuildPendingSettlementForNetPayAsync(int senderId, int creditorId, int groupId, decimal amount)
    {
        if (amount <= 0)
            throw new SettlementOperationException("Kwota musi być większa od zera.");

        var groupInfo = await _groupRepository.GetGroupInfoByIdAsync(groupId)
            ?? throw new SettlementOperationException("Nie znaleziono grupy.");

        var groupMember = await _groupMemberRepository.GetMemberAsync(groupId, senderId);
        if (groupMember == null)
            throw new SettlementOperationException("Nie jesteś członkiem wskazanej grupy.");

        var creditorMember = await _groupMemberRepository.GetMemberAsync(groupId, creditorId);
        if (creditorMember == null)
            throw new SettlementOperationException("Wierzyciel nie należy do wskazanej grupy.");

        var debtRecord = await _groupDebtRepository.GetDebtAsync(groupId, senderId, creditorId);
        var currentDebtAmount = debtRecord?.Amount ?? 0m;
        if (currentDebtAmount < amount)
            throw new SettlementOperationException($"Nie możesz spłacić więcej niż wynosi Twój dług wobec tego użytkownika w tej grupie. Aktualny dług: {currentDebtAmount:N2}");

        if (await _settlementRepository.HasPendingSettlementAsync(senderId, creditorId, groupId))
            throw new SettlementOperationException("Masz już oczekującą spłatę do tej osoby w tej grupie.");

        var sender = await _userRepository.GetUserByIdAsync(senderId)
            ?? throw new SettlementOperationException("Nie znaleziono nadawcy w bazie.");
        var receiver = await _userRepository.GetUserByIdAsync(creditorId)
            ?? throw new SettlementOperationException("Nie znaleziono odbiorcy w bazie.");
        var groupEntity = await _groupRepository.GetGroupInfoByIdAsync(groupId)
            ?? throw new SettlementOperationException("Nie znaleziono encji grupy w bazie.");

        var description = $"Spłata salda netto — grupa {groupInfo.Name}";
        var settlement = Settlement.Create(sender, receiver, groupEntity, amount, description);
        await _settlementRepository.AddAsync(settlement);
        return settlement;
    }
}
