using FluentValidation;
using Memphis.Shared.Dtos;

namespace Memphis.Shared.Validators;

public class CommentValidator : AbstractValidator<CommentDto>
{
    public CommentValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
    }
}
