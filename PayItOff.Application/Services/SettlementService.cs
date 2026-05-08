using Microsoft.Extensions.Configuration;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Interfaces;
using PayItOff.Shared.Responses;

namespace PayItOff.Application.Services
{
    public class SettlementService : ISettlementService
    {
        private readonly IConfiguration _configuration;
        private readonly IGroupDebtRepository _groupDebtRepository;
        private readonly IUserRepository _userRepository;

        public SettlementService(IConfiguration configuration, IGroupDebtRepository groupDebtRepository, IUserRepository userRepository)
        {
            _configuration = configuration;
            _groupDebtRepository = groupDebtRepository;
            _userRepository = userRepository;

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
    }
}
