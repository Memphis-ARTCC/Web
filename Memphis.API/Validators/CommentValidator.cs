using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class CommentValidator : AbstractValidator<CommentPayload>
{
    public CommentValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().GreaterThan(0);
        RuleFor(x => x.Message).NotEmpty();
    }
}
