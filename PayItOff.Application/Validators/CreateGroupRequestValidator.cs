using FluentValidation;
using PayItOff.Shared.Requests;

namespace PayItOff.Application.Validators
{
    public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
    {
        public CreateGroupRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(20).WithMessage("Podaj poprawną nazwę grupy.");
        }
    }
}
