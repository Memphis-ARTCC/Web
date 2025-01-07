using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class OtsValidator : AbstractValidator<OtsPayload>
{
    public OtsValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Milestone).NotEmpty();
        RuleFor(x => x.Facility).NotEmpty();
    }
}