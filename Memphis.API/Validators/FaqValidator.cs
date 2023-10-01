using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class FaqValidator : AbstractValidator<Faq>
{
    public FaqValidator()
    {
        RuleFor(x => x.Question).NotEmpty();
        RuleFor(x => x.Answer).NotEmpty();
        RuleFor(x => x.Order).NotEmpty();
    }
}
