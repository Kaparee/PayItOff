using Microsoft.Extensions.Configuration;
using PayItOff.Application.Helpers;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.DomainServices;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Domain.Exceptions;
using PayItOff.Domain.Interfaces;
using PayItOff.Shared.Requests;

namespace PayItOff.Application.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGroupRepository _groupRepository;
        private readonly IUserRepository _userRepository;
        private readonly IExpenseRepository _expenseRepository;
        private readonly IGroupDebtRepository _groupDebtRepository;
        private readonly IFileService _fileService;
        private readonly INotificationService _notificationService;

        private readonly IGroupMemberRepository _groupMemberRepository;

        public ExpenseService(IConfiguration configuration, IUnitOfWork unitOfWork, IGroupRepository groupRepository, IUserRepository userRepository, IExpenseRepository expenseRepository, IGroupDebtRepository groupDebtRepository, IFileService fileService, INotificationService notificationService, IGroupMemberRepository groupMemberRepository)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _groupRepository = groupRepository;
            _userRepository = userRepository;
            _expenseRepository = expenseRepository;
            _groupDebtRepository = groupDebtRepository;
            _fileService = fileService;
            _notificationService = notificationService;
            _groupMemberRepository = groupMemberRepository;
        }

        public async Task CreateExpenseBatch(int userId, CreateExpenseBatchRequest request)
        {
            var group = await _groupRepository.GetGroupInfoByIdAsync(request.GroupId);
            if (group == null) { throw new GroupNotFoundException(); }
            var creator = await _userRepository.GetUserByIdAsync(userId);
            if (creator == null) { throw new UserNotFoundException(); }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var allUserIds = request.Expenses.Select(e => e.PayerId)
                    .Concat(request.Expenses.SelectMany(e => e.Groups.SelectMany(g => g.ParticipantIds)))
                    .Concat(request.Expenses.SelectMany(e => e.Items.SelectMany(i => i.ParticipantIds)))
                    .Distinct()
                    .ToList();

                var usersDict = await _userRepository.GetUsersByIdsAsync(allUserIds);

                foreach (var subDto in request.Expenses)
                {
                    var globalDebts = new Dictionary<(int DebtorId, int CreditorId), decimal>();

                    if (!usersDict.ContainsKey(subDto.PayerId)) { throw new UserNotFoundException(); }
                    var payer = usersDict[subDto.PayerId];

                    var expense = Expense.Create(group, creator, payer, subDto.Name, subDto.PurchasedAt);
                    if (subDto.ReceiptImageUrls != null && subDto.ReceiptImageUrls.Any())
                    {
                        foreach (var photoUrl in subDto.ReceiptImageUrls)
                        {
                            if (!string.IsNullOrWhiteSpace(photoUrl))
                            {
                                expense.AddPhoto(ExpensePhoto.Create(expense.Id, photoUrl));
                            }
                        }
                    }

                    foreach (var gDTO in subDto.Groups)
                    {
                        var calculatedGroupTotal = gDTO.Items.Sum(i => i.Quantity * i.UnitPrice);
                        var expenseGroup = ExpenseGroup.Create(expense, gDTO.Name, calculatedGroupTotal);

                        foreach (var iDTO in gDTO.Items)
                        {
                            var expenseItem = ExpenseItem.Create(expense, expenseGroup, iDTO.Name, iDTO.Category, iDTO.Quantity, iDTO.UnitPrice);

                            var calc = DebtCalculator.CalculateEqualSplit(expenseItem.TotalPrice, payer.Id, gDTO.ParticipantIds, iDTO.RemainderRecipientId);

                            foreach (var r in calc.Splits)
                            {
                                if (!usersDict.ContainsKey(r.UserId)) { throw new UserNotFoundException(); }
                                var user = usersDict[r.UserId];
                                var expenseSplit = ExpenseSplit.Create(expenseItem, user, r.Amount);
                                expenseItem.AddSplit(expenseSplit);
                                if (r.UserId != payer.Id)
                                {
                                    AggregateDebt(globalDebts, r.UserId, payer.Id, r.Amount);
                                }
                            }
                            expenseGroup.AddItem(expenseItem);
                        }
                        expense.AddGroup(expenseGroup);
                    }

                    foreach (var iDTO in subDto.Items)
                    {
                        var expenseItem = ExpenseItem.Create(expense, null, iDTO.Name, iDTO.Category, iDTO.Quantity, iDTO.UnitPrice);

                        var calc = DebtCalculator.CalculateEqualSplit(expenseItem.TotalPrice, payer.Id, iDTO.ParticipantIds, iDTO.RemainderRecipientId);
                        foreach (var r in calc.Splits)
                        {
                            if (!usersDict.ContainsKey(r.UserId)) { throw new UserNotFoundException(); }
                            var user = usersDict[r.UserId];
                            var expenseSplit = ExpenseSplit.Create(expenseItem, user, r.Amount);
                            expenseItem.AddSplit(expenseSplit);
                            if (r.UserId != payer.Id)
                            {
                                AggregateDebt(globalDebts, r.UserId, payer.Id, r.Amount);
                            }
                        }
                        expense.AddItem(expenseItem);
                    }

                    expense.RecalculateTotal();
                    await _expenseRepository.AddAsync(expense);
                    await _unitOfWork.SaveChangesAsync();

                    foreach (var debt in globalDebts)
                    {
                        if (debt.Key.DebtorId == debt.Key.CreditorId) continue;
                        if (!usersDict.ContainsKey(debt.Key.DebtorId)) { throw new UserNotFoundException(); }
                        var debtor = usersDict[debt.Key.DebtorId];
                        if (!usersDict.ContainsKey(debt.Key.CreditorId)) { throw new UserNotFoundException(); }
                        var creditor = usersDict[debt.Key.CreditorId];
                        await _groupDebtRepository.ApplyDebtChangeAsync(group, debtor, creditor, debt.Value);

                        if (debtor.NotificationsSettings.NotifyOnExpenseAdded == true)
                        {
                            var body = NotificationTextHelper.ExpenseAdded(group.Name, expense.Name, creator.FullName, creditor.FullName, debt.Value);
                            await _notificationService.CreateNotificationAsync(debtor.Id, creator.Id, NotificationType.Adding, body, expense.Id, EntityType.Expenses);
                        }
                    }
                }

                group.UpdateTimestamp();
                await _groupRepository.UpdateAsync(group);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteExpenseAsync(int userId, int expenseId)
        {
            var expense = await _expenseRepository.GetExpenseWithSplitsAsync(expenseId);
            if (expense == null) throw new ExpenseNotFoundException();

            var group = await _groupRepository.GetGroupInfoByIdAsync(expense.GroupId);
            if (group == null) throw new GroupNotFoundException();

            var member = await _groupMemberRepository.GetMemberAsync(expense.GroupId, userId);
            if (member == null || (member.Role != GroupMemberRole.Owner && member.Role != GroupMemberRole.Admin))
            {
                throw new InvalidUserRoleException();
            }

            var debtsToRevert = expense.CalculateDebts();
            var payer = await _userRepository.GetUserByIdAsync(expense.PayerId);
            if (payer == null) throw new UserNotFoundException();

            await _unitOfWork.BeginTransactionAsync();
            try
            {

                foreach (var debt in debtsToRevert)
                {
                    var debtor = await _userRepository.GetUserByIdAsync(debt.Key);
                    if (debtor != null)
                    {
                        await _groupDebtRepository.ApplyDebtChangeAsync(group, payer, debtor, debt.Value);
                    }
                }


                await _expenseRepository.DeleteAsync(expense);


                var participantIds = expense.Items.SelectMany(i => i.Splits.Select(s => s.UserId)).Distinct().Where(id => id != userId);
                foreach (var participantId in participantIds)
                {
                    await _notificationService.CreateNotificationAsync(participantId, userId, NotificationType.Deleting, $"Wydatek '{expense.Name}' został usunięty z grupy '{group.Name}'.", expense.GroupId, EntityType.Groups);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteExpenseItemAsync(int userId, int expenseId, int itemId)
        {
            var expense = await _expenseRepository.GetExpenseWithSplitsAsync(expenseId);
            if (expense == null) throw new ExpenseNotFoundException();

            var group = await _groupRepository.GetGroupInfoByIdAsync(expense.GroupId);
            if (group == null) throw new GroupNotFoundException();

            var member = await _groupMemberRepository.GetMemberAsync(expense.GroupId, userId);
            if (member == null || (member.Role != GroupMemberRole.Owner && member.Role != GroupMemberRole.Admin))
            {
                throw new InvalidUserRoleException();
            }

            var item = expense.Items.FirstOrDefault(i => i.Id == itemId)
                ?? expense.Groups.SelectMany(g => g.Items).FirstOrDefault(i => i.Id == itemId);
            if (item == null) throw new Exception("Nie znaleziono pozycji na paragonie");

            var payer = await _userRepository.GetUserByIdAsync(expense.PayerId);
            if (payer == null) throw new UserNotFoundException();


            var debtsToRevert = new Dictionary<int, decimal>();
            foreach (var split in item.Splits)
            {
                if (split.UserId != payer.Id)
                {
                    if (debtsToRevert.ContainsKey(split.UserId)) debtsToRevert[split.UserId] += split.OwedAmount;
                    else debtsToRevert[split.UserId] = split.OwedAmount;
                }
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {

                foreach (var debt in debtsToRevert)
                {
                    var debtor = await _userRepository.GetUserByIdAsync(debt.Key);
                    if (debtor != null)
                    {
                        await _groupDebtRepository.ApplyDebtChangeAsync(group, payer, debtor, debt.Value);
                    }
                }


                await _expenseRepository.DeleteExpenseItemAsync(item);


                decimal newTotal = expense.Items.Where(i => i.Id != itemId && i.ExpenseGroupId == null).Sum(i => i.TotalPrice)
                                 + expense.Groups.Sum(g => g.Items.Where(i => i.Id != itemId).Sum(i => i.TotalPrice));
                expense.GetType().GetProperty("TotalAmount")?.SetValue(expense, newTotal);
                expense.GetType().GetProperty("UpdatedAt")?.SetValue(expense, DateTime.UtcNow);

                if (expense.TotalAmount == 0)
                {
                    expense.Delete();
                }

                await _expenseRepository.UpdateAsync(expense);


                var participantIds = item.Splits.Select(s => s.UserId).Distinct().Where(id => id != userId);
                foreach (var participantId in participantIds)
                {
                    await _notificationService.CreateNotificationAsync(participantId, userId, NotificationType.Deleting, $"Pozycja '{item.Name}' z wydatku '{expense.Name}' została usunięta z grupy '{group.Name}'.", expense.GroupId, EntityType.Groups);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private void AggregateDebt(Dictionary<(int, int), decimal> dict, int debtorId, int creditorId, decimal amount)
        {
            var key = (debtorId, creditorId);
            if (dict.ContainsKey(key)) dict[key] += amount;
            else dict[key] = amount;
        }

        public async Task<PayItOff.Shared.Responses.ExpenseDetailsResponse> GetExpenseDetailsAsync(int userId, int expenseId)
        {
            var expense = await _expenseRepository.GetExpenseWithSplitsAsync(expenseId);
            if (expense == null) throw new ExpenseNotFoundException();

            var groupMember = await _groupMemberRepository.GetMemberAsync(expense.GroupId, userId);
            if (groupMember == null) { throw new InvalidUserRoleException(); }

            var baseUrl = _configuration["AppUrls:BackendUrl"];

            var response = new PayItOff.Shared.Responses.ExpenseDetailsResponse
            {
                ExpenseId = expense.Id,
                Title = expense.Name,
                TotalAmount = expense.TotalAmount,
                Date = expense.PurchasedAt,
                PayerName = $"{expense.Payer.Name} {expense.Payer.Surname}",
                PayerAvatarUrl = UrlHelper.BuildUserAvatarUrl(baseUrl!, expense.Payer.AvatarUrl),
                PayerPhoneNumber = expense.Payer.PhoneNumber,
                PayerIBAN = expense.Payer.IBAN
            };

            var categories = new HashSet<string>();
            var userSplits = new Dictionary<int, PayItOff.Shared.Responses.ExpenseParticipantDto>();

            Action<ExpenseSplit> processSplit = (split) =>
            {
                if (!userSplits.ContainsKey(split.UserId))
                {
                    userSplits[split.UserId] = new PayItOff.Shared.Responses.ExpenseParticipantDto
                    {
                        UserId = split.UserId,
                        FullName = $"{split.User.Name} {split.User.Surname}",
                        AvatarUrl = UrlHelper.BuildUserAvatarUrl(baseUrl!, split.User.AvatarUrl),
                        OwedAmount = 0
                    };
                }
                userSplits[split.UserId].OwedAmount += split.OwedAmount;
            };

            foreach (var item in expense.Items)
            {
                if (item.ExpenseGroupId != null) continue;
                categories.Add(item.Category);
                foreach (var split in item.Splits)
                {
                    processSplit(split);
                }
            }

            foreach (var group in expense.Groups)
            {
                foreach (var item in group.Items)
                {
                    categories.Add(item.Category);
                    foreach (var split in item.Splits)
                    {
                        processSplit(split);
                    }
                }
            }

            response.Category = string.Join(", ", categories);
            response.Participants = userSplits.Values.ToList();
            response.ReceiptPhotos = expense.Photos
                .Select(p => UrlHelper.BuildFileUrl(baseUrl!, p.PhotoUrl))
                .ToList();

            return response;
        }

        public async Task<PayItOff.Shared.Responses.ExpenseDetailsResponse> GetExpenseItemDetailsAsync(int userId, int expenseId, int itemId)
        {
            var expense = await _expenseRepository.GetExpenseWithSplitsAsync(expenseId);
            if (expense == null) throw new ExpenseNotFoundException();

            var item = expense.Items.FirstOrDefault(i => i.Id == itemId)
                       ?? expense.Groups.SelectMany(g => g.Items).FirstOrDefault(i => i.Id == itemId);
            if (item == null) throw new ExpenseNotFoundException();

            var groupMember = await _groupMemberRepository.GetMemberAsync(expense.GroupId, userId);
            if (groupMember == null) { throw new InvalidUserRoleException(); }

            var baseUrl = _configuration["AppUrls:BackendUrl"];

            var response = new PayItOff.Shared.Responses.ExpenseDetailsResponse
            {
                ExpenseId = expense.Id,
                ItemId = item.Id,
                Title = item.Name,
                TotalAmount = item.TotalPrice,
                Date = expense.PurchasedAt,
                PayerName = $"{expense.Payer.Name} {expense.Payer.Surname}",
                PayerAvatarUrl = UrlHelper.BuildUserAvatarUrl(baseUrl!, expense.Payer.AvatarUrl),
                PayerPhoneNumber = expense.Payer.PhoneNumber,
                PayerIBAN = expense.Payer.IBAN,
                Category = item.Category
            };

            var userSplits = new Dictionary<int, PayItOff.Shared.Responses.ExpenseParticipantDto>();

            foreach (var split in item.Splits)
            {
                if (!userSplits.ContainsKey(split.UserId))
                {
                    userSplits[split.UserId] = new PayItOff.Shared.Responses.ExpenseParticipantDto
                    {
                        UserId = split.UserId,
                        FullName = $"{split.User.Name} {split.User.Surname}",
                        AvatarUrl = UrlHelper.BuildUserAvatarUrl(baseUrl!, split.User.AvatarUrl),
                        OwedAmount = 0
                    };
                }
                userSplits[split.UserId].OwedAmount += split.OwedAmount;
            }

            response.Participants = userSplits.Values.ToList();
            response.ReceiptPhotos = expense.Photos
                .Select(p => UrlHelper.BuildFileUrl(baseUrl!, p.PhotoUrl))
                .ToList();

            return response;
        }
        public async Task UpdateExpenseItemAsync(int userId, int expenseId, int itemId, UpdateExpenseItemRequest request)
        {
            var expense = await _expenseRepository.GetExpenseWithSplitsAsync(expenseId);
            if (expense == null) throw new ExpenseNotFoundException();

            var item = expense.Items.FirstOrDefault(i => i.Id == itemId);
            ExpenseGroup? parentGroup = null;
            if (item == null)
            {
                foreach (var group in expense.Groups)
                {
                    item = group.Items.FirstOrDefault(i => i.Id == itemId);
                    if (item != null)
                    {
                        parentGroup = group;
                        break;
                    }
                }
            }
            if (item == null) throw new Exception("Expense item not found");

            var isOwner = await _groupMemberRepository.IsUserOwner(userId, expense.GroupId);
            var member = await _groupMemberRepository.GetMemberAsync(expense.GroupId, userId);
            if (!isOwner && member?.Role != GroupMemberRole.Admin && expense.CreatorId != userId)
                throw new InvalidUserRoleException();

            var oldName = item.Name;
            var oldSplits = item.Splits.ToList();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var group = await _groupRepository.GetGroupInfoByIdAsync(expense.GroupId);
                if (group == null) throw new GroupNotFoundException();

                var allUserIds = item.Splits.Select(s => s.UserId).Concat(new[] { expense.PayerId }).Distinct().ToList();
                if (request.Splits != null && request.Splits.Any())
                {
                    allUserIds = allUserIds.Concat(request.Splits.Select(s => s.UserId)).Distinct().ToList();
                }
                var usersDict = await _userRepository.GetUsersByIdsAsync(allUserIds);
                var payer = usersDict[expense.PayerId];

                if (request.Splits != null && request.Splits.Any())
                {
                    foreach (var split in item.Splits)
                    {
                        if (split.UserId == payer.Id) continue;
                        var debtor = usersDict[split.UserId];
                        await _groupDebtRepository.ApplyDebtChangeAsync(group, debtor, payer, -split.OwedAmount);
                    }

                    item.ClearSplits();

                    foreach (var newSplit in request.Splits)
                    {
                        var newSplitEntity = ExpenseSplit.Create(item, usersDict[newSplit.UserId], newSplit.Amount);
                        item.AddSplit(newSplitEntity);

                        if (newSplit.UserId == payer.Id) continue;
                        var debtor = usersDict[newSplit.UserId];
                        await _groupDebtRepository.ApplyDebtChangeAsync(group, debtor, payer, newSplit.Amount);
                    }

                    item.UpdateUnitPrice(request.TotalPrice / item.Quantity);
                    if (parentGroup != null)
                    {
                        parentGroup.UpdateAmount(parentGroup.Items.Sum(i => i.TotalPrice));
                    }
                    expense.RecalculateTotal();
                }

                item.Edit(request.Name, request.Category);

                await _expenseRepository.UpdateAsync(expense);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                var affectedUserIds = oldSplits.Select(s => s.UserId).ToList();
                if (request.Splits != null) affectedUserIds.AddRange(request.Splits.Select(s => s.UserId));
                affectedUserIds = affectedUserIds.Distinct().Where(id => id != userId).ToList();

                foreach (var id in affectedUserIds)
                {
                    string notificationBody = $"Użytkownik zaktualizował wydatek \"{oldName}\" w grupie {group.Name}.";
                    if (oldName != request.Name) notificationBody += $" Nowa nazwa to \"{request.Name}\".";

                    await _notificationService.CreateNotificationAsync(id, userId, NotificationType.Normal, notificationBody, expense.Id, EntityType.Expenses);
                }
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
