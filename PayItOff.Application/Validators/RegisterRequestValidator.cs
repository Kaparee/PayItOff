using FluentValidation;
using PayItOff.Shared.Requests;
using System.Text.RegularExpressions;

namespace PayItOff.Application.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Podaj poprawny adres Email.");
            RuleFor(x => x.Password).MinimumLength(8).Matches(new Regex("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-])")).WithMessage("Hasło musi mieć min. 8 znaków, składać się z małych i dużych liter oraz znaku specjalnego");
            RuleFor(x => x.Nickname).MinimumLength(3).WithMessage("Nick musi mieć więcej niz 3 znaki.");
        }
    }
}
