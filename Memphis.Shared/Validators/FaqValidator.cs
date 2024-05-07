using FluentValidation;
using Memphis.Shared.Dtos;

namespace Memphis.Shared.Validators;

public class FaqValidator : AbstractValidator<FaqDto>
{
    public FaqValidator()
    {
        RuleFor(x => x.Question).NotEmpty();
        RuleFor(x => x.Answer).NotEmpty();
        RuleFor(x => x.Order).NotEmpty();
    }
}
