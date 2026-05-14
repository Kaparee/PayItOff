using Microsoft.Extensions.Configuration;
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
        IGroupMemberRepository groupMemberRepository)
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
            AvatarUrl = $"{baseUrl}/avatars/{data.AvatarUrl ?? "default-user-avatar.png"}",
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
            AvatarUrl = $"{baseUrl}/avatars/{data.AvatarUrl ?? "default-user-avatar.png"}",
            Categories = data.Categories,
            Date = data.Date,
            Amount = data.Amount
        }).ToList();

        return new GlobalSettlementResponse { Items = items, TotalAmount = items.Sum(i => i.Amount) };
    }

    public async Task<PagedTransactionResponse> GetHistoryAsync(int userId, UserExpenseHistoryRequest request)
    {
        const int pageSize = 25;
        var baseUrl = _configuration["AppUrls:BackendUrl"];

        var (splits, settlements, totalCount) = await _expenseSplitRepository.GetMixedHistoryAsync(
            userId, request.TargetId, request.Type, request.Page, pageSize);

        var counts = await _expenseSplitRepository.GetMixedHistoryCountsAsync(userId, request.TargetId);

        var groupedSplits = splits
            .GroupBy(s => s.ExpenseItem.ExpenseId)
            .Select(group =>
            {
                var expense = group.First().ExpenseItem.Expense;
                bool amIPayer = expense.PayerId == userId;

                User otherUser;
                if (amIPayer)
                {
                    var otherSplit = group.FirstOrDefault(s => s.UserId != userId);
                    otherUser = otherSplit?.User ?? expense.Payer;
                }
                else
                {
                    otherUser = expense.Payer;
                }

                if (otherUser.Id == userId)
                {
                    var fallbackId = group.Select(s => s.UserId).FirstOrDefault(uid => uid != userId);
                    if (fallbackId != 0)
                        otherUser = group.First(s => s.UserId == fallbackId).User;
                }

                if (otherUser.Id == userId)
                    throw new InvalidOperationException("Nie udało się ustalić drugiej strony transakcji.");

                return new UserDebtComponentResponse
                {
                    ExpenseId = expense.Id,
                    GroupId = expense.GroupId,
                    Date = expense.PurchasedAt,
                    GroupName = expense.Group?.Name!,
                    AmIDebtor = !amIPayer,
                    Amount = amIPayer
                        ? group.Where(s => s.UserId != userId).Sum(s => s.OwedAmount)
                        : group.Where(s => s.UserId == userId).Sum(s => s.OwedAmount),
                    Categories = group.Select(s => s.ExpenseItem.Category).Distinct().ToList(),
                    OtherUserId = otherUser.Id,
                    OtherName = otherUser.Name,
                    OtherSurname = otherUser.Surname,
                    OtherAvatarUrl = $"{baseUrl}/avatars/{otherUser.AvatarUrl ?? "default-user-avatar.png"}",
                    IsSettlement = false,
                    Status = "Confirmed",
                    SettlementBorderColor = string.Empty,
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

            string borderColor = string.Empty;
            if (s.Status == SettlementStatus.Pending)
                borderColor = amISender ? "#FF4500" : "#00FF7F";
            else if (s.Status == SettlementStatus.Rejected && amISender)
                borderColor = "#000000";

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
                OtherAvatarUrl = $"{baseUrl}/avatars/{otherUser.AvatarUrl ?? "default-user-avatar.png"}",
                IsSettlement = true,
                Status = s.Status.ToString(),
                SettlementBorderColor = borderColor,
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

    public async Task CreateSettlementAsync(int userId, CreateSettlementRequest request)
    {
        if (request.Amount <= 0)
            throw new SettlementOperationException("Kwota spłaty musi być większa od zera.");

        var sender = await _userRepository.GetUserByIdAsync(userId) ?? throw new UserNotFoundException();
        var receiver = await _userRepository.GetUserByIdAsync(request.ReceiverId) ?? throw new UserNotFoundException();
        var group = await _groupRepository.GetGroupInfoByIdAsync(request.GroupId) ?? throw new SettlementOperationException("Nie znaleziono grupy.");

        if (await _groupMemberRepository.GetMemberAsync(request.GroupId, userId) is null)
            throw new SettlementOperationException("Nie należysz do tej grupy.");

        if (await _groupMemberRepository.GetMemberAsync(request.GroupId, request.ReceiverId) is null)
            throw new SettlementOperationException("Odbiorca nie należy do tej grupy.");

        var debt = await _groupDebtRepository.GetDebtAsync(request.GroupId, userId, request.ReceiverId)
            ?? throw new SettlementOperationException("Brak zaksięgowanego długu do tej osoby w wybranej grupie.");

        if (debt.Amount < request.Amount)
            throw new SettlementOperationException("Kwota przekracza aktualny dług w tej grupie.");

        if (await _settlementRepository.HasPendingSettlementAsync(userId, request.ReceiverId, request.GroupId))
            throw new SettlementOperationException("Masz już oczekującą spłatę do tej osoby w tej grupie — poczekaj na akceptację lub anuluj ją.");

        var settlement = Settlement.Create(sender, receiver, group, request.Amount, request.Description ?? "Spłata długu");
        await _settlementRepository.AddAsync(settlement);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> AcceptSettlementAsync(int userId, int settlementId)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var settlement = await _settlementRepository.GetSettlementByIdAsync(userId, settlementId) ?? throw new Exception("Nie znaleziono spłaty.");
            if (settlement.ReceiverId != userId) throw new UnauthorizedAccessException("Tylko odbiorca może zaakceptować spłatę.");

            settlement.Confirm();
            var sender = await _userRepository.GetUserByIdAsync(settlement.SenderId) ?? throw new UserNotFoundException();
            var receiver = await _userRepository.GetUserByIdAsync(userId) ?? throw new UserNotFoundException();
            var group = await _groupRepository.GetGroupInfoByIdAsync(settlement.GroupId) ?? throw new Exception("Brak grupy.");

            await _groupDebtRepository.ApplyDebtChangeAsync(group, sender, receiver, -settlement.Amount);
            await _unitOfWork.CommitAsync();
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> RejectSettlementAsync(int userId, int settlementId)
    {
        var settlement = await _settlementRepository.GetSettlementByIdAsync(userId, settlementId) ?? throw new Exception("Nie znaleziono spłaty.");
        if (settlement.ReceiverId != userId) throw new UnauthorizedAccessException("Tylko odbiorca może odrzucić spłatę.");
        settlement.Reject();
        await _unitOfWork.SaveChangesAsync();
        return true;
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

        var body = $"{creditor.Name} {creditor.Surname} przypomina o zapłacie {debt.Amount:N2} PLN w grupie {group.Name}.";
        var notification = Notification.Create(debtor, creditor, NotificationType.Normal, body, debt.Id, EntityType.GroupDebts);
        await _notificationRepository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        if (debtor.NotificationsSettings.ReceiveEmail)
        {
            try
            {
                await _emailService.SendEmailAsync(debtor.Email, "PayItOff — przypomnienie o długu", $"<p>{body}</p>");
            }
            catch
            {
                // ignorujemy błąd maila
            }
        }
    }
}