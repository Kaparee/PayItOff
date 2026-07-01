using Microsoft.Extensions.Configuration;
using PayItOff.Application.Interfaces;

namespace PayItOff.Application.Services
{
    public class EmailService : IEmailService
    {
        public static bool IsDisabledForSeeder { get; set; } = false;

        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendEmailAsync(string to, string subject, string body)
        {
            return Task.CompletedTask;
        }
    }
}