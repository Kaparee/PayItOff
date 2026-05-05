using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
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
            if(group == null) { throw new GroupNotFoundException(); }
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
                    if (!usersDict.ContainsKey(subDto.PayerId)){ throw new UserNotFoundException(); }
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
    }
}