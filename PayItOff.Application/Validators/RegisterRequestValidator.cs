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
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^(\+48\s?)?[0-9]{3}\s?[0-9]{3}\s?[0-9]{3}$")
                .WithMessage("Niepoprawny format numeru telefonu. Użyj formatu: 123456789 lub +48 123 456 789.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
            RuleFor(x => x.IBAN)
                .Matches(@"^(PL|pl)?\s*[0-9]{2}\s*([0-9]{4}\s*){6}$")
                .WithMessage("Niepoprawny format konta. Polski IBAN musi mieć 26 cyfr, może zaczynać się od 'PL' i zawierać spacje.")
                .When(x => !string.IsNullOrWhiteSpace(x.IBAN));
        }
    }
}
