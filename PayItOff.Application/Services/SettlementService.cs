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
using PayItOff.Shared.Responses;
using System.Buffers.Text;

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

        public async Task<List<GlobalDebtSummaryResponse>> GetUserAllIncomesSummaryAsync(int userId)
        {
            var incomes = await _groupDebtRepository.GetUserTotalIncomesAsync(userId);

            var baseUrl = _configuration["AppUrls:BackendUrl"];

            var response = incomes.Select(data => new GlobalDebtSummaryResponse
            {
                UserId = data.UserId,
                Name = data.Name,
                Surname = data.Surname,
                AvatarUrl = $"{baseUrl}/avatars/{data.AvatarUrl ?? "default-avatar.png"}",
                Amount = data.Amount
            }).ToList();

            return response;
        }

        public async Task<List<GlobalDebtSummaryResponse>> GetUserAllExpensesSummaryAsync(int userId)
        {
            var expenses = await _groupDebtRepository.GetUserTotalExpensesAsync(userId);

            var baseUrl = _configuration["AppUrls:BackendUrl"];

            var response = expenses.Select(data => new GlobalDebtSummaryResponse
            {
                UserId = data.UserId,
                Name = data.Name,
                Surname = data.Surname,
                AvatarUrl = $"{baseUrl}/avatars/{data.AvatarUrl ?? "default-avatar.png"}",
                Amount = data.Amount
            }).ToList();

            return response;
        }
    }
}
