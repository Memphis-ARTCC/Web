using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class StaffingRequestValidator : AbstractValidator<StaffingRequestPayload>
{
    public StaffingRequestValidator()
    {
        RuleFor(x => x.Cid).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Organization).NotEmpty();
        RuleFor(x => x.EstimatedPilots).GreaterThan(0);
        RuleFor(x => x.Start).NotEmpty();
        RuleFor(x => x.Duration).NotEmpty();
    }
}
