using FluentValidation;
using Memphis.Shared.Dtos;

namespace Memphis.Shared.Validators;

public class OtsValidator : AbstractValidator<OtsDto>
{
    public OtsValidator()
    {
        RuleFor(x => x.Submitter).NotEmpty();
        RuleFor(x => x.User).NotEmpty();
        RuleFor(x => x.Milestone).NotEmpty();
        RuleFor(x => x.Facility).NotEmpty();
    }
}