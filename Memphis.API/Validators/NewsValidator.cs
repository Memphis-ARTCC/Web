using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class NewsValidator : AbstractValidator<NewsPayload>
{
    public NewsValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
    }
}
