using Microsoft.Extensions.Configuration;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Application.Services
{
    public class SettlementService : ISettlementService
    {
        private readonly IConfiguration _configuration;
        private readonly IGroupDebtRepository _groupDebtRepository;
        private readonly IUserRepository _userRepository;
        private readonly IExpenseSplitRepository _expenseSplitRepository;

        public SettlementService(IConfiguration configuration, IGroupDebtRepository groupDebtRepository, IUserRepository userRepository, IExpenseSplitRepository expenseSplitRepository)
        {
            _configuration = configuration;
            _groupDebtRepository = groupDebtRepository;
            _userRepository = userRepository;
            _expenseSplitRepository = expenseSplitRepository;

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
            const int pageSize = 15;
            var (rawSplits, _) = await _expenseSplitRepository.GetFilteredSplitsAsync(
                userId, request.TargetId, request.Type, request.Page, pageSize);

            var baseUrl = _configuration["AppUrls:BackendUrl"];

            var groupedItems = rawSplits
                .GroupBy(s => s.ExpenseItem.ExpenseId)
                .Select(group =>
                {
                    var first = group.First();
                    var expense = first.ExpenseItem.Expense;

                    bool amIDebtor = first.UserId == userId && expense.PayerId != userId;

                    var other = amIDebtor ? expense.Payer : first.User;

                    return new UserDebtComponentResponse
                    {
                        ExpenseId = expense.Id,
                        Date = expense.PurchasedAt,
                        GroupName = expense.Group?.Name ?? "Wydatki prywatne",
                        AmIDebtor = amIDebtor,
                        Amount = group.Sum(s => s.OwedAmount),
                        Categories = group.Select(s => s.ExpenseItem.Category).Distinct().ToList(),
                        OtherName = other.Name,
                        OtherSurname = other.Surname,
                        OtherAvatarUrl = $"{baseUrl}/avatars/{other.AvatarUrl ?? "default-user-avatar.png"}"
                    };
                })
                .OrderByDescending(x => x.Date)
                .ToList();

            var paged = groupedItems.Skip((request.Page - 1) * pageSize).Take(pageSize).ToList();

            return new PagedTransactionResponse
            {
                Items = paged,
                TotalPages = (int)Math.Ceiling(groupedItems.Count / (double)pageSize),
                CurrentPage = request.Page
            };
        }
    }
}
