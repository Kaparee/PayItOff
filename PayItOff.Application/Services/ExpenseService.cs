using Microsoft.Extensions.Configuration;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.DomainServices;
using PayItOff.Domain.Entities;
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

        public ExpenseService(IConfiguration configuration, IUnitOfWork unitOfWork, IGroupRepository groupRepository, IUserRepository userRepository, IExpenseRepository expenseRepository, IGroupDebtRepository groupDebtRepository, IFileService fileService)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _groupRepository = groupRepository;
            _userRepository = userRepository;
            _expenseRepository = expenseRepository;
            _groupDebtRepository = groupDebtRepository;
            _fileService = fileService;
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

                var globalDebts = new Dictionary<(int DebtorId, int CreditorId), decimal>();

                foreach (var subDto in request.Expenses)
                {
                    if (!usersDict.ContainsKey(subDto.PayerId)) { throw new UserNotFoundException(); }
                    var payer = usersDict[subDto.PayerId];

                    var expense = Expense.Create(group, creator, payer, subDto.Name, subDto.ReciptImageUrl, subDto.PurchasedAt);

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
                }
                
                group.UpdateTimestamp();
                await _groupRepository.UpdateAsync(group);
                await _unitOfWork.SaveChangesAsync();
                
                foreach (var debt in globalDebts)
                {
                    if (debt.Key.DebtorId == debt.Key.CreditorId) continue;
                    if (!usersDict.ContainsKey(debt.Key.DebtorId)) { throw new UserNotFoundException(); }
                    var debtor = usersDict[debt.Key.DebtorId];
                    if (!usersDict.ContainsKey(debt.Key.CreditorId)) { throw new UserNotFoundException(); }
                    var creditor = usersDict[debt.Key.CreditorId];
                    await _groupDebtRepository.ApplyDebtChangeAsync(group, debtor, creditor, debt.Value);
                }

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

            // Autoryzacja: użytkownik musi być płatnikiem, twórcą lub uczestnikiem wydatku
            var isAuthorized = expense.PayerId == userId
                || expense.CreatorId == userId
                || expense.Items.Any(i => i.Splits.Any(s => s.UserId == userId))
                || expense.Groups.Any(g => g.Items.Any(i => i.Splits.Any(s => s.UserId == userId)));

            if (!isAuthorized) throw new InvalidUserRoleException();

            var response = new PayItOff.Shared.Responses.ExpenseDetailsResponse
            {
                ExpenseId = expense.Id,
                Title = expense.Name,
                TotalAmount = expense.TotalAmount,
                Date = expense.PurchasedAt,
                PayerName = $"{expense.Payer.Name} {expense.Payer.Surname}",
                PayerAvatarUrl = PayItOff.Application.Helpers.AvatarUrlHelper.BuildUserAvatarUrl(_configuration["AppUrls:BackendUrl"], expense.Payer.AvatarUrl),
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
                        AvatarUrl = PayItOff.Application.Helpers.AvatarUrlHelper.BuildUserAvatarUrl(_configuration["AppUrls:BackendUrl"], split.User.AvatarUrl),
                        OwedAmount = 0
                    };
                }
                userSplits[split.UserId].OwedAmount += split.OwedAmount;
            };

            foreach (var item in expense.Items)
            {
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

            return response;
        }

        public async Task<PayItOff.Shared.Responses.ExpenseDetailsResponse> GetExpenseItemDetailsAsync(int userId, int expenseId, int itemId)
        {
            var expense = await _expenseRepository.GetExpenseWithSplitsAsync(expenseId);
            if (expense == null) throw new ExpenseNotFoundException();

            var item = expense.Items.FirstOrDefault(i => i.Id == itemId) 
                       ?? expense.Groups.SelectMany(g => g.Items).FirstOrDefault(i => i.Id == itemId);
            if (item == null) throw new ExpenseNotFoundException();

            var isAuthorized = expense.PayerId == userId
                || expense.CreatorId == userId
                || item.Splits.Any(s => s.UserId == userId);

            if (!isAuthorized) throw new InvalidUserRoleException();

            var response = new PayItOff.Shared.Responses.ExpenseDetailsResponse
            {
                ExpenseId = expense.Id,
                Title = item.Name,
                TotalAmount = item.TotalPrice,
                Date = expense.PurchasedAt,
                PayerName = $"{expense.Payer.Name} {expense.Payer.Surname}",
                PayerAvatarUrl = PayItOff.Application.Helpers.AvatarUrlHelper.BuildUserAvatarUrl(_configuration["AppUrls:BackendUrl"], expense.Payer.AvatarUrl),
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
                        AvatarUrl = PayItOff.Application.Helpers.AvatarUrlHelper.BuildUserAvatarUrl(_configuration["AppUrls:BackendUrl"], split.User.AvatarUrl),
                        OwedAmount = 0
                    };
                }
                userSplits[split.UserId].OwedAmount += split.OwedAmount;
            }

            response.Participants = userSplits.Values.ToList();

            return response;
        }
    }
}