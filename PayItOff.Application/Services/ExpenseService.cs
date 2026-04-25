using MailKit.Net.Smtp;
using MailKit.Security;
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

        public ExpenseService(IConfiguration configuration, IUnitOfWork unitOfWork, IGroupRepository groupRepository, IUserRepository userRepository, IExpenseRepository expenseRepository, IGroupDebtRepository groupDebtRepository)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _groupRepository = groupRepository;
            _userRepository = userRepository;
            _expenseRepository = expenseRepository;
            _groupDebtRepository = groupDebtRepository;
        }

        public async Task CreateExpenseBatch(int userId, CreateExpenseBatchRequest request)
        {
            var group = await _groupRepository.GetGroupInfoByIdAsync(request.GroupId);
            if(group == null) { throw new GroupNotFoundException(); }
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var globalDebts = new Dictionary<(int DebtorId, int CreditorId), decimal>();

                foreach (var subDto in request.Expenses)
                {
                    var payer = await _userRepository.GetUserByIdAsync(subDto.PayerId);
                    if (payer == null) { throw new UserNotFoundException(); }

                    var expense = Expense.Create(group, payer, subDto.Name, subDto.ReciptImageUrl, subDto.PurchasedAt);

                    foreach (var gDTO in subDto.Groups)
                    {
                        var expenseGroup = ExpenseGroup.Create(expense, gDTO.Name, gDTO.TotalAmount);

                        foreach (var iDTO in gDTO.Items)
                        {
                            var expenseItem = ExpenseItem.Create(expense, expenseGroup, iDTO.Name, iDTO.Category, iDTO.Quantity, iDTO.UnitPrice);

                            var calc = DebtCalculator.CalculateEqualSplit(expenseItem.TotalPrice, payer.Id, gDTO.ParticipantIds, subDto.RemainderRecipientId);

                            foreach (var r in calc.Splits)
                            {
                                var user = await _userRepository.GetUserByIdAsync(r.UserId);
                                if (user == null) { throw new UserNotFoundException(); }
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

                        var calc = DebtCalculator.CalculateEqualSplit(expenseItem.TotalPrice, payer.Id, iDTO.ParticipantIds, subDto.RemainderRecipientId);
                        foreach (var r in calc.Splits)
                        {
                            var user = await _userRepository.GetUserByIdAsync(r.UserId);
                            if (user == null) { throw new UserNotFoundException(); }
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
                    var debtor = await _userRepository.GetUserByIdAsync(debt.Key.DebtorId);
                    if (debtor == null) { throw new UserNotFoundException(); }
                    var creditor = await _userRepository.GetUserByIdAsync(debt.Key.CreditorId);
                    if (creditor == null) { throw new UserNotFoundException(); }
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